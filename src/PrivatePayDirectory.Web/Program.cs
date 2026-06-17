using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using PrivatePayDirectory.Infrastructure;
using PrivatePayDirectory.Web;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// Authentication — cookie with email/password (federation can be added later)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

// Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.RequireAdmin, p => p.RequireRole("Administrator"))
    .AddPolicy(Policies.RequireTherapist, p => p.RequireRole("Therapist", "Administrator"))
    .AddPolicy(Policies.RequireAuthenticated, p => p.RequireAuthenticatedUser());

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", Policies.RequireAdmin);
    options.Conventions.AuthorizePage("/Therapist/Edit", Policies.RequireAuthenticated);
    options.Conventions.AuthorizePage("/Therapist/Register", Policies.RequireAuthenticated);
});

var app = builder.Build();
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

startupLogger.LogInformation(
    "Application starting. Environment={EnvironmentName}",
    app.Environment.EnvironmentName);

// Provision Cosmos containers + Blob container; seed dev admin on first run
using (var scope = app.Services.CreateScope())
{
    startupLogger.LogInformation("Ensuring infrastructure resources at startup.");
    await InfrastructureServiceExtensions.EnsureResourcesAsync(scope.ServiceProvider);
    if (app.Environment.IsDevelopment())
    {
        startupLogger.LogInformation("Development environment detected. Seeding local development data.");
        await SeedDevAdminAsync(scope.ServiceProvider);
        await SeedDevTherapistsAsync(scope.ServiceProvider);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

// Logout handled here to avoid Razor Pages antiforgery handler conflicts
app.MapPost("/Account/Logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
}).DisableAntiforgery();

// Minimal API: server-side photo upload (avoids CORS issues with Azurite in dev)
app.MapPost("/api/photo-upload", async (
    IFormFile file,
    string therapistId,
    IPhotoService photoService,
    HttpContext ctx) =>
{
    if (!ctx.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var therapistIdClaim = ctx.User.FindFirst("TherapistId")?.Value;
    if (therapistIdClaim != therapistId && !ctx.User.IsInRole("Administrator"))
        return Results.Forbid();

    using var stream = file.OpenReadStream();
    var key = await photoService.UploadPhotoAsync(therapistId, stream, file.ContentType);
    return Results.Ok(new { key });
}).RequireAuthorization().DisableAntiforgery();

// Minimal API: generate pre-signed upload URL for therapist photo
app.MapGet("/api/photo-upload-url", async (
    string therapistId,
    string contentType,
    IPhotoService photoService,
    HttpContext ctx) =>
{
    if (!ctx.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    try
    {
        var therapistIdClaim = ctx.User.FindFirst("TherapistId")?.Value;
        if (therapistIdClaim != therapistId && !ctx.User.IsInRole("Administrator"))
            return Results.Forbid();

        var uploadUrl = await photoService.GenerateUploadUrlAsync(therapistId, contentType);
        var key = $"therapists/{therapistId}/profile";
        return Results.Ok(new { uploadUrl, key });
    }
    catch (Exception exc)
    {
        Console.WriteLine(exc.Message);
        return Results.InternalServerError();
    }
}).RequireAuthorization().DisableAntiforgery();

startupLogger.LogInformation("Startup complete. Beginning request handling.");

app.Run();

// Seeds a single admin account for local development (admin@local.dev / Admin1234!)
static async Task SeedDevAdminAsync(IServiceProvider services)
{
    var userRepo = services.GetRequiredService<IUserRepository>();
    const string email = "admin@local.dev";
    var existing = await userRepo.GetByEmailAsync(email);
    if (existing != null) return;

    var admin = new AppUser
    {
        UserId = Guid.NewGuid().ToString(),
        Email = email,
        Role = UserRole.Administrator,
    };
    var hasher = services.GetRequiredService<IPasswordHasher<AppUser>>();
    admin.PasswordHash = hasher.HashPassword(admin, "Admin1234!");
    await userRepo.SaveAsync(admin);
}

static async Task SeedDevTherapistsAsync(IServiceProvider services)
{
    var repo = services.GetRequiredService<ITherapistRepository>();

    // Skip if already seeded
    var existing = await repo.GetAllAsync();
    if (existing.Any(t => t.UserId == "seed")) return;

    var therapists = new[]
    {
        new Therapist
        {
            TherapistId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            FirstName = "Alice", LastName = "Morgan", Title = "LCSW",
            Bio = "Specializing in anxiety and depression with a compassionate, evidence-based approach.",
            Specialties = ["Anxiety", "Depression", "Trauma & PTSD"],
            InsuranceAccepted = ["Private Pay"],
            Languages = ["English"],
            LicensedVirtualStates = ["TX", "CA", "NY"],
            Offices = [],
            AcceptingNewClients = true,
            Phone = "512-555-0101", Email = "alice.morgan@example.com",
        },
        new Therapist
        {
            TherapistId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            FirstName = "David", LastName = "Chen", Title = "LPC",
            Bio = "Helping individuals and couples navigate life transitions and relationship challenges.",
            Specialties = ["Couples Therapy", "Life Transitions", "Stress Management"],
            InsuranceAccepted = ["Private Pay"],
            Languages = ["English", "Mandarin"],
            LicensedVirtualStates = [],
            Offices =
            [
                new OfficeLocation
                {
                    Label = "Austin Office",
                    Street = "123 Congress Ave", City = "Austin", State = "TX", Zip = "78701"
                }
            ],
            AcceptingNewClients = true,
            Phone = "512-555-0202", Email = "david.chen@example.com",
        },
        new Therapist
        {
            TherapistId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            FirstName = "Maria", LastName = "Gutierrez", Title = "PhD",
            Bio = "Bilingual psychologist offering culturally sensitive care for adults and adolescents.",
            Specialties = ["Anxiety", "Cultural & Identity Issues", "Adolescents"],
            InsuranceAccepted = ["Private Pay"],
            Languages = ["English", "Spanish"],
            LicensedVirtualStates = ["TX", "FL"],
            Offices =
            [
                new OfficeLocation
                {
                    Label = "Houston Office",
                    Street = "456 Main St", City = "Houston", State = "TX", Zip = "77002"
                }
            ],
            AcceptingNewClients = true,
            Phone = "713-555-0303", Email = "maria.gutierrez@example.com",
        },
        new Therapist
        {
            TherapistId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            FirstName = "James", LastName = "Okafor", Title = "LMFT",
            Bio = "Marriage and family therapist focused on building resilience and healthy communication.",
            Specialties = ["Couples Therapy", "Family Therapy", "Grief & Loss"],
            InsuranceAccepted = ["Private Pay"],
            Languages = ["English"],
            LicensedVirtualStates = ["NY", "NJ"],
            Offices = [],
            AcceptingNewClients = false,
            Phone = "212-555-0404", Email = "james.okafor@example.com",
        },
        new Therapist
        {
            TherapistId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            FirstName = "Sarah", LastName = "Patel", Title = "LCSW",
            Bio = "Trauma-informed therapist with a focus on EMDR and somatic approaches.",
            Specialties = ["Trauma & PTSD", "LGBTQ+ Issues", "Anxiety"],
            InsuranceAccepted = ["Private Pay"],
            Languages = ["English", "Hindi"],
            LicensedVirtualStates = ["CA", "WA", "OR"],
            Offices =
            [
                new OfficeLocation
                {
                    Label = "Seattle Office",
                    Street = "789 Pike St", City = "Seattle", State = "WA", Zip = "98101"
                }
            ],
            AcceptingNewClients = true,
            Phone = "206-555-0505", Email = "sarah.patel@example.com",
        },
        new Therapist
        {
            TherapistId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            FirstName = "Robert", LastName = "Kim", Title = "PsyD",
            Bio = "Specializing in men's mental health, performance anxiety, and career stress.",
            Specialties = ["Men's Issues", "Anxiety", "Stress Management"],
            InsuranceAccepted = ["Private Pay"],
            Languages = ["English", "Korean"],
            LicensedVirtualStates = ["CA"],
            Offices =
            [
                new OfficeLocation
                {
                    Label = "Los Angeles Office",
                    Street = "321 Wilshire Blvd", City = "Los Angeles", State = "CA", Zip = "90010"
                }
            ],
            AcceptingNewClients = true,
            Phone = "310-555-0606", Email = "robert.kim@example.com",
        },
    };

    foreach (var t in therapists)
        await repo.SaveAsync(t);
}



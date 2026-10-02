using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using PrivatePayDirectory.Infrastructure;
using PrivatePayDirectory.Web;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// Persist Data Protection keys to blob storage so antiforgery tokens survive restarts/redeployments.
// Keys are stored in a dedicated container separate from photos.
var dpBlobConnStr = builder.Configuration["BlobStorage:ConnectionString"];
if (!string.IsNullOrWhiteSpace(dpBlobConnStr))
{
    var keysContainer = new BlobContainerClient(dpBlobConnStr, "dataprotection-keys");
    keysContainer.CreateIfNotExists();
    builder.Services.AddDataProtection()
        .PersistKeysToAzureBlobStorage(keysContainer.GetBlobClient("keys.xml"))
        .SetApplicationName("PrivatePayDirectory");
}

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
    .AddPolicy(Policies.RequireProvider, p => p.RequireRole("Provider", "Administrator"))
    .AddPolicy(Policies.RequireAuthenticated, p => p.RequireAuthenticatedUser());

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", Policies.RequireAdmin);
    options.Conventions.AuthorizePage("/Provider/Edit", Policies.RequireAuthenticated);
    options.Conventions.AuthorizePage("/Provider/Register", Policies.RequireAuthenticated);
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
        await SeedDevProvidersAsync(scope.ServiceProvider);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    // Container listens on plain HTTP; TLS is terminated at the reverse proxy.
    // Forward the X-Forwarded-Proto header so the app sees requests as HTTPS.
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
}
else
{
    app.UseHttpsRedirection();
}

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
    string providerId,
    IPhotoService photoService,
    HttpContext ctx) =>
{
    if (!ctx.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var providerIdClaim = ctx.User.FindFirst("ProviderId")?.Value;
    if (providerIdClaim != providerId && !ctx.User.IsInRole("Administrator"))
        return Results.Forbid();

    using var stream = file.OpenReadStream();
    var key = await photoService.UploadPhotoAsync(providerId, stream, file.ContentType);
    return Results.Ok(new { key });
}).RequireAuthorization().DisableAntiforgery();

// Minimal API: generate pre-signed upload URL for provider photo
app.MapGet("/api/photo-upload-url", async (
    string providerId,
    string contentType,
    IPhotoService photoService,
    HttpContext ctx) =>
{
    if (!ctx.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    try
    {
        var providerIdClaim = ctx.User.FindFirst("ProviderId")?.Value;
        if (providerIdClaim != providerId && !ctx.User.IsInRole("Administrator"))
            return Results.Forbid();

        var uploadUrl = await photoService.GenerateUploadUrlAsync(providerId, contentType);
        var key = $"providers/{providerId}/profile";
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

static async Task SeedDevProvidersAsync(IServiceProvider services)
{
    var repo = services.GetRequiredService<IProviderRepository>();

    // Skip if already seeded
    var existing = await repo.GetAllAsync();
    if (existing.Any(p => p.UserId == "seed")) return;

    var providers = new[]
    {
        new Provider
        {
            ProviderId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            Profession = Profession.Therapist,
            FirstName = "Alice", LastName = "Morgan", Title = "LCSW",
            Bio = "Specializing in anxiety and depression with a compassionate, evidence-based approach.",
            Specialties = ["Anxiety", "Depression", "Trauma / PTSD"],
            InsuranceAccepted = ["Private Pay"],
            Languages = ["English"],
            LicensedVirtualStates = ["TX", "CA", "NY"],
            Offices = [],
            AcceptingNewClients = true,
            Phone = "512-555-0101", Email = "alice.morgan@example.com",
        },
        new Provider
        {
            ProviderId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            Profession = Profession.Therapist,
            FirstName = "David", LastName = "Chen", Title = "LPC",
            Bio = "Helping individuals and couples navigate life transitions and relationship challenges.",
            Specialties = ["Couples / Marriage", "Life Transitions", "Stress Management"],
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
        new Provider
        {
            ProviderId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            Profession = Profession.Therapist,
            FirstName = "Maria", LastName = "Gutierrez", Title = "PhD",
            Bio = "Bilingual psychologist offering culturally sensitive care for adults and adolescents.",
            Specialties = ["Anxiety", "Child & Adolescent"],
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
        new Provider
        {
            ProviderId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            Profession = Profession.Chiropractor,
            FirstName = "James", LastName = "Okafor", Title = "DC",
            Bio = "Specializing in sports injuries and chronic pain management with a holistic approach.",
            Specialties = ["Back Pain", "Sports Injuries", "Neck Pain"],
            InsuranceAccepted = ["Private Pay"],
            Languages = ["English"],
            LicensedVirtualStates = [],
            Offices =
            [
                new OfficeLocation
                {
                    Label = "New York Office",
                    Street = "100 Broadway", City = "New York", State = "NY", Zip = "10005"
                }
            ],
            AcceptingNewClients = false,
            Phone = "212-555-0404", Email = "james.okafor@example.com",
        },
        new Provider
        {
            ProviderId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            Profession = Profession.MassageTherapist,
            FirstName = "Sarah", LastName = "Patel", Title = "LMT",
            Bio = "Certified massage therapist offering deep tissue, prenatal, and sports massage.",
            Specialties = ["Deep Tissue", "Prenatal Massage", "Sports Massage"],
            InsuranceAccepted = ["Private Pay"],
            Languages = ["English"],
            LicensedVirtualStates = [],
            Offices =
            [
                new OfficeLocation
                {
                    Label = "Seattle Studio",
                    Street = "789 Pike St", City = "Seattle", State = "WA", Zip = "98101"
                }
            ],
            AcceptingNewClients = true,
            Phone = "206-555-0505", Email = "sarah.patel@example.com",
        },
        new Provider
        {
            ProviderId = Guid.NewGuid().ToString(),
            UserId = "seed",
            IsVisible = true,
            Profession = Profession.Hairstylist,
            FirstName = "Robert", LastName = "Kim", Title = "",
            Bio = "Specializing in color, balayage, and curly hair — making every client feel their best.",
            Specialties = ["Color", "Balayage", "Curly Hair"],
            InsuranceAccepted = ["Private Pay"],
            Languages = ["English", "Korean"],
            LicensedVirtualStates = [],
            Offices =
            [
                new OfficeLocation
                {
                    Label = "Los Angeles Salon",
                    Street = "321 Wilshire Blvd", City = "Los Angeles", State = "CA", Zip = "90010"
                }
            ],
            AcceptingNewClients = true,
            Phone = "310-555-0606", Email = "robert.kim@example.com",
        },
    };

    foreach (var p in providers)
        await repo.SaveAsync(p);
}

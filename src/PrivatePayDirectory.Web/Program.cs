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
using PrivatePayDirectory.Infrastructure.Local;
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

builder.Services.Configure<RouteOptions>(options =>
    options.ConstraintMap["profession"] = typeof(ProfessionRouteConstraint));

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

// Provision Cosmos containers + Blob container; seed dev admin on first run; add/remove example profiles
using (var scope = app.Services.CreateScope())
{
    startupLogger.LogInformation("Ensuring infrastructure resources at startup.");
    await InfrastructureServiceExtensions.EnsureResourcesAsync(scope.ServiceProvider);
    if (app.Environment.IsDevelopment())
    {
        startupLogger.LogInformation("Development environment detected. Seeding local development admin.");
        await SeedDevAdminAsync(scope.ServiceProvider);
    }
    await DemoData.SyncAsync(
        scope.ServiceProvider.GetRequiredService<IProviderRepository>(),
        app.Configuration.GetValue<bool>("DemoData:Enabled"),
        startupLogger);
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

// Local storage mode: serve photos saved on disk (Azure mode serves them from Blob Storage via SAS URLs)
if (app.Services.GetService<LocalPhotoService>() is { } localPhotos)
{
    app.MapGet(LocalPhotoService.UrlPrefix + "{**key}", (string key) =>
        localPhotos.Open(key) is var (content, contentType)
            ? Results.Stream(content, contentType)
            : Results.NotFound());
}

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

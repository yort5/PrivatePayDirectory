using System.Text.Json;
using System.Xml.Linq;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Web;

/// <summary>
/// Search-engine / AI-crawler support: absolute URLs, robots.txt, sitemap.xml, and profile JSON-LD.
/// Example (demo) profiles are kept out of the sitemap and marked noindex on their pages.
/// </summary>
public static class Seo
{
    /// <summary>
    /// Absolute URL for a site path. Uses the Site:BaseUrl setting when present (recommended in production,
    /// so URLs are https:// even behind the reverse proxy), otherwise the current request's scheme and host.
    /// </summary>
    public static string AbsoluteUrl(HttpContext context, string path)
    {
        var baseUrl = context.RequestServices.GetRequiredService<IConfiguration>()["Site:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        return baseUrl.TrimEnd('/') + path;
    }

    public static void MapSeoEndpoints(this WebApplication app)
    {
        app.MapGet("/robots.txt", (HttpContext ctx) => Results.Text(
            $"""
            User-agent: *
            Disallow: /Admin
            Disallow: /Account
            Disallow: /Provider/Edit
            Disallow: /Provider/Register
            Disallow: /api/

            Sitemap: {AbsoluteUrl(ctx, "/sitemap.xml")}
            """, "text/plain"));

        app.MapGet("/sitemap.xml", async (HttpContext ctx, IProviderRepository repo) =>
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XElement Url(string path, DateTime? lastModified = null) =>
                new(ns + "url",
                    new XElement(ns + "loc", AbsoluteUrl(ctx, path)),
                    lastModified is { } d ? new XElement(ns + "lastmod", d.ToString("yyyy-MM-dd")) : null);

            var profiles = (await repo.GetVisibleAsync())
                .Where(p => p.UserId != DemoData.DemoUserId && p.Slug != null);

            var urlset = new XElement(ns + "urlset",
                Url("/"),
                Taxonomy.Professions.Select(p => Url("/" + p.Slug)),
                profiles.Select(p => Url(ProviderUrls.ProfilePath(p), p.UpdatedAt)));

            return Results.Text(new XDocument(new XDeclaration("1.0", "utf-8", null), urlset).Declaration + "\n" + urlset,
                "application/xml");
        });
    }

    /// <summary>A complete &lt;script type="application/ld+json"&gt; element with a provider's schema.org data.</summary>
    public static string ProviderJsonLdScript(Provider p, string profileUrl) =>
        $"<script type=\"application/ld+json\">{ProviderJsonLd(p, profileUrl)}</script>";

    /// <summary>schema.org structured data describing a provider.</summary>
    public static string ProviderJsonLd(Provider p, string profileUrl)
    {
        var office = p.Offices.FirstOrDefault();
        var data = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = Taxonomy.Info(p.Profession).SchemaType,
            ["name"] = string.IsNullOrWhiteSpace(p.Title) ? $"{p.FirstName} {p.LastName}" : $"{p.FirstName} {p.LastName}, {p.Title}",
            ["url"] = profileUrl,
            ["description"] = string.IsNullOrWhiteSpace(p.Bio) ? null : p.Bio,
            ["telephone"] = p.Phone,
            ["email"] = p.Email,
            ["sameAs"] = p.WebsiteUrl,
            ["knowsAbout"] = p.Specialties.Count > 0 ? p.Specialties : null,
            ["paymentAccepted"] = p.InsuranceAccepted.Count > 0 ? string.Join(", ", p.InsuranceAccepted) : null,
            ["address"] = office == null ? null : new Dictionary<string, string>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = office.Street,
                ["addressLocality"] = office.City,
                ["addressRegion"] = office.State,
                ["postalCode"] = office.Zip,
                ["addressCountry"] = "US",
            },
            // Virtual sessions: the states the provider is licensed in
            ["areaServed"] = p.LicensedVirtualStates.Count == 0 ? null
                : p.LicensedVirtualStates.Select(s => new Dictionary<string, string> { ["@type"] = "State", ["name"] = s }).ToList(),
        };

        // The default encoder escapes '<', so bio text can't close the <script> element
        return JsonSerializer.Serialize(data.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value));
    }
}

using System.Text.Json;
using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Models;
using PrivatePayDirectory.Infrastructure.Local;
using PrivatePayDirectory.Web;

namespace PrivatePayDirectory.Tests;

public class SeoTests
{
    [Theory]
    [InlineData("Grace Fischer Austin", "grace-fischer-austin")]
    [InlineData("José O'Brien (Example)", "jose-obrien-example")]
    [InlineData("  Ann--Marie  St. Louis ", "ann-marie-st-louis")]
    [InlineData("", "")]
    public void Slugify_MakesLowercaseAsciiHyphenatedWords(string input, string expected) =>
        Assert.Equal(expected, ProviderUrls.Slugify(input));

    [Fact]
    public async Task AssignSlug_NumbersDuplicates_AndKeepsOwnSlugOnResave()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ppd-tests-" + Guid.NewGuid());
        try
        {
            var repo = new LocalProviderRepository(
                new LocalJsonStore<Provider>(Path.Combine(dir, "providers.json"), p => p.ProviderId));
            var office = new OfficeLocation { City = "Austin", State = "TX" };

            var first = new Provider { FirstName = "Sam", LastName = "Lee", Offices = [office] };
            await ProviderUrls.AssignSlugAsync(repo, first);
            await repo.SaveAsync(first);

            var second = new Provider { FirstName = "Sam", LastName = "Lee", Offices = [office] };
            await ProviderUrls.AssignSlugAsync(repo, second);
            await repo.SaveAsync(second);

            await ProviderUrls.AssignSlugAsync(repo, first); // re-saving must not bump its own slug

            Assert.Equal("sam-lee-austin", first.Slug);
            Assert.Equal("sam-lee-austin-2", second.Slug);
            Assert.Equal("/therapists/sam-lee-austin", ProviderUrls.ProfilePath(first));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ProviderJsonLd_DescribesProvider_AndCannotBreakOutOfScriptTag()
    {
        var p = new Provider
        {
            Profession = Profession.Hairstylist,
            FirstName = "Robin", LastName = "Kay", Title = "Colorist",
            Bio = "Hi </script><script>alert(1)</script>",
            Specialties = ["Color", "Balayage"],
            InsuranceAccepted = ["Private Pay"],
            LicensedVirtualStates = ["TX"],
            Offices = [new OfficeLocation { Street = "1 Main St", City = "Austin", State = "TX", Zip = "78701" }],
        };

        var json = Seo.ProviderJsonLd(p, "https://example.org/hairstylists/robin-kay-austin");
        Assert.DoesNotContain("</script>", json);

        var root = JsonDocument.Parse(json).RootElement;
        Assert.Equal("HairSalon", root.GetProperty("@type").GetString());
        Assert.Equal("Robin Kay, Colorist", root.GetProperty("name").GetString());
        Assert.Equal(p.Bio, root.GetProperty("description").GetString()); // escaped, not altered
        Assert.Equal("Austin", root.GetProperty("address").GetProperty("addressLocality").GetString());
        Assert.Equal(2, root.GetProperty("knowsAbout").GetArrayLength());
        Assert.Equal("TX", root.GetProperty("areaServed")[0].GetProperty("name").GetString());
        Assert.False(root.TryGetProperty("telephone", out _)); // nulls are omitted
    }

    [Fact]
    public void DemoData_IsClearlyMarked_WithUniqueIdsAndSlugs()
    {
        var providers = DemoData.Generate();

        Assert.Equal(69, providers.Count);
        Assert.All(providers, p =>
        {
            Assert.Equal(DemoData.DemoUserId, p.UserId);
            Assert.EndsWith("(Example)", p.LastName);
            Assert.Contains("example profile", p.Bio);
            Assert.EndsWith("@example.com", p.Email);
        });
        Assert.Equal(providers.Count, providers.Select(p => p.ProviderId).Distinct().Count());
        Assert.Equal(providers.Count, providers.Select(p => p.Slug).Distinct().Count());
    }
}

using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Tests;

public class TaxonomyRegionTests
{
    [Fact]
    public void ProvinceCodesNeverCollideWithStateCodes() =>
        Assert.Empty(Taxonomy.CanadianProvinces.Intersect(Taxonomy.UnitedStates));

    [Theory]
    [InlineData("ON", true)]
    [InlineData("qc", true)]
    [InlineData("TX", false)]
    [InlineData(null, false)]
    public void IsCanadian(string? code, bool expected) => Assert.Equal(expected, Taxonomy.IsCanadian(code));

    [Theory]
    [InlineData(new[] { "TX", "CO" }, "States")]
    [InlineData(new[] { "ON", "BC" }, "Provinces")]
    [InlineData(new[] { "WA", "BC" }, "States & Provinces")]
    public void RegionNoun(string[] codes, string expected) => Assert.Equal(expected, Taxonomy.RegionNoun(codes));

    [Theory]
    [InlineData("m5h2n2", "ON", "M5H 2N2")]
    [InlineData(" v6b-1a1 ", "BC", "V6B 1A1")]
    [InlineData("M5H 2N2", "ON", "M5H 2N2")]
    [InlineData("not a code", "ON", "not a code")]
    [InlineData(" 78701 ", "TX", "78701")]
    [InlineData("m5h2n2", "TX", "m5h2n2")]
    public void NormalizePostalCode(string input, string region, string expected) =>
        Assert.Equal(expected, Taxonomy.NormalizePostalCode(input, region));
}

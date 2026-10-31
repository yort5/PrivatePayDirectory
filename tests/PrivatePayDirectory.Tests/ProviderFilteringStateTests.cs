using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using PrivatePayDirectory.Infrastructure.Repositories;

namespace PrivatePayDirectory.Tests;

public class ProviderFilteringStateTests
{
    private static readonly Provider VirtualInOntario = new() { LastName = "A", LicensedVirtualStates = ["ON"] };
    private static readonly Provider OfficeInOntario = new() { LastName = "B", Offices = [new() { State = "ON" }] };
    private static readonly Provider BothInOntario = new() { LastName = "C", LicensedVirtualStates = ["ON"], Offices = [new() { State = "ON" }] };
    private static readonly Provider TexasOnly = new() { LastName = "D", LicensedVirtualStates = ["TX"], Offices = [new() { State = "TX" }] };
    private static readonly Provider[] All = [VirtualInOntario, OfficeInOntario, BothInOntario, TexasOnly];

    private static string Names(ProviderFilter f) =>
        string.Concat(ProviderFiltering.Apply(All, f).Select(p => p.LastName));

    [Fact]
    public void AnyStateMatchesLicensedOrOffice() => Assert.Equal("ABC", Names(new() { AnyState = "on" }));

    [Fact]
    public void VirtualStateMatchesLicensedOnly() =>
        Assert.Equal("AC", Names(new() { OffersVirtual = true, VirtualState = "ON" }));

    [Fact]
    public void OfficeStateMatchesOfficesOnly() =>
        Assert.Equal("BC", Names(new() { OffersInPerson = true, OfficeState = "ON" }));
}

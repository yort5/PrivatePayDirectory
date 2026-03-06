namespace PrivatePayDirectory.Core.Models;

public class OfficeLocation
{
    public string OfficeId { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

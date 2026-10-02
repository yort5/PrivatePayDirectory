namespace PrivatePayDirectory.Infrastructure.Repositories;

public class CosmosOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string AccountEndpoint { get; set; } = string.Empty;
    public string AccountKey { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "PrivatePayDirectory";
    public string ProvidersContainer { get; set; } = "Providers";
    public string UsersContainer { get; set; } = "Users";
}

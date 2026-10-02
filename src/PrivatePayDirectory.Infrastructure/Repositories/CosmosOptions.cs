namespace PrivatePayDirectory.Infrastructure.Repositories;

public class CosmosOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string AccountEndpoint { get; set; } = string.Empty;
    public string AccountKey { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "PrivatePayDirectory";
    // Physical container name kept as "Therapists" — renaming it would require a data migration
    public string ProvidersContainer { get; set; } = "Therapists";
    public string UsersContainer { get; set; } = "Users";
}

namespace PrivatePayDirectory.Infrastructure.Repositories;

public class CosmosOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "PrivatePayDirectory";
    public string TherapistsContainer { get; set; } = "Therapists";
    public string UsersContainer { get; set; } = "Users";
}

namespace PrivatePayDirectory.Infrastructure;

public class StorageOptions
{
    /// <summary>"Azure" (Cosmos DB + Blob Storage) or "Local" (JSON files + photos on disk, for local dev).</summary>
    public string Mode { get; set; } = "Azure";

    /// <summary>Directory for Local mode data. Relative paths resolve against the working directory.</summary>
    public string LocalDataPath { get; set; } = ".localdata";

    public bool IsLocal => string.Equals(Mode, "Local", StringComparison.OrdinalIgnoreCase);
}

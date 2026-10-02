using System.Text.Json;

namespace PrivatePayDirectory.Infrastructure.Local;

/// <summary>
/// A tiny document store backed by one JSON file — stands in for a Cosmos container in Local mode.
/// Documents are kept as JSON and deserialized on every read, so callers get independent copies
/// (matching Cosmos, where mutating a returned object does nothing until it's saved).
/// </summary>
public class LocalJsonStore<T>(string filePath, Func<T, string> getId)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(InfrastructureServiceExtensions.CosmosJsonOptions) { WriteIndented = true };

    private readonly Lock _lock = new();
    private Dictionary<string, JsonElement>? _docs;

    public T? Get(string id)
    {
        lock (_lock)
            return Docs.TryGetValue(id, out var doc) ? doc.Deserialize<T>(JsonOptions) : default;
    }

    public List<T> GetAll()
    {
        lock (_lock)
            return Docs.Values.Select(d => d.Deserialize<T>(JsonOptions)!).ToList();
    }

    public void Upsert(T item)
    {
        lock (_lock)
        {
            Docs[getId(item)] = JsonSerializer.SerializeToElement(item, JsonOptions);
            Flush();
        }
    }

    public void Delete(string id)
    {
        lock (_lock)
        {
            if (Docs.Remove(id)) Flush();
        }
    }

    private Dictionary<string, JsonElement> Docs => _docs ??= Load();

    private Dictionary<string, JsonElement> Load()
    {
        if (!File.Exists(filePath)) return [];
        var docs = JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(filePath), JsonOptions) ?? [];
        return docs.ToDictionary(d => getId(d.Deserialize<T>(JsonOptions)!));
    }

    private void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var tmp = filePath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(Docs.Values, JsonOptions));
        File.Move(tmp, filePath, overwrite: true);
    }
}

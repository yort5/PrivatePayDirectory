using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace PrivatePayDirectory.Infrastructure.Serialization;

/// <summary>
/// Plugs System.Text.Json into the Cosmos SDK so the whole app uses one serializer.
/// Cosmos requires its serializer to implement ToStream/FromStream over the raw HTTP body.
/// </summary>
public class CosmosSystemTextJsonSerializer(JsonSerializerOptions options) : CosmosSerializer
{
    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.CanSeek && stream.Length == 0)
                return default!;

            return JsonSerializer.Deserialize<T>(stream, options)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, input, options);
        stream.Position = 0;
        return stream;
    }
}

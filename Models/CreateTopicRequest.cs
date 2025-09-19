using System.Text.Json.Serialization;

namespace PubSubServer.Models;

public class CreateTopicRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;
}

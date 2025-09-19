using System.Text.Json.Serialization;

namespace PubSubServer.Models;

public class Message
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("payload")]
    public object Payload { get; set; } = default!;
}

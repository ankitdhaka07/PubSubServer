using System.Text.Json.Serialization;

namespace PubSubServer.Models;

public class ClientMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("message")]
    public Message? Message { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("last_n")]
    public int? LastN { get; set; }

    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }
}

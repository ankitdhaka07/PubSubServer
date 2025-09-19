using System.Text.Json.Serialization;

namespace PubSubServer.Models;

public class ServerMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("message")]
    public Message? Message { get; set; }

    [JsonPropertyName("error")]
    public ErrorInfo? Error { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("ts")]
    public string? Ts { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }
}

using System.Text.Json.Serialization;

namespace PubSubServer.Models;

public class ErrorInfo
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = default!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;
}

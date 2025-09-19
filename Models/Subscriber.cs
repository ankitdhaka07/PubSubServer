using System.Net.WebSockets;
using System.Threading.Channels;

namespace PubSubServer.Models;

public class Subscriber
{
    public string ClientId { get; set; } = default!;
    public WebSocket Socket { get; set; } = default!;
    public Channel<ServerMessage> Queue { get; set; } = Channel.CreateBounded<ServerMessage>(100);
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public HashSet<string> SubscribedTopics { get; set; } = new();
}

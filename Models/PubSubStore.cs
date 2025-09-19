using System.Collections.Concurrent;

namespace PubSubServer.Models;
public class PubSubStore
{
    public ConcurrentDictionary<string, Topic> Topics { get; } = new();
    public DateTime StartTime { get; } = DateTime.UtcNow;
}

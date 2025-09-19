using System.Collections.Concurrent;

namespace PubSubServer.Models;

public class Topic
{
    public string Name { get; set; } = default!;
    public ConcurrentDictionary<string, Subscriber> Subscribers { get; set; } = new();
    public Queue<ServerMessage> MessageHistory { get; set; } = new();
    public int MessageCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    private readonly object _historyLock = new object();

    public void AddMessage(ServerMessage message, int maxHistory = 100)
    {
        lock (_historyLock)
        {
            MessageHistory.Enqueue(message);
            MessageCount++;

            // Keep only last maxHistory messages
            while (MessageHistory.Count > maxHistory)
            {
                MessageHistory.Dequeue();
            }
        }
    }

    public List<ServerMessage> GetLastMessages(int count)
    {
        lock (_historyLock)
        {
            return MessageHistory.TakeLast(count).ToList();
        }
    }
}

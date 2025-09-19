using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PubSubServer.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace PubSubServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TopicsController : ControllerBase
{

    private readonly PubSubStore _store;

    public TopicsController(PubSubStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Create a new topic
    /// </summary>
    [HttpPost]
    public IActionResult CreateTopic([FromBody] CreateTopicRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
        {
            return BadRequest(new { error = "Topic name is required" });
        }

        var topic = new Topic { Name = request.Name };
        if (_store.Topics.TryAdd(request.Name, topic))
        {
            return CreatedAtAction(
                nameof(GetTopic),
                new { name = request.Name },
                new { status = "created", topic = request.Name }
            );
        }
        else
        {
            return Conflict(new { error = "Topic already exists", topic = request.Name });
        }
    }

    /// <summary>
    /// Get all topics with subscriber counts
    /// </summary>
    [HttpGet]
    public IActionResult GetTopics()
    {
        var topics = _store.Topics.Values.Select(t => new
        {
            name = t.Name,
            subscribers = t.Subscribers.Count,
            messages = t.MessageCount,
            created_at = t.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        }).ToArray();

        return Ok(new { topics });
    }

    /// <summary>
    /// Get specific topic information
    /// </summary>
    [HttpGet("{name}")]
    public IActionResult GetTopic(string name)
    {
        if (_store.Topics.TryGetValue(name, out var topic))
        {
            return Ok(new
            {
                name = topic.Name,
                subscribers = topic.Subscribers.Count,
                messages = topic.MessageCount,
                created_at = topic.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }
        else
        {
            return NotFound(new { error = "Topic not found", topic = name });
        }
    }

    /// <summary>
    /// Delete a topic and notify all subscribers
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteTopic(string name)
    {
        if (_store.Topics.TryRemove(name, out var topic))
        {
            // Notify all subscribers that topic is deleted
            var infoMessage = new ServerMessage
            {
                Type = "info",
                Topic = name,
                Msg = "topic_deleted",
                Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            // Send notification to all subscribers
            var notificationTasks = new List<Task>();
            foreach (var subscriber in topic.Subscribers.Values)
            {
                notificationTasks.Add(NotifySubscriberAsync(subscriber, infoMessage, name));
            }

            await Task.WhenAll(notificationTasks);

            return Ok(new { status = "deleted", topic = name });
        }
        else
        {
            return NotFound(new { error = "Topic not found", topic = name });
        }
    }

    private async Task NotifySubscriberAsync(Subscriber subscriber, ServerMessage message, string topicName)
    {
        try
        {
            if (subscriber.Socket.State == WebSocketState.Open)
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await subscriber.Socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }

            subscriber.SubscribedTopics.Remove(topicName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error notifying subscriber {subscriber.ClientId}: {ex.Message}");
        }
    }
}
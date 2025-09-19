using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PubSubServer.Models;

namespace PubSubServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase

{
    private readonly PubSubStore _store;

    public HealthController(PubSubStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get health status of the PubSub service
    /// </summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            uptime_sec = (DateTime.UtcNow - _store.StartTime).TotalSeconds,
            topics = _store.Topics.Count,
            subscribers = _store.Topics.Values.Sum(t => t.Subscribers.Count),
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        });
    }

    /// <summary>
    /// Get detailed statistics
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var topicStats = _store.Topics.ToDictionary(
            kvp => kvp.Key,
            kvp => new
            {
                messages = kvp.Value.MessageCount,
                subscribers = kvp.Value.Subscribers.Count,
                created_at = kvp.Value.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        );

        return Ok(new
        {
            topics = topicStats,
            summary = new
            {
                total_topics = _store.Topics.Count,
                total_subscribers = _store.Topics.Values.Sum(t => t.Subscribers.Count),
                total_messages = _store.Topics.Values.Sum(t => t.MessageCount),
                uptime_sec = (DateTime.UtcNow - _store.StartTime).TotalSeconds
            }
        });
    }
}

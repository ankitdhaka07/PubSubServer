using PubSubServer.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace PubSubServer;

public class WebSocketHandler
{
    private static readonly ConcurrentDictionary<string, Subscriber> _allSubscribers = new();

    public static async Task HandleWebSocketAsync(WebSocket socket, PubSubStore store)
    {
        var buffer = new byte[1024 * 4];
        Subscriber? currentSubscriber = null;

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"📥 Received: {json}");

                    try
                    {
                        var clientMessage = JsonSerializer.Deserialize<ClientMessage>(json);
                        if (clientMessage == null)
                        {
                            await SendErrorAsync(socket, null, "BAD_REQUEST", "Invalid JSON format");
                            continue;
                        }

                        var response = await ProcessMessageAsync(clientMessage, socket, store, currentSubscriber);

                        // Update current subscriber reference if this was a subscribe operation
                        if (clientMessage.Type == "subscribe" && response?.Type == "ack")
                        {
                            currentSubscriber = _allSubscribers.Values.FirstOrDefault(s => s.Socket == socket);
                        }
                        else if (clientMessage.Type == "unsubscribe" && clientMessage.ClientId != null)
                        {
                            // Remove from subscriber tracking if fully unsubscribed
                            if (currentSubscriber?.SubscribedTopics.Count == 0)
                            {
                                _allSubscribers.TryRemove(clientMessage.ClientId, out _);
                                currentSubscriber = null;
                            }
                        }

                        if (response != null)
                        {
                            await SendMessageAsync(socket, response);
                        }
                    }
                    catch (JsonException)
                    {
                        await SendErrorAsync(socket, null, "BAD_REQUEST", "Invalid JSON format");
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CleanupSubscriber(currentSubscriber, store);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }
        catch (WebSocketException)
        {
            // Connection closed unexpectedly
            await CleanupSubscriber(currentSubscriber, store);
        }
    }

    private static async Task<ServerMessage?> ProcessMessageAsync(ClientMessage message, WebSocket socket, PubSubStore store, Subscriber? currentSubscriber)
    {
        return message.Type switch
        {
            "subscribe" => await HandleSubscribeAsync(message, socket, store),
            "unsubscribe" => await HandleUnsubscribeAsync(message, store, currentSubscriber),
            "publish" => await HandlePublishAsync(message, store),
            "ping" => HandlePing(message),
            _ => new ServerMessage
            {
                Type = "error",
                RequestId = message.RequestId,
                Error = new ErrorInfo { Code = "BAD_REQUEST", Message = $"Unknown message type: {message.Type}" },
                Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        };
    }

    private static async Task<ServerMessage> HandleSubscribeAsync(ClientMessage message, WebSocket socket, PubSubStore store)
    {
        if (string.IsNullOrEmpty(message.Topic) || string.IsNullOrEmpty(message.ClientId))
        {
            return new ServerMessage
            {
                Type = "error",
                RequestId = message.RequestId,
                Error = new ErrorInfo { Code = "BAD_REQUEST", Message = "topic and client_id are required for subscribe" },
                Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        // Get or create topic
        var topic = store.Topics.GetOrAdd(message.Topic, _ => new Topic { Name = message.Topic });

        // Get or create subscriber
        var subscriber = _allSubscribers.GetOrAdd(message.ClientId, _ => new Subscriber
        {
            ClientId = message.ClientId,
            Socket = socket
        });

        // Update socket reference (in case of reconnection)
        subscriber.Socket = socket;
        subscriber.SubscribedTopics.Add(message.Topic);

        // Add subscriber to topic
        topic.Subscribers[message.ClientId] = subscriber;

        // Send historical messages if requested
        if (message.LastN > 0)
        {
            var historicalMessages = topic.GetLastMessages(message.LastN.Value);
            foreach (var histMsg in historicalMessages)
            {
                await SendMessageAsync(socket, histMsg);
            }
        }

        // Start message pump for this subscriber if not already running
        _ = Task.Run(async () => await MessagePump(subscriber));

        return new ServerMessage
        {
            Type = "ack",
            RequestId = message.RequestId,
            Topic = message.Topic,
            Status = "ok",
            Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    private static async Task<ServerMessage> HandleUnsubscribeAsync(ClientMessage message, PubSubStore store, Subscriber? currentSubscriber)
    {
        if (string.IsNullOrEmpty(message.Topic) || string.IsNullOrEmpty(message.ClientId))
        {
            return new ServerMessage
            {
                Type = "error",
                RequestId = message.RequestId,
                Error = new ErrorInfo { Code = "BAD_REQUEST", Message = "topic and client_id are required for unsubscribe" },
                Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        if (store.Topics.TryGetValue(message.Topic, out var topic))
        {
            topic.Subscribers.TryRemove(message.ClientId, out _);
        }

        if (currentSubscriber != null)
        {
            currentSubscriber.SubscribedTopics.Remove(message.Topic);
        }

        return new ServerMessage
        {
            Type = "ack",
            RequestId = message.RequestId,
            Topic = message.Topic,
            Status = "ok",
            Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    private static async Task<ServerMessage> HandlePublishAsync(ClientMessage message, PubSubStore store)
    {
        if (string.IsNullOrEmpty(message.Topic) || message.Message == null)
        {
            return new ServerMessage
            {
                Type = "error",
                RequestId = message.RequestId,
                Error = new ErrorInfo { Code = "BAD_REQUEST", Message = "topic and message are required for publish" },
                Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        if (!store.Topics.TryGetValue(message.Topic, out var topic))
        {
            return new ServerMessage
            {
                Type = "error",
                RequestId = message.RequestId,
                Error = new ErrorInfo { Code = "TOPIC_NOT_FOUND", Message = $"Topic '{message.Topic}' not found" },
                Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        // Create event message for subscribers
        var eventMessage = new ServerMessage
        {
            Type = "event",
            Topic = message.Topic,
            Message = message.Message,
            Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        // Add to topic history
        topic.AddMessage(eventMessage);

        // Send to all subscribers
        var tasks = new List<Task>();
        foreach (var subscriber in topic.Subscribers.Values)
        {
            tasks.Add(EnqueueMessageForSubscriber(subscriber, eventMessage));
        }

        await Task.WhenAll(tasks);

        // Return ack to publisher
        return new ServerMessage
        {
            Type = "ack",
            RequestId = message.RequestId,
            Topic = message.Topic,
            Status = "ok",
            Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    private static ServerMessage HandlePing(ClientMessage message)
    {
        return new ServerMessage
        {
            Type = "pong",
            RequestId = message.RequestId,
            Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    private static async Task EnqueueMessageForSubscriber(Subscriber subscriber, ServerMessage message)
    {
        try
        {
            if (!subscriber.Queue.Writer.TryWrite(message))
            {
                // Queue is full, send SLOW_CONSUMER error
                var errorMessage = new ServerMessage
                {
                    Type = "error",
                    Error = new ErrorInfo { Code = "SLOW_CONSUMER", Message = "Subscriber queue overflow" },
                    Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                await SendMessageAsync(subscriber.Socket, errorMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error enqueueing message for subscriber {subscriber.ClientId}: {ex.Message}");
        }
    }

    private static async Task MessagePump(Subscriber subscriber)
    {
        try
        {
            await foreach (var message in subscriber.Queue.Reader.ReadAllAsync())
            {
                if (subscriber.Socket.State == WebSocketState.Open)
                {
                    await SendMessageAsync(subscriber.Socket, message);
                }
                else
                {
                    break; // Socket closed, stop pumping
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Message pump error for subscriber {subscriber.ClientId}: {ex.Message}");
        }
    }

    private static async Task SendMessageAsync(WebSocket socket, ServerMessage message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);

            Console.WriteLine($"📤 Sending: {json}");
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending message: {ex.Message}");
        }
    }

    private static async Task SendErrorAsync(WebSocket socket, string? requestId, string code, string message)
    {
        var errorMessage = new ServerMessage
        {
            Type = "error",
            RequestId = requestId,
            Error = new ErrorInfo { Code = code, Message = message },
            Ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        await SendMessageAsync(socket, errorMessage);
    }

    private static async Task CleanupSubscriber(Subscriber? subscriber, PubSubStore store)
    {
        if (subscriber == null) return;

        // Remove from all topics
        foreach (var topicName in subscriber.SubscribedTopics.ToList())
        {
            if (store.Topics.TryGetValue(topicName, out var topic))
            {
                topic.Subscribers.TryRemove(subscriber.ClientId, out _);
            }
        }

        // Remove from global subscribers
        _allSubscribers.TryRemove(subscriber.ClientId, out _);

        // Close the message queue
        subscriber.Queue.Writer.Complete();
    }
}

using Microsoft.OpenApi.Models;
using PubSubServer;
using PubSubServer.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<PubSubStore>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pub/Sub Server API",
        Version = "v1",
        Description = "A simple Pub/Sub system with REST + WebSockets",
    });
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pub/Sub API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root "/"
    });
}

app.UseWebSockets();
app.UseRouting();
app.MapControllers();
app.MapGet("/", () => "🚀 PubSub Server - WebSocket: /ws | REST API: /api/topics | Health: /api/health | Docs: /swagger");
// ✅ REST API Endpoints

// ✅ WebSocket Endpoint
app.Map("/ws", async (HttpContext context, PubSubStore store) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await WebSocketHandler.HandleWebSocketAsync(socket, store);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
app.Run();

# PubSub Server 🚀

A real-time publish-subscribe messaging system built with **ASP.NET Core 9.0**, featuring both **REST API** and **WebSocket** support.

## 🌟 Features

- **Real-time messaging** via WebSockets
- **REST API** for topic management
- **Topic-based subscriptions**
- **Message history retrieval**
- **Health monitoring**
- **Swagger documentation**
- **Dockerized** deployment

## 🛠️ Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) (for local development)
- [Docker](https://www.docker.com/) (for containerized deployment)

## 🚀 Quick Start

### Option 1: Run with Docker (Recommended)

1. **Clone the repository:**

   ```bash
   git clone <your-repository-url>
   cd PubSubServer
   ```

2. **Build the Docker image:**

   ```bash
   docker build -t pubsubserver:latest .
   ```

3. **Run the container:**
   ```bash
   docker run -d -p 8080:8080 --name pubsubserver pubsubserver:latest
   ```

The server will be available at: `http://localhost:8080`

## 📚 API Documentation

Once the server is running, access the interactive Swagger documentation at:

- **Swagger UI:** `http://localhost:8080/swagger`
- **API Docs JSON:** `http://localhost:8080/swagger/v1/swagger.json`

## 🔗 Endpoints

### Health & Status

| Method | Endpoint            | Description               |
| ------ | ------------------- | ------------------------- |
| `GET`  | `/api/health`       | Get service health status |
| `GET`  | `/api/health/stats` | Get detailed statistics   |

### Topic Management

| Method   | Endpoint             | Description             |
| -------- | -------------------- | ----------------------- |
| `POST`   | `/api/topics`        | Create a new topic      |
| `GET`    | `/api/topics`        | List all topics         |
| `GET`    | `/api/topics/{name}` | Get specific topic info |
| `DELETE` | `/api/topics/{name}` | Delete a topic          |

### WebSocket

| Endpoint | Description                                  |
| -------- | -------------------------------------------- |
| `/ws`    | WebSocket connection for real-time messaging |

## 📡 WebSocket Usage

Connect to `ws://localhost:8080/ws` and send JSON messages:

### Client Message Types

#### 1. Subscribe to Topic

```json
{
  "type": "subscribe",
  "topic": "my-topic",
  "client_id": "client-123"
}
```

#### 2. Publish Message

```json
{
  "type": "publish",
  "topic": "my-topic",
  "message": {
    "content": "Hello World!",
    "sender": "user-123"
  },
  "client_id": "client-123"
}
```

#### 3. Get Message History

```json
{
  "type": "history",
  "topic": "my-topic",
  "last_n": 10,
  "client_id": "client-123"
}
```

#### 4. Unsubscribe from Topic

```json
{
  "type": "unsubscribe",
  "topic": "my-topic",
  "client_id": "client-123"
}
```

### Server Response Types

- **ack**: Acknowledgment of successful operation
- **message**: Published message from another client
- **history**: Historical messages
- **info**: System notifications
- **error**: Error responses

## 🧪 Testing Examples

### 1. Test Health Endpoint

```bash
curl http://localhost:8080/api/health
```

**Response:**

```json
{
  "status": "healthy",
  "uptime_sec": 123.45,
  "topics": 2,
  "subscribers": 5,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### 2. Create a Topic

```bash
curl -X POST http://localhost:8080/api/topics \
  -H "Content-Type: application/json" \
  -d '{"name": "chat-room"}'
```

**Response:**

```json
{
  "status": "created",
  "topic": "chat-room"
}
```

### 3. List All Topics

```bash
curl http://localhost:8080/api/topics
```

**Response:**

```json
{
  "topics": [
    {
      "name": "chat-room",
      "subscribers": 0,
      "messages": 0,
      "created_at": "2024-01-15T10:30:00Z"
    }
  ]
}
```

## 🐳 Docker Commands Reference

### Build and Run

```bash
# Build image
docker build -t pubsubserver:latest .

# Run container
docker run -d -p 8080:8080 --name pubsubserver pubsubserver:latest

# Check logs
docker logs pubsubserver

# Check status
docker ps
```

### Stop and Clean Up

```bash
# Stop container
docker stop pubsubserver

# Remove container
docker rm pubsubserver

# Remove image
docker rmi pubsubserver:latest
```

### Debugging

```bash
# Interactive shell in container
docker exec -it pubsubserver /bin/bash

# Follow logs in real-time
docker logs -f pubsubserver
```

## 🔧 Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Set to `Development` for Swagger UI
- `ASPNETCORE_URLS`: Server binding URLs (default: `http://+:8080`)

## 📊 Message Flow

1. **Client connects** to WebSocket (`/ws`)
2. **Client subscribes** to topic(s)
3. **Other clients publish** messages to topics
4. **Server broadcasts** messages to all subscribers
5. **Clients receive** real-time notifications

## 🔍 Monitoring

### Health Check

The container includes a built-in health check that monitors `/api/health` endpoint every 30 seconds.

### Logs

Application logs are available via Docker logs:

```bash
docker logs pubsubserver
```

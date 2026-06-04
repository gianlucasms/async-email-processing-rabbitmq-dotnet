# Async Email Processing with RabbitMQ + .NET 10

This project demonstrates an asynchronous email processing architecture using RabbitMQ and a .NET 10 Worker Service.

The main goal is to decouple email sending from the main application flow, improving scalability, performance, and resilience.

---

## Architecture Overview

The system follows a **producer-consumer pattern**:


API (Producer) → RabbitMQ → Worker → Email Service → ACK


- The API publishes messages when a user is created
- RabbitMQ acts as a message broker
- The Worker consumes messages asynchronously
- The Email Service processes the request (simulated)
- Messages are acknowledged after processing

---

## Technologies

- .NET 10 (Web API + Worker Service)
- RabbitMQ
- Docker & Docker Compose

---

## Project Structure


src/
├── UserApi/ → API responsible for publishing messages
├── EmailWorker/ → Background worker that consumes messages
└── Shared/ → Shared contracts and configurations


---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/SEU_USUARIO/NOME_DO_REPO.git](https://github.com/gianlucasms/async-email-processing-rabbitmq-dotnet.git
cd async-email-processing-rabbitmq-dotnet
2. Start RabbitMQ (Docker)
docker compose up -d

RabbitMQ Management UI:

URL: http://localhost:15672
User: guest
Password: guest

3. Run the API
cd src/UserApi
dotnet run

4. Run the Worker
cd src/EmailWorker
dotnet run

How It Works
A request is sent to the API to create a user
The API publishes a message to RabbitMQ
The message is stored in a queue (email-sending)
The Worker consumes the message
The Email Service processes it (simulated)
The message is acknowledged (ACK)

Example Message
{
  "userId": "11111111-1111-1111-1111-111111111111",
  "email": "teste@email.com",
  "name": "teste",
  "occurredAt": "2026-03-25T12:00:00"
}

Key Concepts Demonstrated
Asynchronous processing
Message-based communication
Producer / Consumer pattern
Decoupling between services
Background processing with Worker Service

Notes
Email sending is simulated (no real SMTP integration)
Focus is on architecture and message flow

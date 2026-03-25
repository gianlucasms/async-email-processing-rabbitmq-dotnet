using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Configuration;
using Shared.Events;
using UserApi.Models;
using UserApi.Services;

namespace UserApi.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IRabbitMqPublisher _publisher;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IRabbitMqPublisher publisher,
        IOptions<RabbitMqSettings> settings,
        ILogger<UsersController> logger)
    {
        _publisher = publisher;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        // Simulate persistence by generating a new user ID and logging the registration
        var userId = Guid.NewGuid();
        _logger.LogInformation("User registered — Id: {UserId}, Name: {Name}, Email: {Email}",
            userId, request.Name, request.Email);

        var userCreatedEvent = new UserCreatedEvent(
            UserId: userId,
            Name: request.Name,
            Email: request.Email,
            OccurredAt: DateTime.UtcNow);

        await _publisher.PublishAsync(_settings.QueueName, userCreatedEvent, cancellationToken);

        return CreatedAtAction(
            actionName: nameof(CreateUser),
            value: new { userId, request.Name, request.Email, message = "User registered. Welcome email queued." });
    }
}

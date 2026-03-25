using Shared.Events;

namespace EmailWorker.Services;

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(UserCreatedEvent userCreatedEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EmailService] Preparing welcome email for {Name} <{Email}> (UserId: {UserId})",
            userCreatedEvent.Name, userCreatedEvent.Email, userCreatedEvent.UserId);

        // Simulate email sending latency with a delay
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        _logger.LogInformation(
            "[EmailService] Welcome email successfully sent to {Email} — event occurred at {OccurredAt:O}",
            userCreatedEvent.Email, userCreatedEvent.OccurredAt);
    }
}

using Shared.Events;

namespace EmailWorker.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(UserCreatedEvent userCreatedEvent, CancellationToken cancellationToken = default);
}

namespace Shared.Events;

public sealed record UserCreatedEvent(
    Guid UserId,
    string Name,
    string Email,
    DateTime OccurredAt);

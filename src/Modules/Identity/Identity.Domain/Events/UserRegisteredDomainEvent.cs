using SharedKernel;

namespace Identity.Domain.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : IDomainEvent;

using MediatR;

namespace Application.UseCases.Users.Events;
public record UserCreatedEvent(Guid UserId, string Username) : INotification;
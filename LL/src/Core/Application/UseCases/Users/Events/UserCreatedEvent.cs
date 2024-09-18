using MediatR;

namespace Application.UseCases.Users.Events;
public record UserCreatedEvent(string UserId, string Username) : INotification;
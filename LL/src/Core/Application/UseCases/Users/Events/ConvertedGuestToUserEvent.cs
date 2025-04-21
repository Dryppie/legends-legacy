using MediatR;

namespace Application.UseCases.Users.Events;
public record ConvertedGuestToUserEvent(string UserId, string Username) : INotification;
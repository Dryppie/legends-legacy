using MediatR;

namespace Application.UseCases.Users.Events;
public record ConvertedGuestToUserEvent(Guid UserId, string CharacterName) : INotification;

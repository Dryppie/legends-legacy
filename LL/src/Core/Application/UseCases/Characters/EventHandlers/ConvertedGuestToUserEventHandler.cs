using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Users.Events;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;

public class ConvertedGuestToUserEventHandler : INotificationHandler<ConvertedGuestToUserEvent>
{
    private readonly ICharacterService _characterService;
    private readonly IPublisher _publisher;
    public ConvertedGuestToUserEventHandler(ICharacterService characterService, IMediator publisher)
    {
        _characterService = characterService;
        _publisher = publisher;
    }

    public async Task Handle(ConvertedGuestToUserEvent convertedGuestToUserEvent, CancellationToken cancellationToken)
    {
        await _characterService.UpdateCharacterNameAsync(convertedGuestToUserEvent.UserId, convertedGuestToUserEvent.Username, cancellationToken);
    }
}
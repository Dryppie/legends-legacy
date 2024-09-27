using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Events;
using Application.UseCases.Users.Events;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;

public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ICharacterService _characterService;
    private readonly IPublisher _publisher;
    public UserCreatedEventHandler(ICharacterService characterService, IMediator publisher)
    {
        _characterService = characterService;
        _publisher = publisher;
    }

    public async Task Handle(UserCreatedEvent userCreatedEvent, CancellationToken cancellationToken)
    {
        var character = await _characterService.CreateCharacterAsync(userCreatedEvent.UserId, userCreatedEvent.Username, cancellationToken);
        await _publisher.Publish(new CharacterCreatedEvent(character.Id), cancellationToken);
    }
}
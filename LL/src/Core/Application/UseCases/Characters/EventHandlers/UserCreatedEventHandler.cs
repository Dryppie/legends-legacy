using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Characters.Events;
using Application.UseCases.Outbox;
using Application.UseCases.Users.Events;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;

public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ICharacterService _characterService;
    private readonly IGameEventOutbox _outbox;
    private readonly IPublisher _publisher;

    public UserCreatedEventHandler(
        ICharacterService characterService,
        IGameEventOutbox outbox,
        IMediator publisher)
    {
        _characterService = characterService;
        _outbox = outbox;
        _publisher = publisher;
    }

    public async Task Handle(UserCreatedEvent userCreatedEvent, CancellationToken cancellationToken)
    {
        var character = await _characterService.CreateCharacterAsync(userCreatedEvent.UserId, userCreatedEvent.CharacterName, cancellationToken);
        await _outbox.EnqueueAsync(
            GameEventTypes.CharacterCreated,
            new CharacterCreatedPayload(character.Id),
            character.Id,
            character.UserId,
            cancellationToken);
        await _publisher.Publish(new CharacterCreatedEvent(character.Id), cancellationToken);
    }
}

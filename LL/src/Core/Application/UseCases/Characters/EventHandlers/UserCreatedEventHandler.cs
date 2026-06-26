using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Characters.Events;
using Application.UseCases.Users.Events;
using MediatR;

namespace Application.UseCases.Characters.EventHandlers;

public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ICharacterService _characterService;
    private readonly IAchievementService _achievementService;
    private readonly IPublisher _publisher;

    public UserCreatedEventHandler(
        ICharacterService characterService,
        IAchievementService achievementService,
        IMediator publisher)
    {
        _characterService = characterService;
        _achievementService = achievementService;
        _publisher = publisher;
    }

    public async Task Handle(UserCreatedEvent userCreatedEvent, CancellationToken cancellationToken)
    {
        var character = await _characterService.CreateCharacterAsync(userCreatedEvent.UserId, userCreatedEvent.Username, cancellationToken);
        await _achievementService.RecordCharacterCreatedAsync(character.Id, cancellationToken);
        await _publisher.Publish(new CharacterCreatedEvent(character.Id), cancellationToken);
    }
}

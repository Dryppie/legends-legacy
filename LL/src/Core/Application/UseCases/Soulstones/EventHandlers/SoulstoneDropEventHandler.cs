using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.WebSockets;
using Application.UseCases.Soulstones.Events;
using Application.WebSockets.Contracts;
using MediatR;

namespace Application.UseCases.Soulstones.EventHandlers;
public class SoulstoneDropEventHandler : INotificationHandler<SoulstoneDropEvent>
{
    private readonly ICharacterService _characterService;
    private readonly IGameEventPublisher _eventPublisher;

    public SoulstoneDropEventHandler(
        ICharacterService characterService,
        IGameEventPublisher eventPublisher)
    {
        _characterService = characterService;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(SoulstoneDropEvent notification, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetCharacterByCharacterIdAsync(notification.CharacterId, cancellationToken);
        if (character == null) return;

        character.Soulstones += notification.SoulstonesEarned;

        await _eventPublisher.PublishAsync(
            new Audience.Character(notification.CharacterId),
            new SoulstoneDropMsg(
                notification.CharacterId,
                notification.SoulstonesEarned,
                character.Soulstones));
    }
}

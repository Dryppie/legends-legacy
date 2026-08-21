using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Soulstones.Events;
using MediatR;

namespace Application.UseCases.Soulstones.EventHandlers;
public class SoulstoneDropEventHandler : INotificationHandler<SoulstoneDropEvent>
{
    private readonly ICharacterService _characterService;

    public SoulstoneDropEventHandler(ICharacterService characterService)
    {
        _characterService = characterService;
    }

    public async Task Handle(SoulstoneDropEvent notification, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetCharacterByCharacterIdAsync(notification.CharacterId, cancellationToken);
        if (character == null) return;

        character.Soulstones += notification.SoulstonesEarned;
    }
}

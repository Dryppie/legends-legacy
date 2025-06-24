using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Soulstones.Events;
using MediatR;

namespace Application.UseCases.Soulstones.EventHandlers;
public class SoulstoneDropEventHandler : INotificationHandler<SoulstoneDropEvent>
{
    private readonly ICharacterService _characterService;
    private readonly ILootService _lootService;

    public SoulstoneDropEventHandler(ICharacterService characterService, ILootService lootService)
    {
        _characterService = characterService;
        _lootService = lootService;
    }

    public async Task Handle(SoulstoneDropEvent notification, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetCharacterByCharacterIdAsync(notification.CharacterId, cancellationToken);
        if (character == null) return;

        character.Soulstones += notification.SoulstonesEarned;
        await _characterService.SaveChangesAsync(cancellationToken);
    }
}

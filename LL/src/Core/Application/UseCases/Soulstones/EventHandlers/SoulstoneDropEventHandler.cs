using Application.Interfaces.Services.LL;
using Application.UseCases.Soulstones.Events;
using Application.UseCases.Soulstones.Providers;
using Domain.Extensions.Soulstones;
using MediatR;

namespace Application.UseCases.Soulstones.EventHandlers;
public class SoulstoneDropEventHandler : INotificationHandler<SoulstoneDropEvent>
{
    private readonly ICharacterService _characterService;
    private readonly ILootService _lootService;
    private readonly SoulstoneUpgradeDefinitionProvider _defs;

    public SoulstoneDropEventHandler(ICharacterService characterService, ILootService lootService, SoulstoneUpgradeDefinitionProvider defs)
    {
        _characterService = characterService;
        _lootService = lootService;
        _defs = defs;
    }

    public async Task Handle(SoulstoneDropEvent notification, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetCharacterWithSoulstoneUpgradesAsync(notification.CharacterId, cancellationToken);
        if (character == null) return;

        double dropRate = character.CharacterSoulstoneUpgrades.GetStatBonus(_defs.All, "SoulstoneDropRate");
        double doubleDropChance = character.CharacterSoulstoneUpgrades.GetStatBonus(_defs.All, "SoulstoneDoubleDropChance");

        var soulstonesEarned = _lootService.GenerateSoulstoneLoot(notification.DurationInSeconds, dropRate, doubleDropChance);
        if (soulstonesEarned < 1) return;

        character.Soulstones += soulstonesEarned;
        await _characterService.SaveChangesAsync(cancellationToken);
    }
}

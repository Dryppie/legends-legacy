using Application.Interfaces.Services.LL;
using Domain.Extensions.Soulstones;
using Domain.Models.Entities.Characters;
using Domain.Models.Soulstones;
using Domain.Models.Soulstones.UpgradeDefinition;

namespace Services.LL.Soulstones;
public class SoulstoneUpgradeService : ISoulstoneUpgradeService
{
    private readonly ICharacterService _characterService;
    private readonly IReadOnlyDictionary<string, SoulstoneUpgradeDefinition> _defs;

    public SoulstoneUpgradeService(ICharacterService characterService, SoulstoneUpgradeDefinitionProvider provider)
    {
        _characterService = characterService;
        _defs = provider.All;
    }

    public async Task<List<SoulstoneUpgradeView>> GetForCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetCharacterWithSoulstoneUpgradesAsync(characterId, cancellationToken);
        if (character == null) return [];
        var levels = character.CharacterSoulstoneUpgrades.ToDictionary(u => u.SoulstoneUpgradeDefinitionId, u => u.Level);

        return _defs.Values.Select(def =>
        {
            levels.TryGetValue(def.Id, out var lvl);

            var next = lvl < def.MaxLevel ? def.Cost.CostOfLevel(lvl + 1) : (int?)null;
            return new SoulstoneUpgradeView(def, lvl, next);
        }).ToList();
    }

    public async Task<bool> PurchaseAsync(Guid characterId, string upgradeId, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetCharacterWithSoulstoneUpgradesAsync(characterId, cancellationToken);
        if (character == null) return false;
        if (!_defs.TryGetValue(upgradeId, out var def)) return false;

        var entry = character.CharacterSoulstoneUpgrades.FirstOrDefault(u => u.SoulstoneUpgradeDefinitionId == upgradeId);
        var current = entry?.Level ?? 0;

        if (current >= def.MaxLevel) return false;

        var cost = def.Cost.CostOfLevel(current + 1);
        character.Soulstones += 10;
        if (!TrySpendSoulstones(character, cost)) return false;

        if (entry is null)
            character.CharacterSoulstoneUpgrades.Add(new CharacterSoulstoneUpgrade
            {
                CharacterId = characterId,
                SoulstoneUpgradeDefinitionId = upgradeId,
                Level = 1
            });
        else
            entry.Level++;

        await _characterService.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool TrySpendSoulstones(Character character, int cost)
    {
        if (character.Soulstones < cost) return false;
        character.Soulstones -= cost;
        return true;
    }
}

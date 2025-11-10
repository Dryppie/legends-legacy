using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Domain.Extensions.Soulstones;
using Domain.Models.Entities.Characters;
using Domain.Models.Soulstones;
using Domain.Models.Soulstones.UpgradeDefinition;
using Services.LL.Providers;

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

        return true;
    }

    private static bool TrySpendSoulstones(Character character, int cost)
    {
        if (character.Soulstones < cost) return false;
        character.Soulstones -= cost;
        return true;
    }

    public async Task<bool> ResetSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetCharacterWithSoulstoneUpgradesAsync(characterId, cancellationToken);
        if (character == null) return false;

        var upgrades = character.CharacterSoulstoneUpgrades;
        if (upgrades.Count == 0) return true; // Nothing to reset

        int totalRefund = 0;

        foreach (var upgrade in upgrades)
        {
            if (_defs.TryGetValue(upgrade.SoulstoneUpgradeDefinitionId, out var def))
            {
                for (int level = 1; level <= upgrade.Level; level++)
                {
                    totalRefund += def.Cost.CostOfLevel(level);
                }
            }
        }

        character.CharacterSoulstoneUpgrades.Clear(); // Remove all upgrades
        character.Soulstones += totalRefund;         // Refund total cost

        return true;
    }
}

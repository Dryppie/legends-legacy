using Application.Interfaces.Services.LL;
using Application.UseCases.Soulstones.Providers;
using Domain.Extensions.Soulstones;
using Domain.Models.Entities.Characters;
using Domain.Models.Soulstones;
using Domain.Models.Soulstones.UpgradeDefinition;

namespace Services.LL.Soulstones;
public class SoulstoneUpgradeService : ISoulstoneUpgradeService
{
    private readonly ICharacterService _characterService;
    private readonly ISoulstoneUpgradeRepository _soulstoneUpgradeRepository;
    private readonly IReadOnlyDictionary<string, SoulstoneUpgradeDefinition> _defs;

    public SoulstoneUpgradeService(ICharacterService characterService, ISoulstoneUpgradeRepository soulstoneUpgradeRepository, SoulstoneUpgradeDefinitionProvider provider)
    {
        _characterService = characterService;
        _soulstoneUpgradeRepository = soulstoneUpgradeRepository;
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

    public async Task<Dictionary<string, double>> GetSoulstoneBonusesByCharacterIdAsync(Guid characterId, string[] upgrades, CancellationToken cancellationToken)
    {
        string[] wantedIds;

        if (upgrades.Length > 0)
        {
            wantedIds = [.. _defs.Values
                             .Where(d => upgrades.Contains(d.Effect.Stat))
                             .Select(d => d.Id)];

            if (wantedIds.Length == 0)
                return [];
        }
        else
        {
            wantedIds = [];
        }

        var soulstoneUpgrades = await _soulstoneUpgradeRepository.GetSoulstoneUpgradesByCharacterIdAsync(characterId, wantedIds, cancellationToken);

        var bonuses = soulstoneUpgrades
            .Select(u =>
            {
                if (!_defs.TryGetValue(u.SoulstoneUpgradeDefinitionId, out var def))
                    return null;             // unknown ID – ignore

                var effect = def.Effect;     // **single effect**
                var value = effect.PerLevel * u.Level;
                return new { effect.Stat, value };
            })
            .Where(x => x != null)
            .ToDictionary(g => g!.Stat,
                            g => g!.value,
                            StringComparer.OrdinalIgnoreCase);

        return bonuses;
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

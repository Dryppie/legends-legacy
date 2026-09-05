using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Achievements;
using Common.Primitives;
using Domain.Models.Entities.Characters;
using Domain.Models.Soulstones;
using Domain.Models.Soulstones.UpgradeDefinition;
using Services.LL.Providers;
using Services.LL.Extensions;
using System.Globalization;

namespace Services.LL.Soulstones;

public sealed class SoulstoneUpgradeService : ISoulstoneUpgradeService
{
    private readonly ISoulstoneUpgradeRepository _repository;
    private readonly SoulstoneUpgradeDefinitionProvider _provider;
    private readonly IAchievementService? _achievementService;

    public SoulstoneUpgradeService(
        ISoulstoneUpgradeRepository repository,
        SoulstoneUpgradeDefinitionProvider provider,
        IAchievementService? achievementService = null)
    {
        _repository = repository;
        _provider = provider;
        _achievementService = achievementService;
    }

    public async Task<List<SoulstoneUpgradeView>> GetForCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _repository.GetCharacterAsync(characterId, cancellationToken);
        if (character is null)
        {
            return [];
        }

        return BuildViews(character);
    }

    public async Task<Response<SoulstoneUpgradeMutationResult>> PurchaseAsync(
        Guid characterId,
        string upgradeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(upgradeId))
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("Choose a Soulstone constellation upgrade.");
        }

        var defs = _provider.All;
        if (!defs.TryGetValue(upgradeId, out var def))
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("Soulstone constellation upgrade was not found.");
        }

        if (!def.Enabled)
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("This Soulstone constellation is not available yet.");
        }

        var character = await _repository.GetCharacterAsync(characterId, cancellationToken);
        if (character is null)
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("Character was not found.");
        }

        var entry = character.CharacterSoulstoneUpgrades
            .FirstOrDefault(u => u.SoulstoneUpgradeDefinitionId.Equals(def.Id, StringComparison.OrdinalIgnoreCase));
        var currentRank = Math.Clamp(entry?.Level ?? 0, 0, def.MaxRank);

        if (currentRank >= def.MaxRank)
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("This Soulstone constellation is already at its maximum rank.");
        }

        var nextRank = currentRank + 1;
        var missingRequirement = GetMissingRequirement(def, character);
        if (missingRequirement is not null)
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail($"Requires {missingRequirement} first.");
        }

        var cost = def.CostsByRank[currentRank];
        if (!TrySpendSoulstones(character, cost))
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("Not enough Soulstones.");
        }

        if (entry is null)
        {
            character.CharacterSoulstoneUpgrades.Add(new CharacterSoulstoneUpgrade
            {
                CharacterId = characterId,
                SoulstoneUpgradeDefinitionId = def.Id,
                Level = 1
            });
        }
        else
        {
            entry.Level = nextRank;
        }

        if (_achievementService is not null)
        {
            var upgrades = BuildViews(character).ToList();
            await _achievementService.RecordSoulstoneUpgradePurchasedAsync(
                characterId,
                upgrades.Count > 0 && upgrades.All(x => x.CurrentRank >= x.MaxRank),
                cancellationToken);
        }

        return Response<SoulstoneUpgradeMutationResult>.Success(new SoulstoneUpgradeMutationResult(
            BuildViews(character),
            character.Soulstones));
    }

    public async Task<Response<SoulstoneUpgradeMutationResult>> ResetSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _repository.GetCharacterAsync(characterId, cancellationToken);
        if (character is null)
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("Character was not found.");
        }

        var totalRefund = character.CharacterSoulstoneUpgrades.Sum(x => GetRefundForUpgrade(x));
        var balance = checked(character.Soulstones + totalRefund);

        _repository.Remove(character, character.CharacterSoulstoneUpgrades.ToArray());
        character.Soulstones = balance;

        return Response<SoulstoneUpgradeMutationResult>.Success(new SoulstoneUpgradeMutationResult(
            BuildViews(character),
            character.Soulstones,
            totalRefund));
    }

    private List<SoulstoneUpgradeView> BuildViews(Character character)
    {
        var levels = character.CharacterSoulstoneUpgrades
            .GroupBy(u => u.SoulstoneUpgradeDefinitionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Max(x => x.Level), StringComparer.OrdinalIgnoreCase);

        return _provider.All.Values
            .Where(def => def.Enabled)
            .OrderBy(def => def.Branch)
            .ThenBy(def => def.SortOrder)
            .ThenBy(def => def.DisplayName)
            .Select(def =>
            {
                levels.TryGetValue(def.Id, out var currentRank);
                return BuildView(def, Math.Clamp(currentRank, 0, def.MaxRank), character.Soulstones, character);
            })
            .ToList();
    }

    private static SoulstoneUpgradeView BuildView(
        SoulstoneUpgradeDefinition def,
        int currentRank,
        long availableSoulstones,
        Character character)
    {
        var nextRank = currentRank + 1;
        var isMaxed = currentRank >= def.MaxRank;
        int? nextCost = isMaxed ? null : def.CostsByRank[currentRank];
        var missingRequirement = GetMissingRequirement(def, character);

        string? disabledReason = null;
        if (isMaxed)
            disabledReason = "Max rank reached.";
        else if (missingRequirement is not null)
            disabledReason = $"Requires {missingRequirement}.";
        else if (nextCost > availableSoulstones)
            disabledReason = "Not enough Soulstones.";

        return new SoulstoneUpgradeView(
            def.Id,
            def.Branch,
            def.DisplayName,
            def.Description,
            currentRank,
            def.MaxRank,
            currentRank == 0 ? "No active effect." : FormatEffects(def, currentRank),
            isMaxed ? null : FormatEffects(def, nextRank),
            nextCost,
            !isMaxed && disabledReason is null,
            disabledReason,
            def.AppliesTo,
            def.DoesNotApplyTo,
            def.CostsByRank.Take(currentRank).Sum(),
            def.SortOrder,
            def.FrontendHint);
    }

    private int GetRefundForUpgrade(CharacterSoulstoneUpgrade upgrade)
    {
        var rank = Math.Max(0, upgrade.Level);
        if (rank == 0)
        {
            return 0;
        }

        if (_provider.All.TryGetValue(upgrade.SoulstoneUpgradeDefinitionId, out var def))
        {
            return def.CostsByRank.Take(Math.Min(rank, def.MaxRank)).Sum();
        }

        return 0;
    }

    private static string? GetMissingRequirement(SoulstoneUpgradeDefinition def, Character character)
    {
        if (def.RequiresUpgradeIds is not { Count: > 0 })
        {
            return null;
        }

        var owned = character.CharacterSoulstoneUpgrades
            .Where(u => u.Level > 0)
            .Select(u => u.SoulstoneUpgradeDefinitionId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return def.RequiresUpgradeIds.FirstOrDefault(required => !owned.Contains(required));
    }

    private static string FormatEffects(SoulstoneUpgradeDefinition def, int rank)
    {
        return string.Join("; ", def.Effects.Select(effect => FormatEffect(effect, rank)));
    }

    private static string FormatEffect(SoulstoneUpgradeEffect effect, int rank)
    {
        var value = effect.ValuesByRank[Math.Clamp(rank, 1, effect.ValuesByRank.Count) - 1];
        var percent = FormatPercent(value);

        return effect.Kind switch
        {
            SoulstoneUpgradeEffectKind.EssenceDropRateRelativeBps => $"+{percent}% Essence drop rate",
            SoulstoneUpgradeEffectKind.EssencePityProgressionGainBps => $"+{percent}% Essence pity progression",
            SoulstoneUpgradeEffectKind.DuplicateEssenceExtraMaterialChanceBps => $"{percent}% duplicate Essence material chance",
            SoulstoneUpgradeEffectKind.FocusedMonsterEssenceDropRateRelativeBps => $"+{percent}% focused monster Essence drop rate",
            SoulstoneUpgradeEffectKind.CombatExperienceGainBps => $"+{percent}% combat EXP",
            SoulstoneUpgradeEffectKind.IdleCombatDefeatExperienceRetentionBps => $"Retain {percent}% idle defeat EXP",
            SoulstoneUpgradeEffectKind.DungeonSigilDropRateRelativeBps => $"+{percent}% dungeon sigil chance",
            SoulstoneUpgradeEffectKind.DungeonRewardRetentionBps => $"+{percent}% retained Rest Site rewards",
            _ => $"+{value.ToString(CultureInfo.InvariantCulture)}"
        };
    }

    private static string FormatPercent(int basisPoints)
    {
        return basisPoints.ToPercent().ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static bool TrySpendSoulstones(Character character, int cost)
    {
        if (character.Soulstones < cost)
        {
            return false;
        }

        character.Soulstones -= cost;
        return true;
    }
}

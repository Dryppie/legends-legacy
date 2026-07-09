using Application.Common.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
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
    private static readonly IReadOnlySet<string> LegacyUpgradeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "combat.essence.drop.rate",
        "combat.double.exp.chance",
        "gathering.double.drop.chance",
        "gathering.double.exp.chance",
        "crafting.double.item.exp.chance",
        "crafting.negative.outcome",
        "misc.soulstone.drop.rate",
        "misc.soulstone.double.drop.chance"
    };

    private readonly ICharacterService _characterService;
    private readonly ICharacterProgressionService _progressionService;
    private readonly SoulstoneUpgradeDefinitionProvider _provider;
    private readonly IDbContext _dbContext;

    public SoulstoneUpgradeService(
        ICharacterService characterService,
        ICharacterProgressionService progressionService,
        SoulstoneUpgradeDefinitionProvider provider,
        IDbContext dbContext)
    {
        _characterService = characterService;
        _progressionService = progressionService;
        _provider = provider;
        _dbContext = dbContext;
    }

    public async Task<List<SoulstoneUpgradeView>> GetForCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetCharacterWithSoulstoneUpgradesAsync(characterId, cancellationToken);
        if (character is null)
        {
            return [];
        }

        return await BuildViewsAsync(character, cancellationToken);
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
        if (!defs.TryGetValue(upgradeId, out var def) || LegacyUpgradeIds.Contains(upgradeId))
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("This legacy Soulstone upgrade is no longer purchasable. Reset your Soulstones to refund old ranks.");
        }

        if (!def.Enabled)
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("This Soulstone constellation is not available yet.");
        }

        var character = await _characterService.GetCharacterWithSoulstoneUpgradesAsync(characterId, cancellationToken);
        if (character is null)
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("Character was not found.");
        }

        var highestRegion = await _progressionService.GetHighestUnlockedRegionAsync(characterId, cancellationToken);
        var entry = character.CharacterSoulstoneUpgrades
            .FirstOrDefault(u => u.SoulstoneUpgradeDefinitionId.Equals(def.Id, StringComparison.OrdinalIgnoreCase));
        var currentRank = Math.Clamp(entry?.Level ?? 0, 0, def.MaxRank);

        if (currentRank >= def.MaxRank)
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("This Soulstone constellation is already at its maximum rank.");
        }

        var nextRank = currentRank + 1;
        var rankCap = GetRankCap(def, highestRegion);
        if (nextRank > rankCap)
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail($"Reach region {GetRequiredRegionForRank(def, nextRank)} to unlock the next rank.");
        }

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

        var startedTransaction = _dbContext.CurrentTransaction is null;
        await using var transaction = startedTransaction
            ? await _dbContext.BeginTransactionAsync(cancellationToken)
            : null;

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

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return Response<SoulstoneUpgradeMutationResult>.Success(new SoulstoneUpgradeMutationResult(
            await BuildViewsAsync(character, cancellationToken),
            character.Soulstones));
    }

    public async Task<Response<SoulstoneUpgradeMutationResult>> ResetSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetCharacterWithSoulstoneUpgradesAsync(characterId, cancellationToken);
        if (character is null)
        {
            return Response<SoulstoneUpgradeMutationResult>.Fail("Character was not found.");
        }

        var totalRefund = character.CharacterSoulstoneUpgrades.Sum(GetRefundForUpgrade);

        var startedTransaction = _dbContext.CurrentTransaction is null;
        await using var transaction = startedTransaction
            ? await _dbContext.BeginTransactionAsync(cancellationToken)
            : null;

        character.CharacterSoulstoneUpgrades.Clear();
        character.Soulstones += totalRefund;

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return Response<SoulstoneUpgradeMutationResult>.Success(new SoulstoneUpgradeMutationResult(
            await BuildViewsAsync(character, cancellationToken),
            character.Soulstones,
            totalRefund));
    }

    private async Task<List<SoulstoneUpgradeView>> BuildViewsAsync(Character character, CancellationToken cancellationToken)
    {
        var levels = character.CharacterSoulstoneUpgrades
            .Where(u => !LegacyUpgradeIds.Contains(u.SoulstoneUpgradeDefinitionId))
            .GroupBy(u => u.SoulstoneUpgradeDefinitionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Max(x => x.Level), StringComparer.OrdinalIgnoreCase);
        var highestRegion = await _progressionService.GetHighestUnlockedRegionAsync(character.Id, cancellationToken);

        return _provider.All.Values
            .Where(def => def.Enabled)
            .OrderBy(def => def.Branch)
            .ThenBy(def => def.SortOrder)
            .ThenBy(def => def.DisplayName)
            .Select(def =>
            {
                levels.TryGetValue(def.Id, out var currentRank);
                return BuildView(def, Math.Clamp((int)currentRank, 0, def.MaxRank), character.Soulstones, highestRegion, character);
            })
            .ToList();
    }

    private static SoulstoneUpgradeView BuildView(
        SoulstoneUpgradeDefinition def,
        int currentRank,
        long availableSoulstones,
        int highestRegion,
        Character character)
    {
        var nextRank = currentRank + 1;
        var isMaxed = currentRank >= def.MaxRank;
        int? nextCost = isMaxed ? null : def.CostsByRank[currentRank];
        var rankCap = GetRankCap(def, highestRegion);
        var isRegionCapped = !isMaxed && nextRank > rankCap;
        var missingRequirement = GetMissingRequirement(def, character);

        string? disabledReason = null;
        if (isMaxed)
            disabledReason = "Max rank reached.";
        else if (isRegionCapped)
            disabledReason = $"Requires region {GetRequiredRegionForRank(def, nextRank)}.";
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
            isRegionCapped,
            isRegionCapped ? GetRequiredRegionForRank(def, nextRank) : null,
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

        return GetLegacyRefund(upgrade.SoulstoneUpgradeDefinitionId, rank);
    }

    private static int GetLegacyRefund(string upgradeId, int level)
    {
        if (!LegacyUpgradeIds.Contains(upgradeId))
        {
            return 0;
        }

        var cappedCost = upgradeId.StartsWith("misc.soulstone.", StringComparison.OrdinalIgnoreCase);
        if (!cappedCost || level <= 50)
        {
            return level * (level + 1) / 2;
        }

        return 1275 + ((level - 50) * 50);
    }

    private static int GetRankCap(SoulstoneUpgradeDefinition def, int highestRegion)
    {
        if (def.RegionRankCaps is { Count: > 0 })
        {
            return def.RegionRankCaps
                .Where(cap => highestRegion >= cap.MinRegion)
                .DefaultIfEmpty(new SoulstoneRegionRankCap(1, 0))
                .Max(cap => cap.MaxRank);
        }

        return highestRegion switch
        {
            <= 2 => Math.Min(2, def.MaxRank),
            <= 4 => Math.Min(3, def.MaxRank),
            <= 7 => Math.Min(4, def.MaxRank),
            _ => def.MaxRank
        };
    }

    private static int GetRequiredRegionForRank(SoulstoneUpgradeDefinition def, int rank)
    {
        if (def.RegionRankCaps is { Count: > 0 })
        {
            return def.RegionRankCaps
                .Where(cap => cap.MaxRank >= rank)
                .OrderBy(cap => cap.MinRegion)
                .Select(cap => cap.MinRegion)
                .FirstOrDefault();
        }

        return rank switch
        {
            <= 2 => 1,
            3 => 3,
            4 => 5,
            _ => 8
        };
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
            SoulstoneUpgradeEffectKind.EssenceDropRateRelativeBps => $"+{percent}% relative Essence drop rate",
            SoulstoneUpgradeEffectKind.EssencePityProgressionGainBps => $"+{percent}% Essence pity progression",
            SoulstoneUpgradeEffectKind.DuplicateEssenceExtraMaterialChanceBps => $"{percent}% duplicate Essence material chance",
            SoulstoneUpgradeEffectKind.FocusedMonsterEssenceDropRateRelativeBps => $"+{percent}% focused monster Essence drop rate",
            SoulstoneUpgradeEffectKind.CombatExperienceGainBps => $"+{percent}% combat EXP",
            SoulstoneUpgradeEffectKind.AreaCommitmentCombatExperienceGainBps => $"+{percent}% area commitment combat EXP",
            SoulstoneUpgradeEffectKind.IdleCombatDefeatExperienceRetentionBps => $"Retain {percent}% idle defeat EXP",
            SoulstoneUpgradeEffectKind.GatheringYieldBps => $"+{percent}% gathered material yield",
            SoulstoneUpgradeEffectKind.GatheringExperienceGainBps => $"+{percent}% gathering EXP",
            SoulstoneUpgradeEffectKind.GatheringRareDropChanceRelativeBps => $"+{percent}% relative rare gathering chance",
            SoulstoneUpgradeEffectKind.CraftingExperienceGainBps => $"+{percent}% crafting EXP",
            SoulstoneUpgradeEffectKind.TemperingNegativeOutcomeReductionBps => $"-{percent} percentage points negative tempering chance",
            SoulstoneUpgradeEffectKind.TemperingFailMaterialRefundChanceBps => $"{percent}% tempering material refund chance",
            SoulstoneUpgradeEffectKind.BlueprintProgressionGainBps => $"+{percent}% blueprint progression",
            SoulstoneUpgradeEffectKind.SigilFragmentDropRateRelativeBps => $"+{percent}% relative sigil fragment chance",
            SoulstoneUpgradeEffectKind.DungeonRewardRetentionBps => $"+{percent}% retained checkpoint rewards",
            SoulstoneUpgradeEffectKind.DungeonRoomPreviewTier => $"Room preview tier {value}",
            SoulstoneUpgradeEffectKind.DungeonRewardFocusTier => $"Dungeon reward focus tier {value}",
            SoulstoneUpgradeEffectKind.ArchivePresetSlotCount => $"+{value} archive preset slots",
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

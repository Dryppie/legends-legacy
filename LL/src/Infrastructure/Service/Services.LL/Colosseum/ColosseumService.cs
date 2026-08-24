using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Colosseum;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Leaderboards;
using Domain.Models.Snapshots;
using Domain.Models.Essences;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Services.LL.Colosseum;
public class ColosseumService : IColosseumService
{
    private static readonly TimeSpan SameDefenderCooldown = TimeSpan.FromMinutes(2);

    private readonly IEntityService _entityService;
    private readonly ICharacterService _characterService;
    private readonly ICombatSetupService _combatSetupService;
    private readonly IColosseumRepository _colosseumRepository;
    private readonly ICombatEngineExecutor _combatEngineExecutor;
    private readonly ICombatEncounterResultFactory _combatEncounterResultFactory;
    private readonly IRatingService _ratingService;
    private readonly ICharacterSnapshotService _characterSnapshotService;
    private readonly IItemBaseRepository _itemBaseRepository;
    private readonly IChampionMarketCatalog _championMarketCatalog;
    private readonly IInventoryService _inventoryService;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IAchievementService? _achievementService;

    public ColosseumService(
        IEntityService es,
        ICharacterService cs,
        ICombatSetupService css,
        IColosseumRepository cr,
        ICombatEngineExecutor combatEngineExecutor,
        ICombatEncounterResultFactory combatEncounterResultFactory,
        IRatingService rs,
        ICharacterSnapshotService characterSnapshotService,
        IItemBaseRepository itemBaseRepository,
        IChampionMarketCatalog championMarketCatalog,
        IInventoryService inventoryService,
        IInventoryItemFactory inventoryItemFactory,
        IAchievementService? achievementService = null)
    {
        _entityService = es;
        _characterService = cs;
        _combatSetupService = css;
        _colosseumRepository = cr;
        _combatEngineExecutor = combatEngineExecutor;
        _combatEncounterResultFactory = combatEncounterResultFactory;
        _ratingService = rs;
        _characterSnapshotService = characterSnapshotService;
        _itemBaseRepository = itemBaseRepository;
        _championMarketCatalog = championMarketCatalog;
        _inventoryService = inventoryService;
        _inventoryItemFactory = inventoryItemFactory;
        _achievementService = achievementService;
    }

    public async Task<StartArenaBattleResult?> StartArenaBattle(Guid characterId, Guid enemyId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var arenaTicketStatus = await GetArenaTicketStatusAsync(characterId, cancellationToken);
        if (characterId == enemyId || arenaTicketStatus.CurrentTickets < 1) return null;

        var attacker = await _colosseumRepository.GetArenaCharacterAsync(characterId, cancellationToken);
        var defender = await _colosseumRepository.GetArenaCharacterAsync(enemyId, cancellationToken);
        if (attacker is null || defender is null || attacker.UserId == defender.UserId) return null;

        var (eligibleOpponents, _) = await _colosseumRepository.GetArenaOpponentsWithRating(characterId, cancellationToken);
        if (eligibleOpponents.All(opponent => opponent.Id != enemyId)) return null;

        if (await _colosseumRepository.HasRecentMatchAsync(characterId, enemyId, now.Subtract(SameDefenderCooldown), cancellationToken))
            return null;

        var defenderSnapshot = await _colosseumRepository.GetArenaDefenseSnapshotAsync(enemyId, cancellationToken);

        var playerTeam = await _entityService.GetEntitiesByIdsForCombatAsync([characterId], cancellationToken);
        if (playerTeam.Count == 0) return null;
        var enemyTeam = await _entityService.GetEntitiesByIdsForCombatAsync([enemyId], cancellationToken);
        if (enemyTeam.Count == 0) return null;

        var combatPlayerEntities = _combatSetupService.CreatePlayerCombatEntities(playerTeam);
        var combatEnemyEntities = await CreateDefenderCombatEntitiesAsync(enemyTeam, defenderSnapshot, cancellationToken);
        await _combatSetupService.PrepareEntitiesForCombat(
            [.. combatPlayerEntities, .. combatEnemyEntities],
            EssenceCombatActivity.Arena);

        var encounterPlan = CreateArenaEncounterPlan(
            characterId,
            enemyId,
            now);
        arenaTicketStatus.CurrentTickets--;
        _colosseumRepository.UpdateArenaTicketStatus(arenaTicketStatus);

        var runtime = new CombatEncounterRuntime(
            encounterPlan,
            [
                new CombatRuntimeParticipant(
                    encounterPlan.FriendlyParticipants.Single(),
                    playerTeam.Single(),
                    combatPlayerEntities.Single())
            ],
            [
                new CombatRuntimeParticipant(
                    encounterPlan.HostileParticipants.Single(),
                    enemyTeam.Single(),
                    combatEnemyEntities.Single())
            ]);

        var combatResult = await _combatEngineExecutor.ExecuteAsync(runtime, cancellationToken);
        combatResult = _combatEncounterResultFactory.Create(runtime, combatResult).CombatResult;

        var attackerArena = attacker.ArenaProfile;
        var defenderArena = defender.ArenaProfile;
        var attackerRankBefore = ArenaRank.GetProgress(attackerArena.Rating);
        var defenderRatingBefore = defenderArena.Rating;
        var ratingResult = ApplyRatings(attacker, defender, combatResult.Outcome);
        var attackerRankAfter = ArenaRank.GetProgress(attackerArena.Rating);

        var streakBefore = attackerArena.CurrentAttackWinStreak;
        ApplyRecordsAndStreak(attacker, defender, combatResult.Outcome);
        var (baseGlory, firstWinBonus) = ApplyAttackGlory(attacker, combatResult.Outcome, now);

        var matchResult = new ColosseumMatchResult
        {
            Id = encounterPlan.EncounterId,
            CharacterAId = characterId,
            CharacterAName = attacker.Name,
            CharacterARatingBefore = ratingResult.CharacterARatingBefore,
            CharacterARatingAfter = ratingResult.CharacterARatingAfter,
            CharacterARatingDelta = ratingResult.CharacterADelta,
            CharacterAGloryEarned = baseGlory + firstWinBonus,
            CharacterAStreakBefore = streakBefore,
            CharacterAStreakAfter = attackerArena.CurrentAttackWinStreak,

            CharacterBId = enemyId,
            CharacterBName = defender.Name,
            CharacterBRatingBefore = defenderRatingBefore,
            CharacterBRatingAfter = ratingResult.CharacterBRatingAfter,
            CharacterBRatingDelta = ratingResult.CharacterBDelta,
            CharacterBGloryEarned = 0,

            WinnerId = combatResult.Outcome == BattleOutcome.Victory ? characterId : combatResult.Outcome == BattleOutcome.Defeat ? enemyId : null,
            WinnerName = combatResult.Outcome == BattleOutcome.Victory ? attacker.Name : combatResult.Outcome == BattleOutcome.Defeat ? defender.Name : string.Empty,
            Outcome = ToHistoryOutcome(combatResult.Outcome),
            PlayedAt = now
        };

        await _colosseumRepository.SaveArenaMatchResult(matchResult, cancellationToken);

        return new StartArenaBattleResult(
            encounterPlan.EncounterId,
            combatResult,
            arenaTicketStatus,
            matchResult,
            attackerRankBefore,
            attackerRankAfter,
            new ArenaOpponentPreview
            {
                Opponent = defender,
                RatingDelta = _ratingService.Preview(ratingResult.CharacterARatingBefore, ratingResult.CharacterBRatingBefore)
            },
            baseGlory + firstWinBonus,
            baseGlory,
            firstWinBonus,
            0,
            streakBefore,
            attackerArena.CurrentAttackWinStreak);
    }

    private async Task<CombatEntity> CreateSnapshotCombatEntityAsync(Character sourceCharacter, CharacterSnapshot snapshot, CancellationToken cancellationToken)
    {
        var template = _combatSetupService.CreatePlayerCombatEntities([sourceCharacter]).Single();
        template.Name = snapshot.Name;
        template.Level = snapshot.Level;
        template.BaseAttributes = snapshot.BaseAttributes
            .Select(x => new Domain.Models.Attributes.EntityAttribute
            {
                EntityId = snapshot.CharacterId,
                AttributeType = x.AttributeType,
                Value = x.Value
            })
            .ToList();
        template.EquippedEssences = snapshot.EquippedEssences
            .OrderBy(x => x.SlotIndex)
            .Select(x => x.ToPlayerEssence(snapshot.CharacterId))
            .ToList();
        template.HasEquippedEssenceSnapshot = true;

        var itemBases = await _itemBaseRepository.GetItemBasesByIdsAsync(
            snapshot.Equipment.Select(x => x.ItemBaseId).Distinct().ToArray(),
            cancellationToken);

        template.Equipment = snapshot.Equipment
            .OrderBy(x => x.Slot)
            .Where(x => itemBases.ContainsKey(x.ItemBaseId))
            .Select(x => new EquipmentInstance
            {
                Id = x.EquipmentInstanceId,
                ItemBaseId = x.ItemBaseId,
                ItemBase = itemBases[x.ItemBaseId],
                BaseRecipeId = x.BaseRecipeId,
                EquipmentSetId = x.EquipmentSetId,
                Rarity = x.Rarity,
                Quality = x.Quality,
                Tier = x.Tier,
                StatModelVersion = x.StatModelVersion,
                Potential = x.Potential,
                ItemXp = x.ItemXp,
                IsMasterpiece = x.IsMasterpiece,
                IsLevelingItem = x.IsLevelingItem,
                InstanceModifiers = x.InstanceModifiers
                    .Select(modifier => modifier.ToInstanceModifier(x.EquipmentInstanceId))
                    .ToList()
            })
            .ToList();

        return template;
    }

    private async Task<List<CombatEntity>> CreateDefenderCombatEntitiesAsync(
        List<Domain.Models.Entities.Entity> enemyTeam,
        ArenaDefenseSnapshot? defenderSnapshot,
        CancellationToken cancellationToken)
    {
        if (defenderSnapshot is { IsValid: true, IsOutdated: false })
        {
            return
            [
                await CreateSnapshotCombatEntityAsync(
                    (Character)enemyTeam.Single(),
                    defenderSnapshot.CharacterSnapshot,
                    cancellationToken)
            ];
        }

        return _combatSetupService.CreatePlayerCombatEntities(enemyTeam);
    }

    private ColosseumRatingResult ApplyRatings(Character attacker, Character defender, BattleOutcome outcome)
    {
        var attackerArena = attacker.ArenaProfile;
        var defenderArena = defender.ArenaProfile;
        var attackerRatingBefore = attackerArena.Rating;
        var defenderRatingBefore = defenderArena.Rating;

        var preview = _ratingService.Preview(attackerArena.Rating, defenderArena.Rating);
        var defenderPreview = _ratingService.Preview(defenderArena.Rating, attackerArena.Rating);

        attackerArena.Rating = outcome switch
        {
            BattleOutcome.Victory => preview.RatingIfVictory,
            BattleOutcome.Draw => preview.RatingIfDraw,
            _ => preview.RatingIfDefeat
        };

        defenderArena.Rating = outcome switch
        {
            BattleOutcome.Victory => defenderPreview.RatingIfDefeat,
            BattleOutcome.Draw => defenderPreview.RatingIfDraw,
            _ => defenderPreview.RatingIfVictory
        };

        attackerArena.LifetimeHighestRating = Math.Max(attackerArena.LifetimeHighestRating, attackerArena.Rating);
        defenderArena.LifetimeHighestRating = Math.Max(defenderArena.LifetimeHighestRating, defenderArena.Rating);

        return new ColosseumRatingResult
        {
            CharacterARatingBefore = attackerRatingBefore,
            CharacterARatingAfter = attackerArena.Rating,
            CharacterBRatingBefore = defenderRatingBefore,
            CharacterBRatingAfter = defenderArena.Rating
        };
    }

    private static void ApplyRecordsAndStreak(Character attacker, Character defender, BattleOutcome outcome)
    {
        var attackerArena = attacker.ArenaProfile;
        var defenderArena = defender.ArenaProfile;

        switch (outcome)
        {
            case BattleOutcome.Victory:
                attackerArena.AttackWins++;
                defenderArena.DefenseLosses++;
                attackerArena.CurrentAttackWinStreak++;
                attackerArena.BestAttackWinStreak = Math.Max(attackerArena.BestAttackWinStreak, attackerArena.CurrentAttackWinStreak);
                break;
            case BattleOutcome.Draw:
                attackerArena.AttackDraws++;
                defenderArena.DefenseDraws++;
                attackerArena.CurrentAttackWinStreak = 0;
                break;
            default:
                attackerArena.AttackLosses++;
                defenderArena.DefenseWins++;
                attackerArena.CurrentAttackWinStreak = 0;
                break;
        }
    }

    private static (int BaseGlory, int DailyFirstWinBonus) ApplyAttackGlory(Character attacker, BattleOutcome outcome, DateTimeOffset now)
    {
        var (baseGlory, firstWinBonus) = ArenaRewards.CalculateAttackGlory(
            outcome,
            !HasReceivedFirstWinBonusToday(attacker, now));

        if (firstWinBonus > 0)
        {
            attacker.ArenaProfile.LastFirstWinBonusAt = now;
        }

        attacker.ArenaProfile.Glory += baseGlory + firstWinBonus;
        return (baseGlory, firstWinBonus);
    }

    private static bool HasReceivedFirstWinBonusToday(Character attacker, DateTimeOffset now)
    {
        return attacker.ArenaProfile.LastFirstWinBonusAt?.UtcDateTime.Date == now.UtcDateTime.Date;
    }

    private static string ToHistoryOutcome(BattleOutcome outcome)
    {
        return outcome switch
        {
            BattleOutcome.Victory => "AttackerWin",
            BattleOutcome.Draw => "Draw",
            _ => "DefenderWin"
        };
    }

    private static CombatEncounterPlan CreateArenaEncounterPlan(
        Guid characterId,
        Guid enemyId,
        DateTimeOffset startsAt)
    {
        var matchId = Guid.NewGuid();
        return new CombatEncounterPlan(
            EncounterId: matchId,
            Mode: CombatMode.Pvp,
            Sequence: 1,
            StartsAt: startsAt,
            Participants:
            [
                new CombatParticipantSlot(characterId.ToString(), characterId, CombatSide.Friendly),
                new CombatParticipantSlot(enemyId.ToString(), enemyId, CombatSide.Hostile)
            ],
            SourceContext: new PvpEncounterSourceContext(matchId, characterId, enemyId));
    }

    /// <summary>
    /// Get the opponents you are eligible to fight, including rating gains / losses
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<ArenaOpponentPreview>> GetArenaOpponents(Guid characterId, CancellationToken cancellationToken)
    {
        var (opponents, myRating) = await _colosseumRepository.GetArenaOpponentsWithRating(characterId, cancellationToken);
        var cooldownSince = DateTimeOffset.UtcNow.Subtract(SameDefenderCooldown);
        var recentMatchTimes = await _colosseumRepository.GetRecentAttackerMatchTimesAsync(
            characterId,
            opponents.Select(opponent => opponent.Id).ToArray(),
            cooldownSince,
            cancellationToken);

        return opponents
            .Select(opp => new ArenaOpponentPreview
            {
                Opponent = opp,
                RatingDelta = _ratingService.Preview(myRating, opp.ArenaProfile.Rating),
                ChallengeAvailableAt = recentMatchTimes.TryGetValue(opp.Id, out var lastPlayedAt)
                    ? lastPlayedAt.Add(SameDefenderCooldown)
                    : null
            })
            .ToList();
    }

    public async Task<ArenaDefenseSnapshot?> UpdateDefenseSnapshotAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _colosseumRepository.GetArenaCharacterAsync(characterId, cancellationToken);
        if (character is null) return null;

        var team = await _entityService.GetEntitiesByIdsForCombatAsync([characterId], cancellationToken);
        if (team.Count == 0) return null;

        var combatEntities = _combatSetupService.CreatePlayerCombatEntities(team);
        await _combatSetupService.PrepareEntitiesForCombat(combatEntities, EssenceCombatActivity.Arena);

        var snapshot = await _characterSnapshotService.CreateAsync(characterId, EssenceCombatActivity.Arena, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var defenseSnapshot = new ArenaDefenseSnapshot
        {
            CharacterId = characterId,
            CharacterSnapshotId = snapshot.Id,
            CharacterSnapshot = snapshot,
            LoadoutHash = CreateLoadoutHash(snapshot),
            IsValid = true,
            IsOutdated = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _colosseumRepository.SaveArenaDefenseSnapshotAsync(defenseSnapshot, cancellationToken);
        return defenseSnapshot;
    }

    public async Task<ArenaDefenseSnapshot?> GetArenaDefenseSnapshotAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _colosseumRepository.GetArenaDefenseSnapshotAsync(characterId, cancellationToken);
    }

    public IReadOnlyList<ChampionMarketItem> GetChampionMarketItems()
    {
        return _championMarketCatalog.GetActive(DateTimeOffset.UtcNow);
    }

    public async Task<ChampionMarketPurchaseResult?> PurchaseChampionMarketItemAsync(
        Guid characterId,
        string itemId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (quantity < 1) return null;

        var now = DateTimeOffset.UtcNow;
        var item = _championMarketCatalog
            .GetActive(now)
            .FirstOrDefault(x => x.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        if (item?.IsEnabled != true) return null;

        var character = await _colosseumRepository.GetArenaCharacterAsync(characterId, cancellationToken);
        if (character is null) return null;

        if (!MeetsMarketRequirement(character, item)) return null;

        var weeklyResetAt = ArenaCalendar.GetCurrentWeeklyResetStart(now);
        var weeklyPurchased = await _colosseumRepository.CountChampionMarketPurchasesAsync(characterId, item.Id, weeklyResetAt, cancellationToken);
        var lifetimePurchased = await _colosseumRepository.CountChampionMarketPurchasesAsync(characterId, item.Id, null, cancellationToken);

        if (item.WeeklyPurchaseLimit.HasValue && weeklyPurchased + quantity > item.WeeklyPurchaseLimit.Value) return null;
        if (item.LifetimePurchaseLimit.HasValue && lifetimePurchased + quantity > item.LifetimePurchaseLimit.Value) return null;

        var arena = character.ArenaProfile;
        var totalCost = item.GloryCost * quantity;
        if (arena.Glory < totalCost) return null;

        var rewardItemId = item.RewardItemId;
        var rewardItemName = item.RewardItemName;
        var rewardItemQuantity = checked(item.RewardItemQuantity * quantity);
        var rewardInventoryItems = new List<Domain.Models.Inventories.InventoryItem>();
        if (rewardItemQuantity > 0)
        {
            var itemBases = await _itemBaseRepository.GetItemBasesByIdsAsync([rewardItemId!], cancellationToken);
            if (!itemBases.TryGetValue(rewardItemId!, out var itemBase)) return null;

            rewardInventoryItems.AddRange(
                _inventoryItemFactory.CreateForQuantity(itemBase, rewardItemQuantity, characterId));
        }

        if (!string.IsNullOrWhiteSpace(item.RewardTitleKey))
        {
            if (_achievementService is null)
            {
                return null;
            }

            var titleUnlocked = await _achievementService.UnlockTitleAsync(
                character.UserId,
                characterId,
                item.RewardTitleKey,
                JsonSerializer.Serialize(new
                {
                    Source = ItemAcquisitionSources.ChampionMarket,
                    MarketItemId = item.Id
                }),
                cancellationToken);
            if (!titleUnlocked)
            {
                return null;
            }
        }

        arena.Glory -= totalCost;
        var cindersGranted = item.CindersGranted * quantity;
        var soulstonesGranted = item.SoulstonesGranted * quantity;
        var sigilFragmentsGranted = item.SigilFragmentsGranted * quantity;
        character.Cinders += cindersGranted;
        character.Soulstones += soulstonesGranted;
        character.SigilFragments += sigilFragmentsGranted;

        if (rewardInventoryItems.Count > 0)
        {
            await _inventoryService.AddItemsToInventory(
                characterId,
                rewardInventoryItems,
                ItemAcquisitionSources.ChampionMarket,
                cancellationToken);
        }

        await _colosseumRepository.SaveChampionMarketPurchaseAsync(new ChampionMarketPurchase
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            ItemId = item.Id,
            Quantity = quantity,
            GloryCostPaid = totalCost,
            PurchasedAt = now
        }, cancellationToken);
        if (_achievementService is not null)
        {
            await _achievementService.RecordChampionMarketPurchaseAsync(characterId, cancellationToken);
        }

        return new ChampionMarketPurchaseResult(
            item,
            quantity,
            totalCost,
            arena.Glory,
            cindersGranted,
            soulstonesGranted,
            sigilFragmentsGranted,
            rewardItemId,
            rewardItemName,
            rewardItemQuantity,
            rewardInventoryItems);
    }

    public async Task<int> CountChampionMarketPurchasesAsync(Guid characterId, string itemId, DateTimeOffset? since, CancellationToken cancellationToken)
    {
        return await _colosseumRepository.CountChampionMarketPurchasesAsync(characterId, itemId, since, cancellationToken);
    }

    public async Task<IReadOnlyList<ChampionMarketTitleGrant>> BackfillMissingChampionMarketTitleGrantsAsync(
        CancellationToken cancellationToken)
    {
        if (_achievementService is null)
        {
            return [];
        }

        var titleItems = _championMarketCatalog
            .GetAll()
            .Where(x => !string.IsNullOrWhiteSpace(x.RewardTitleKey))
            .ToDictionary(x => x.Id, x => x.RewardTitleKey!, StringComparer.OrdinalIgnoreCase);

        if (titleItems.Count == 0)
        {
            return [];
        }

        var purchases = await _colosseumRepository.GetChampionMarketPurchasesByItemIdsAsync(
            titleItems.Keys,
            cancellationToken);

        if (purchases.Count == 0)
        {
            return [];
        }

        var accountIds = await _colosseumRepository.GetAccountIdsForCharactersAsync(
            purchases.Select(x => x.CharacterId).Distinct().ToArray(),
            cancellationToken);

        var granted = new List<ChampionMarketTitleGrant>();
        var handled = new HashSet<(Guid CharacterId, string TitleKey)>();

        foreach (var purchase in purchases)
        {
            if (!titleItems.TryGetValue(purchase.ItemId, out var titleKey))
            {
                continue;
            }

            if (!handled.Add((purchase.CharacterId, titleKey)))
            {
                continue;
            }

            if (!accountIds.TryGetValue(purchase.CharacterId, out var accountId))
            {
                continue;
            }

            var unlocked = await _achievementService.UnlockTitleAsync(
                accountId,
                purchase.CharacterId,
                titleKey,
                JsonSerializer.Serialize(new
                {
                    Source = ItemAcquisitionSources.ChampionMarket,
                    MarketItemId = purchase.ItemId,
                    Backfilled = true,
                    OriginalPurchaseAt = purchase.PurchasedAt
                }),
                cancellationToken);

            if (unlocked)
            {
                granted.Add(new ChampionMarketTitleGrant(
                    purchase.CharacterId,
                    purchase.ItemId,
                    titleKey,
                    purchase.PurchasedAt));
            }
        }

        return granted;
    }

    private static bool MeetsMarketRequirement(Character character, ChampionMarketItem item)
    {
        var arena = character.ArenaProfile;
        if (item.RequiredRating.HasValue && arena.Rating < item.RequiredRating.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(item.RequiredRankTier))
        {
            var currentTier = ArenaRank.GetTier(arena.Rating);
            var requiredTier = ArenaRank.Tiers.FirstOrDefault(x => x.Id == item.RequiredRankTier);
            if (requiredTier is null || currentTier.SortOrder < requiredTier.SortOrder)
            {
                return false;
            }
        }

        return true;
    }

    private static string CreateLoadoutHash(CharacterSnapshot snapshot)
    {
        var payload = string.Join("|",
            snapshot.BaseAttributes.OrderBy(x => x.AttributeType).Select(x => $"a:{x.AttributeType}:{x.Value}")
                .Concat(snapshot.Equipment.OrderBy(x => x.Slot).Select(x => $"e:{x.Slot}:{x.ItemBaseId}:{x.Rarity}:{x.Potential}:{x.ItemXp}:{x.IsMasterpiece}:{x.IsLevelingItem}:{string.Join(",", x.InstanceModifiers.OrderBy(m => m.AttributeType).Select(m => $"{m.AttributeType}:{m.Amount}:{m.ModifierType}"))}"))
                .Concat(snapshot.EquippedEssences.OrderBy(x => x.SlotIndex).Select(x => $"s:{x.SlotIndex}:{x.EssenceDefinitionId}:{x.AscensionTier}:{x.IsEvolved}")));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    public async Task<Character?> GetArenaCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _colosseumRepository.GetArenaCharacterAsync(characterId, cancellationToken);
    }

    public async Task SaveArenaMatchResult(Guid characterId, Guid enemyId, BattleOutcome outcome, ColosseumRatingResult ratingResult, CancellationToken cancellationToken)
    {
        var characterA = await _characterService.GetBaseCharacterByIdAsync(characterId, cancellationToken);
        if (characterA == null) return;
        var characterB = await _characterService.GetBaseCharacterByIdAsync(enemyId, cancellationToken);
        if (characterB == null) return;

        var arenaMatchResult = new ColosseumMatchResult
        {
            CharacterAId = characterId,
            CharacterAName = characterA.Name,
            CharacterARatingBefore = ratingResult.CharacterARatingBefore,
            CharacterARatingAfter = ratingResult.CharacterARatingAfter,

            CharacterBId = enemyId,
            CharacterBName = characterB.Name,
            CharacterBRatingBefore = ratingResult.CharacterBRatingBefore,
            CharacterBRatingAfter = ratingResult.CharacterBRatingAfter,

            WinnerId = outcome == BattleOutcome.Victory ? characterId : outcome == BattleOutcome.Defeat ? enemyId : null,
            WinnerName = outcome == BattleOutcome.Victory ? characterA.Name : outcome == BattleOutcome.Defeat ? characterB.Name : "",
            PlayedAt = DateTimeOffset.UtcNow
        };

        await _colosseumRepository.SaveArenaMatchResult(arenaMatchResult, cancellationToken);
    }

    public async Task<List<ColosseumMatchResult>> GetColosseumMatchResults(Guid characterId, CancellationToken cancellationToken)
    {
        return await _colosseumRepository.GetColosseumMatchResults(characterId, cancellationToken);
    }

    public async Task<List<LeaderboardEntry>> GetRankings(Guid characterId, CancellationToken cancellationToken)
    {
        var characters = await _colosseumRepository.GetRankings(characterId, cancellationToken);

        // Assign the real rank to each character
        var rankedCharacters = characters
            .Select((character, index) => new { Character = character, Rank = index + 1 })
            .ToList();

        // Take the top 50
        var top50 = rankedCharacters.Take(50).ToList();

        // Check if requester is in the top 50
        var inTop50 = top50.Any(r => r.Character.Id == characterId);

        if (!inTop50)
        {
            // Find the requester's actual ranking
            var requesterRank = rankedCharacters.FirstOrDefault(r => r.Character.Id == characterId);
            if (requesterRank != null)
            {
                top50.Add(requesterRank); // Add requester to the bottom of the list, with their true rank
            }
        }

        // Create the result list
        var rankings = top50
            .Select(ranking => new LeaderboardEntry()
            {
                CharacterId = ranking.Character.Id,
                CharacterName = ranking.Character.Name,
                Level = ranking.Character.ArenaProfile.Rating,
                Rank = ranking.Rank,
            })
            .ToList();

        return rankings;
    }


    public async Task<ArenaTicketStatus> GetArenaTicketStatusAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var arenaTicketStatus = await _colosseumRepository.GetArenaTicketStatusAsync(characterId, cancellationToken);
        
        var restoreInterval = TimeSpan.FromHours(3);
        var timePassed = now - arenaTicketStatus.LastTicketUpdate;
        var ticketsToRestore = (int)(timePassed.TotalHours / restoreInterval.TotalHours);

        if (ticketsToRestore > 0)
        {
            arenaTicketStatus.CurrentTickets = Math.Min(arenaTicketStatus.CurrentTickets + ticketsToRestore, arenaTicketStatus.MaxTickets);
            // Update LastTicketUpdate based on restored tickets. Even if capped, a new ticket might still restore in..  17 minutes
            arenaTicketStatus.LastTicketUpdate = arenaTicketStatus.LastTicketUpdate.AddHours(ticketsToRestore * restoreInterval.TotalHours);

            _colosseumRepository.UpdateArenaTicketStatus(arenaTicketStatus);
        }

        return arenaTicketStatus;
    }
}

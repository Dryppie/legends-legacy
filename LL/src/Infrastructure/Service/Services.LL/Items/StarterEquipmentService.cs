using Application.Interfaces.Services.LL.Items;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Quests;
using Microsoft.Extensions.Options;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Items;

public sealed class StarterEquipmentService(StarterEquipmentCatalog catalog, IStarterEquipmentRepository repository,
    IQuestRepository quests, IItemBaseRepository itemBases, ILootRewardWriter rewards,
    IOptions<EquipmentProgressionOptions> options) : IStarterEquipmentService
{
    public async Task<EquipmentAccess> GetAccessAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var flags = options.Value;
        var starters = new List<StarterEquipmentAccess>();
        if (flags.StarterAcquisitionEnabled)
        {
            foreach (var kind in Enum.GetValues<StarterEquipmentGrantKind>())
            {
                var grant = await repository.GetGrantAsync(characterId, kind, cancellationToken);
                var prerequisite = kind == StarterEquipmentGrantKind.FirstWeapon
                    ? "quest.onboarding.soul_archive" : "quest.onboarding.first_weapon";
                var progress = await quests.GetProgressAsync(characterId, prerequisite, cancellationToken);
                var reason = grant is not null ? "Already claimed."
                    : progress?.Status != QuestStatus.Completed ? "Complete the preceding onboarding quest first."
                    : !await repository.HasInventoryAsync(characterId, cancellationToken) ? "Character inventory was not found."
                    : null;
                starters.Add(new(kind, reason is null, reason, grant));
            }
        }
        return new(flags.StarterAcquisitionEnabled, flags.ForgeEnabled, flags.ProtectedAcquisitionEnabled,
            flags.BaselineRecoveryEnabled, flags.OrdinaryAcquisitionEnabled, starters);
    }

    public IReadOnlyList<StarterEquipmentOption> GetOptions() =>
        options.Value.StarterAcquisitionEnabled ? catalog.Options : [];

    public async Task<StarterEquipmentClaimResult> ClaimAsync(Guid characterId, StarterEquipmentGrantKind kind,
        IReadOnlyList<string> definitionIds, CancellationToken cancellationToken)
    {
        if (!options.Value.StarterAcquisitionEnabled)
            return StarterEquipmentClaimResult.Fail("Starter equipment is not available yet.");
        if (characterId == Guid.Empty || !Enum.IsDefined(kind) || definitionIds is null)
            return StarterEquipmentClaimResult.Fail("Invalid starter equipment request.");

        // Read the frozen grant before consulting mutable content or quest requirements.
        var existing = await repository.GetGrantAsync(characterId, kind, cancellationToken);
        if (existing is not null)
            return kind == StarterEquipmentGrantKind.ReadyForRoad && definitionIds.Count == 0 || existing.MatchesSelection(definitionIds)
                ? new(existing, null)
                : StarterEquipmentClaimResult.Fail("You have already chosen this starter reward.");

        IReadOnlyList<string> selection;
        try { selection = catalog.Select(kind, definitionIds); }
        catch (ArgumentException ex) { return StarterEquipmentClaimResult.Fail(ex.Message); }

        var prerequisite = kind == StarterEquipmentGrantKind.FirstWeapon
            ? "quest.onboarding.soul_archive" : "quest.onboarding.first_weapon";
        var progress = await quests.GetProgressAsync(characterId, prerequisite, cancellationToken);
        if (progress?.Status != QuestStatus.Completed)
            return StarterEquipmentClaimResult.Fail("Complete the preceding onboarding quest first.");
        if (!await repository.HasInventoryAsync(characterId, cancellationToken))
            return StarterEquipmentClaimResult.Fail("Character inventory was not found.");

        var now = DateTimeOffset.UtcNow;
        var source = kind == StarterEquipmentGrantKind.FirstWeapon
            ? "quest.onboarding.first_weapon" : "quest.onboarding.tools_of_trade";
        var descriptors = selection.Select((id, index) => EquipmentData.Create(
            EquipmentState.Award(Guid.NewGuid(), catalog.Evaluator, id, 1, 0,
                new(EquipmentAwardKind.QuestReward, source, $"{characterId:N}:{kind}:{index}"),
                new(EquipmentOwnershipKind.BoundPersonal, characterId)), catalog.Evaluator)).ToArray();
        var bases = await itemBases.GetItemBasesByIdsAsync(descriptors.Select(x => x.ItemBaseId).Distinct().ToArray(), cancellationToken);
        if (descriptors.Any(x => !bases.TryGetValue(x.ItemBaseId, out var itemBase)
            || itemBase is not EquipmentBase equipmentBase || equipmentBase.EquipmentType != x.EquipmentType || itemBase.Stackable))
            throw new InvalidOperationException("Starter equipment catalog does not match the item bases.");
        var items = descriptors.Select(data =>
        {
            var instance = new EquipmentInstance { Id = data.State.Id, ItemBaseId = data.ItemBaseId,
                ItemBase = bases[data.ItemBaseId], AcquiredAtUtc = now, AcquisitionSource = EquipmentKeys.StarterSource };
            instance.ApplyProgressionData(data);
            return new InventoryItem { InventoryId = characterId, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = 1 };
        }).ToArray();
        var grant = new StarterEquipmentGrant(characterId, kind, descriptors, now);
        // ICommand owns the transaction and character lock. Writer, receipt and outbox share that transaction.
        repository.AddGrant(grant);
        await rewards.AddLootAsync(characterId, items, EquipmentKeys.StarterSource, source, cancellationToken);
        return new(grant, null);
    }
}

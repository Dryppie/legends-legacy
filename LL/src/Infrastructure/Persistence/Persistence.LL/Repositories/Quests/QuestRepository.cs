using Application.Common.Interfaces;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Quests;

public sealed class QuestRepository(IDbContext context) : IQuestRepository
{
    public async Task<IReadOnlyList<CharacterQuestProgress>> GetProgressesAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        await context.CharacterQuestProgresses
            .Include(x => x.Objectives)
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

    public Task<CharacterQuestProgress?> GetProgressAsync(
        Guid characterId,
        string questId,
        CancellationToken cancellationToken) =>
        context.CharacterQuestProgresses
            .Include(x => x.Objectives)
            .FirstOrDefaultAsync(
                x => x.CharacterId == characterId && x.QuestId == questId,
                cancellationToken);

    public Task<int?> GetCharacterLevelAsync(Guid characterId, CancellationToken cancellationToken) =>
        context.Characters
            .Where(x => x.Id == characterId)
            .Select(x => (int?)x.Level)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasProcessedEventAsync(Guid outboxMessageId, CancellationToken cancellationToken) =>
        context.QuestEventLedgers.AnyAsync(x => x.OutboxMessageId == outboxMessageId, cancellationToken);

    public Task<bool> HasEssenceInAnyLoadoutAsync(
        Guid characterId,
        string essenceDefinitionId,
        CancellationToken cancellationToken) =>
        context.EssenceLoadouts
            .Where(x => x.CharacterId == characterId)
            .SelectMany(x => x.Slots)
            .AnyAsync(
                x => x.PlayerEssence != null &&
                     x.PlayerEssence.EssenceDefinitionId == essenceDefinitionId,
                cancellationToken);

    public Task<bool> HasQualifyingEquipmentEquippedAsync(
        Guid characterId,
        IReadOnlyCollection<string> itemBaseIds,
        int? tier,
        bool mustBeCrafted,
        bool toolSlotOnly,
        CancellationToken cancellationToken)
    {
        var ids = itemBaseIds.ToArray();
        return context.EquipmentSlots
            .Include(x => x.EquipmentInstance)
            .AnyAsync(
                x => x.EntityId == characterId &&
                     x.EquipmentInstance != null &&
                     ids.Contains(x.EquipmentInstance.ItemBaseId) &&
                     (!tier.HasValue || x.EquipmentInstance.Tier == tier.Value) &&
                     (!mustBeCrafted || x.EquipmentInstance.BaseRecipeId != null) &&
                     (!toolSlotOnly || x.EquipmentSlotType == EquipmentSlotType.Tool),
                cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetCraftedRecipeIdsAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var recipeIds = await context.CharacterRecipeMasteries
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId && x.Experience > 0)
            .Select(x => x.RecipeId)
            .ToListAsync(cancellationToken);

        return recipeIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public void AddProgress(CharacterQuestProgress progress) =>
        context.CharacterQuestProgresses.Add(progress);

    public void AddEventLedger(QuestEventLedger ledger) =>
        context.QuestEventLedgers.Add(ledger);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}

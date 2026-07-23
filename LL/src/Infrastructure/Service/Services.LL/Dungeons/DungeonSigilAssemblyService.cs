using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Dungeons;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Services.LL.Interfaces;

namespace Services.LL.Dungeons;

public sealed class DungeonSigilAssemblyService(
    IDungeonDefinitions dungeonDefinitions,
    IDungeonAccessPolicy dungeonAccess,
    IDungeonSigilAssemblySettingsProvider settingsProvider,
    IDungeonSigilAssemblyRepository repository,
    ICharacterService characters,
    IInventoryService inventory,
    IInventoryRepository inventoryRepository,
    IItemBaseRepository itemBases,
    IInventoryItemFactory inventoryItemFactory) : IDungeonSigilAssemblyService
{
    public async Task<DungeonSigilAssemblyOperationResult> AssembleAsync(
        Guid characterId,
        string dungeonId,
        CancellationToken cancellationToken)
    {
        var settings = settingsProvider.GetSettings();
        if (!settings.Enabled)
        {
            return DungeonSigilAssemblyOperationResult.Fail("Dungeon sigil assembly is currently disabled.");
        }

        var dungeon = dungeonDefinitions.GetAll().FirstOrDefault(x =>
            x.Id.Equals(dungeonId, StringComparison.OrdinalIgnoreCase));
        if (dungeon is null || string.IsNullOrWhiteSpace(dungeon.SigilItemId))
        {
            return DungeonSigilAssemblyOperationResult.Fail("That dungeon does not have an assemblable sigil.");
        }

        var character = await characters.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        if (character is null)
        {
            return DungeonSigilAssemblyOperationResult.Fail("Character was not found.");
        }

        var access = await dungeonAccess.EvaluateForSigilAssemblyAsync(
            characterId,
            dungeon,
            cancellationToken);
        if (!access.CanEnter)
        {
            return DungeonSigilAssemblyOperationResult.Fail(
                access.MissingRequirements.Count > 0
                    ? string.Join(" ", access.MissingRequirements)
                    : "That dungeon sigil is not currently accessible.");
        }

        var sigilItemBases = await itemBases.GetItemBasesByIdsAsync([dungeon.SigilItemId], cancellationToken);
        if (!sigilItemBases.TryGetValue(dungeon.SigilItemId, out var sigilItemBase))
        {
            return DungeonSigilAssemblyOperationResult.Fail("The dungeon sigil item definition could not be found.");
        }

        var remainingFragments = character.SigilFragments < settings.FragmentCost
            ? null
            : await repository.TrySpendFragmentsAsync(characterId, settings.FragmentCost, cancellationToken);
        if (remainingFragments is null)
        {
            return DungeonSigilAssemblyOperationResult.Fail(
                $"Assembling this sigil requires {settings.FragmentCost} Sigil Fragments.");
        }

        var ownedQuantity = await inventoryRepository.GetInventoryQuantityAsync(
            characterId,
            dungeon.SigilItemId,
            cancellationToken);
        var assembledSigil = inventoryItemFactory.Create(sigilItemBase, 1, characterId);
        await inventory.AddItemsToInventory(characterId, [assembledSigil], cancellationToken);

        return DungeonSigilAssemblyOperationResult.Success(new DungeonSigilAssemblyResult(
            dungeon.Id,
            dungeon.SigilItemId,
            sigilItemBase.Name,
            ownedQuantity + 1,
            remainingFragments.Value));
    }
}

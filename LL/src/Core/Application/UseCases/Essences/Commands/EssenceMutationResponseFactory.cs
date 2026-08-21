using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Items;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Essences.Dtos;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;

namespace Application.UseCases.Essences.Commands;

public sealed class EssenceMutationResponseFactory(
    IMapper mapper,
    IEssenceService essences,
    ICreatureArchiveService creatureArchive,
    IInventoryService inventory,
    IEquipmentSlotService equipment)
{
    public async Task<EssenceMutationResponseDto?> CreateAsync(
        Guid characterId,
        bool succeeded,
        string message,
        CancellationToken cancellationToken,
        int? dustGained = null,
        int? dustSpent = null,
        int? xpGained = null,
        int? levelsGained = null,
        bool? reachedTierCap = null)
    {
        var archive = await essences.GetSoulArchiveAsync(characterId, cancellationToken);
        var loadouts = await essences.GetLoadoutsAsync(characterId, cancellationToken);
        var creatures = await creatureArchive.GetCreatureArchiveAsync(characterId, cancellationToken);
        var codex = await creatureArchive.GetEssenceCodexAsync(characterId, cancellationToken);
        var inventorySnapshot = await inventory.GetInventoryByIdAsync(characterId, cancellationToken);
        if (inventorySnapshot is null)
        {
            return null;
        }

        var equipmentSnapshot = await equipment.GetEquipmentSlotsByEntityIdAsync(
            characterId,
            cancellationToken);

        return new EssenceMutationResponseDto
        {
            Succeeded = succeeded,
            Message = message,
            Archive = mapper.Map<SoulArchiveDto>(archive),
            Loadouts = mapper.Map<EssenceLoadoutsDto>(loadouts),
            CreatureArchive = mapper.Map<CreatureArchiveDto>(creatures),
            Codex = mapper.Map<EssenceCodexDto>(codex),
            InventoryItems = mapper.Map<List<InventoryItemDto>>(inventorySnapshot.InventoryItems),
            EquipmentSlots = mapper.Map<List<EquipmentSlotDto>>(equipmentSnapshot),
            DustGained = dustGained,
            DustSpent = dustSpent,
            XpGained = xpGained,
            LevelsGained = levelsGained,
            ReachedTierCap = reachedTierCap
        };
    }
}

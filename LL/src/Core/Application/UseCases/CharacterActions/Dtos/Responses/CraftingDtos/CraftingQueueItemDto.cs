using Application.Common.Mappings;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Domain.Models.Professions.Crafting;

namespace Application.UseCases.CharacterActions.Dtos.Responses.CraftingDtos;
public class CraftingQueueItemDto : IMapFrom<CraftingQueueItem>
{
    public Guid Id { get; set; }
    public Guid EquipmentInstanceId { get; set; }
    public string TemperingRecipeId { get; set; } = string.Empty;
    public EquipmentInstanceDto EquipmentInstance { get; set; } = null!;
    
    public void Mapping(Profile profile)
    {
        profile.CreateMap<CraftingQueueItem, CraftingQueueItemDto>();
    }
}

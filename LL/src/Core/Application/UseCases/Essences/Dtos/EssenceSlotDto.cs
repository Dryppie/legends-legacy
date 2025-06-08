using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Essences.EssenceSlots;

namespace Application.UseCases.Essences.Dtos;
public class EssenceSlotDto : IMapFrom<EssenceSlot>
{
    public Guid Id { get; set; }
    public SlotType SlotType { get; set; }
    public SlotState SlotState { get; set; }
    public Guid EntityId { get; set; }
    public Guid? EssenceId { get; set; }
    public EssenceDto? OccupiedEssence { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EssenceSlot, EssenceSlotDto>();
    }
}

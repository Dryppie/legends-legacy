using AutoMapper;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments.Loadouts;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentLoadoutMappingProfile : Profile
{
    public EquipmentLoadoutMappingProfile()
    {
        CreateMap<EquipmentLoadoutSlot, EquipmentLoadoutSlotDto>();
        CreateMap<EquipmentLoadout, EquipmentLoadoutDto>()
            .ForCtorParam(nameof(EquipmentLoadoutDto.AutoUseActivities), opt => opt.MapFrom(source =>
                Enum.GetValues<EssenceCombatActivity>().Where(activity => EssenceLoadoutSelection.IsValidSingleActivity(activity) && source.AutoUseActivities.HasFlag(activity)).ToList()));
    }
}

using Application.Interfaces.Services.LL.Essences;
using AutoMapper;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceLoadoutMappingProfile : Profile
{
    public EssenceLoadoutMappingProfile()
    {
        CreateMap<EssenceLoadouts, EssenceLoadoutsDto>().ConvertUsing<EssenceLoadoutsConverter>();
        CreateMap<EssenceLoadout, EssenceLoadoutDto>().ConvertUsing<EssenceLoadoutConverter>();
    }
}

public sealed class EssenceLoadoutsConverter : ITypeConverter<EssenceLoadouts, EssenceLoadoutsDto>
{
    public EssenceLoadoutsDto Convert(EssenceLoadouts source, EssenceLoadoutsDto destination, ResolutionContext context) =>
        new(source.Loadouts.Select(x => context.Mapper.Map<EssenceLoadoutDto>(x)).ToList(), source.Limit, source.UnlockedSlots);
}

public sealed class EssenceLoadoutConverter : ITypeConverter<EssenceLoadout, EssenceLoadoutDto>
{
    private readonly IEssenceDefinitionRepository _definitions;

    public EssenceLoadoutConverter(IEssenceDefinitionRepository definitions)
    {
        _definitions = definitions;
    }

    public EssenceLoadoutDto Convert(EssenceLoadout source, EssenceLoadoutDto destination, ResolutionContext context) =>
        new(source.Id, source.Name, source.IsActive, source.Slots.OrderBy(x => x.SlotIndex).Select(slot => MapSlot(slot, context)).ToList());

    private EssenceLoadoutSlotDto MapSlot(EssenceLoadoutSlot slot, ResolutionContext context)
    {
        var definition = slot.PlayerEssence is null ? null : _definitions.GetById(slot.PlayerEssence.EssenceDefinitionId);
        return new(
            slot.SlotIndex,
            slot.PlayerEssenceId,
            slot.PlayerEssence?.EssenceDefinitionId,
            definition?.DisplayName,
            definition is null ? null : context.Mapper.Map<EssenceDefinitionDto>(definition));
    }
}

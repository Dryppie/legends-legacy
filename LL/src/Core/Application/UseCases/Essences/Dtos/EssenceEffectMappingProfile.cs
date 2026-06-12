using AutoMapper;
using Domain.Models.AbilityDefinitions;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceEffectMappingProfile : Profile
{
    public EssenceEffectMappingProfile()
    {
        CreateMap<AbilityEffectDefinition, EssenceEffectDto>().ConvertUsing<AbilityEffectDefinitionConverter>();
    }
}

public sealed class AbilityEffectDefinitionConverter : ITypeConverter<AbilityEffectDefinition, EssenceEffectDto>
{
    public EssenceEffectDto Convert(AbilityEffectDefinition source, EssenceEffectDto destination, ResolutionContext context) =>
        new(source.Id, source.Type, source.Target, source.Scaling.BaseValue, source.Attribute, source.Status, source.DurationSeconds);
}

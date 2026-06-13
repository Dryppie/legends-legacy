using AutoMapper;
using Domain.Models.Essences.Definitions;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceAttributeBonusMappingProfile : Profile
{
    public EssenceAttributeBonusMappingProfile()
    {
        CreateMap<EssenceAttributeBonusDefinition, EssenceAttributeBonusDto>().ConvertUsing<EssenceAttributeBonusDefinitionConverter>();
    }
}

public sealed class EssenceAttributeBonusDefinitionConverter : ITypeConverter<EssenceAttributeBonusDefinition, EssenceAttributeBonusDto>
{
    public EssenceAttributeBonusDto Convert(EssenceAttributeBonusDefinition source, EssenceAttributeBonusDto destination, ResolutionContext context) =>
        new(source.Attribute, source.ModifierKind.ToString(), source.BaseValue, source.BaseValue);
}

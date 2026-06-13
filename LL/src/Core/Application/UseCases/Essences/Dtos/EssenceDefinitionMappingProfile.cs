using AutoMapper;
using Domain.Models.Essences.Definitions;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceDefinitionMappingProfile : Profile
{
    public EssenceDefinitionMappingProfile()
    {
        CreateMap<EssenceDefinition, EssenceDefinitionDto>().ConvertUsing<EssenceDefinitionConverter>();
    }
}

public sealed class EssenceDefinitionConverter : ITypeConverter<EssenceDefinition, EssenceDefinitionDto>
{
    public EssenceDefinitionDto Convert(EssenceDefinition source, EssenceDefinitionDto destination, ResolutionContext context) =>
        new(
            source.Id,
            source.SourceMonsterId,
            source.Name,
            source.Description,
            source.Rarity,
            GroupTags(source.Tags),
            source.AttributeBonuses.Select(x => context.Mapper.Map<EssenceAttributeBonusDto>(x)).ToList(),
            context.Mapper.Map<EssenceAbilityDto>(source.ActiveAbility),
            context.Mapper.Map<EssenceAbilityDto>(source.PassiveAbility),
            context.Mapper.Map<EssenceEvolutionDto>(source.Evolution),
            context.Mapper.Map<EssenceDropDto>(source.Drop));

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> GroupTags(IEnumerable<string> tags) =>
        tags.GroupBy(EssenceTagCatalog.GetCategory).ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Order().ToList());
}

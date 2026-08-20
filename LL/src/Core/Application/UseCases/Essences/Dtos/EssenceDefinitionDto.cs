using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Essences.Definitions;
using Domain.Models.Items;

namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceDefinitionDto(
    string Id,
    string SourceMonsterId,
    string Name,
    string VariantName,
    string DisplayName,
    string Description,
    Rarity Rarity,
    IReadOnlyDictionary<string, IReadOnlyList<string>> TagsByCategory,
    IReadOnlyList<EssenceAttributeBonusDto> AttributeBonuses,
    EssenceAbilityDto ActiveAbility,
    EssenceAbilityDto PassiveAbility,
    EssenceEvolutionDto Evolution) : IMapFrom<EssenceDefinition>
{
    public EssenceDefinitionDto()
        : this(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            default,
            new Dictionary<string, IReadOnlyList<string>>(),
            [],
            EmptyAbility(),
            EmptyAbility(),
            new EssenceEvolutionDto(string.Empty, string.Empty, string.Empty, 0, string.Empty, []))
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EssenceDefinition, EssenceDefinitionDto>()
            .ForMember(destination => destination.TagsByCategory, options =>
                options.MapFrom(source => GroupTags(source.Tags)))
            .ForMember(destination => destination.AttributeBonuses, options =>
                options.MapFrom(_ => Array.Empty<EssenceAttributeBonusDto>()));
    }

    private static EssenceAbilityDto EmptyAbility() =>
        new(string.Empty, string.Empty, string.Empty, string.Empty, 0, 0, 1f, 0, [], [], []);

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> GroupTags(IEnumerable<string> tags) =>
        tags.GroupBy(EssenceTagCatalog.GetCategory).ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Order().ToList());
}

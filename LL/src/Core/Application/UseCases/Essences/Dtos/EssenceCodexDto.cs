using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;

public sealed record EssenceCodexDto(
    IReadOnlyList<EssenceCodexEntryDto> Entries) : IMapFrom<EssenceCodex>
{
    public EssenceCodexDto()
        : this([])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EssenceCodex, EssenceCodexDto>();
    }
}

public sealed record EssenceCodexEntryDto(
    string Id,
    string Title,
    string Description,
    string BenefitText,
    int Current,
    int Required,
    bool IsUnlocked,
    string Category) : IMapFrom<EssenceCodexEntry>
{
    public EssenceCodexEntryDto()
        : this(string.Empty, string.Empty, string.Empty, string.Empty, 0, 0, false, string.Empty)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EssenceCodexEntry, EssenceCodexEntryDto>();
    }
}

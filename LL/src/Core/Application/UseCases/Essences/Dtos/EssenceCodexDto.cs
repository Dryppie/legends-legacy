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
    string BonusKind,
    double BonusValue,
    int Current,
    int Required,
    bool IsUnlocked,
    string Category,
    IReadOnlyList<EssenceCodexMemberDto> Essences) : IMapFrom<EssenceCodexEntry>
{
    public EssenceCodexEntryDto()
        : this(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0, 0, 0, false, string.Empty, [])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EssenceCodexEntry, EssenceCodexEntryDto>()
            .ForMember(dest => dest.BonusKind, opt => opt.MapFrom(src => src.BonusKind.ToString()));
    }
}

public sealed record EssenceCodexMemberDto(
    string EssenceDefinitionId,
    string Name,
    bool IsAbsorbed) : IMapFrom<EssenceCodexMember>
{
    public EssenceCodexMemberDto()
        : this(string.Empty, string.Empty, false)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EssenceCodexMember, EssenceCodexMemberDto>();
    }
}

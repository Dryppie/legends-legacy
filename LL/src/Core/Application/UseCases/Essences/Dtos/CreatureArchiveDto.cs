using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;

public sealed record CreatureArchiveDto(
    IReadOnlyList<CreatureArchiveEntryDto> Creatures) : IMapFrom<CreatureArchive>
{
    public CreatureArchiveDto()
        : this([])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CreatureArchive, CreatureArchiveDto>();
    }
}

public sealed record CreatureArchiveEntryDto(
    string CreatureId,
    string Name,
    int KillCount,
    DateTimeOffset FirstDefeatedAtUtc,
    DateTimeOffset LastDefeatedAtUtc,
    string? EssenceDefinitionId,
    string? EssenceName,
    bool IsEssenceAbsorbed,
    IReadOnlyList<string> Tags) : IMapFrom<CreatureArchiveEntry>
{
    public CreatureArchiveEntryDto()
        : this(string.Empty, string.Empty, 0, default, default, null, null, false, [])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CreatureArchiveEntry, CreatureArchiveEntryDto>();
    }
}

using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;

public sealed record CreatureArchiveDto(
    IReadOnlyList<CreatureArchiveEntryDto> Creatures,
    bool CanChangeEssenceFocus,
    DateTimeOffset? EssenceFocusAvailableAtUtc,
    DateTimeOffset? EssenceFocusSetAtUtc) : IMapFrom<CreatureArchive>
{
    public CreatureArchiveDto()
        : this([], true, null, null)
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
    bool IsEssenceFocus,
    DateTimeOffset? EssenceFocusSetAtUtc,
    long EssenceFocusTotalDurationSeconds,
    long CurrentEssenceFocusDurationSeconds,
    IReadOnlyList<CreatureArchiveEssenceEntryDto> Essences,
    IReadOnlyList<string> Tags) : IMapFrom<CreatureArchiveEntry>
{
    public CreatureArchiveEntryDto()
        : this(string.Empty, string.Empty, 0, default, default, false, null, 0, 0, [], [])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CreatureArchiveEntry, CreatureArchiveEntryDto>();
    }
}

public sealed record CreatureArchiveEssenceEntryDto(
    string EssenceDefinitionId,
    string Name,
    bool IsAbsorbed,
    IReadOnlyList<string> Tags) : IMapFrom<CreatureArchiveEssenceEntry>
{
    public CreatureArchiveEssenceEntryDto()
        : this(string.Empty, string.Empty, false, [])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CreatureArchiveEssenceEntry, CreatureArchiveEssenceEntryDto>();
    }
}

using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Prophecies;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class ProphecyGuidanceDto : IMapFrom<PlayerProphecyInstance>
{
    public string Destination { get; set; } = ProphecyGuidanceDestination.WorldCombat;
    public string ActionLabel { get; set; } = "Continue Adventuring";
    public string Hint { get; set; } = "Complete the described objective to progress this prophecy.";

    public void Mapping(Profile profile)
    {
        profile.CreateMap<PlayerProphecyInstance, ProphecyGuidanceDto>()
            .ConvertUsing(instance => ProphecyMappingHelpers.Guidance(instance));
    }
}

public static class ProphecyGuidanceDestination
{
    public const string WorldCombat = nameof(WorldCombat);
    public const string Dungeons = nameof(Dungeons);
    public const string Essences = nameof(Essences);
    public const string SoulArchive = nameof(SoulArchive);
}

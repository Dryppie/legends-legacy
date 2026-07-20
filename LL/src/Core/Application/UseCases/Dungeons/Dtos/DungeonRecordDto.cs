using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Dungeons.Runs;

namespace Application.UseCases.Dungeons.Dtos;

public sealed class DungeonRecordDto : IMapFrom<DungeonCompletionRecord>
{
    public bool HasCleared { get; set; }
    public DateTimeOffset? FirstClearedAt { get; set; }
    public DateTimeOffset? LastClearedAt { get; set; }
    public int TotalClears { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DungeonCompletionRecord, DungeonRecordDto>()
            .ForMember(destination => destination.HasCleared, options => options.MapFrom(_ => true))
            .ForMember(
                destination => destination.FirstClearedAt,
                options => options.MapFrom(source => source.FirstCompletedAt))
            .ForMember(
                destination => destination.LastClearedAt,
                options => options.MapFrom(source => source.LastCompletedAt))
            .ForMember(
                destination => destination.TotalClears,
                options => options.MapFrom(source => source.CompletionCount));
    }
}

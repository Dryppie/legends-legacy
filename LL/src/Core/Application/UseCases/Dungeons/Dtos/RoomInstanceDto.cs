using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Dungeons.Definitions.Events;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;

namespace Application.UseCases.Dungeons.Dtos;

public class RoomInstanceDto : IMapFrom<RoomInstance>
{
    public Guid Id { get; set; }
    public int Index { get; set; }
    public RoomType Type { get; set; }
    public RoomInstanceStatus Status { get; set; }
    public List<string> EncounterIds { get; set; } = [];
    public EventOutcomeType? EventOutcome { get; set; }
    public bool IsHidden { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<RoomInstance, RoomInstanceDto>()
            .ForMember(dest => dest.Index, opt => opt.MapFrom(src => src.RoomIndex));
    }
}

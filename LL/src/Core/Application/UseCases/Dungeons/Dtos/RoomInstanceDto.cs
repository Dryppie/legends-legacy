using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;

namespace Application.UseCases.Dungeons.Dtos;

public class RoomInstanceDto : IMapFrom<RoomInstance>
{
    public int Index { get; set; }
    public RoomType Type { get; set; }
    public List<string> EncounterIds { get; set; } = [];
    public bool IsHidden { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<RoomInstance, RoomInstanceDto>();
    }
}
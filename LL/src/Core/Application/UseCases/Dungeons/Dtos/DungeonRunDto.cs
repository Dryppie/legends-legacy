using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;

namespace Application.UseCases.Dungeons.Dtos;

public class DungeonRunDto : IMapFrom<DungeonRun>
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }

    public string DungeonDefinitionId { get; set; } = string.Empty;
    public string DungeonDefinitionName { get; set; } = string.Empty;

    public int Seed { get; set; }
    public DungeonRunStatus Status { get; set; }
    public int CurrentRoomIndex { get; set; }

    public int TotalRooms { get; set; }

    public List<RoomInstanceDto> Rooms { get; set; } = [];
    public int PendingExperience { get; set; }
    public int PendingCinders { get; set; }
    public int PendingSoulstones { get; set; }
    public List<RunRewardDto> PendingRewards { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DungeonRun, DungeonRunDto>()
            .ForMember(dest => dest.TotalRooms, opt => opt.MapFrom(src => src.Rooms.Count))
            .ForMember(dest => dest.Rooms, opt => opt.MapFrom((src, _, _, context) =>
            {
                return src.Rooms
                    .Select((room, index) =>
                    {
                        var isRevealed = index <= src.CurrentRoomIndex;
                        var isCheckpoint = room.Type == RoomType.Checkpoint;

                        if (isRevealed || isCheckpoint)
                        {
                            return new RoomInstanceDto
                            {
                                Index = room.RoomIndex,
                                Type = room.Type,
                                EncounterIds = room.EncounterIds?.ToList() ?? [],
                                IsHidden = false
                            };
                        }

                        return new RoomInstanceDto
                        {
                            Index = room.RoomIndex,
                            Type = RoomType.Unknown,
                            EncounterIds = [],
                            IsHidden = true
                        };
                    })
                    .ToList();
            })
            );

        profile.CreateMap<RunReward, RunRewardDto>();
    }
}

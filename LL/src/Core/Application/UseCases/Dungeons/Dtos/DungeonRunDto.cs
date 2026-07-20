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
    public DungeonRunStateDto State { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<DungeonRun, DungeonRunDto>()
            .ForMember(dest => dest.TotalRooms, opt => opt.MapFrom(src =>
                src.State != null && src.State.MapNodes.Count > 0
                    ? src.State.MapNodes.Select(node => node.Depth).Distinct().Count()
                    : src.Rooms.Count))
            .ForMember(dest => dest.Rooms, opt => opt.MapFrom((src, _, _, context) =>
            {
                var currentDepth = src.State?.MapNodes
                    .FirstOrDefault(node => node.RoomIndex == src.CurrentRoomIndex)?.Depth;

                return src.Rooms
                    .Select(room =>
                    {
                        var node = src.State?.MapNodes.FirstOrDefault(candidate => candidate.RoomIndex == room.RoomIndex);
                        var isRevealed = currentDepth.HasValue && node is not null
                            ? node.Depth <= currentDepth.Value + 1
                            : room.RoomIndex <= src.CurrentRoomIndex;
                        var isCheckpoint = room.Type == RoomType.Checkpoint;
                        var isBoss = room.Type == RoomType.Boss;

                        if (isRevealed || isCheckpoint || isBoss)
                        {
                            return new RoomInstanceDto
                            {
                                Id = room.Id,
                                Index = room.RoomIndex,
                                Type = room.Type,
                                Status = room.Status,
                                EncounterIds = room.EncounterIds?.ToList() ?? [],
                                EventOutcome = room.Status == RoomInstanceStatus.Pending
                                    ? null
                                    : room.EventOutcome,
                                IsHidden = false
                            };
                        }

                        return new RoomInstanceDto
                        {
                            Id = room.Id,
                            Index = room.RoomIndex,
                            Type = RoomType.Unknown,
                            Status = room.Status,
                            EncounterIds = [],
                            EventOutcome = null,
                            IsHidden = true
                        };
                    })
                    .ToList();
            }));
    }
}

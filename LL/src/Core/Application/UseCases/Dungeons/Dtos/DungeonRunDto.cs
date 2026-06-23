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
            .ForMember(dest => dest.State, opt => opt.MapFrom((src, _) => ToStateDto(src)))
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
            })
            );

        profile.CreateMap<RunReward, RunRewardDto>();
    }

    private static DungeonRunStateDto ToStateDto(DungeonRun src)
    {
        var state = src.State;
        if (state is null)
        {
            return new DungeonRunStateDto
            {
                MechanicDisplayName = "Pressure"
            };
        }

        return new DungeonRunStateDto
        {
            Pressure = state.Pressure,
            MechanicId = string.IsNullOrWhiteSpace(state.MechanicId) ? "pressure" : state.MechanicId,
            MechanicDisplayName = string.IsNullOrWhiteSpace(state.MechanicDisplayName)
                ? "Pressure"
                : state.MechanicDisplayName,
            MechanicMaxValue = state.MechanicMaxValue <= 0 ? 100 : state.MechanicMaxValue,
            RewardMultiplierPercent = state.RewardMultiplierPercent <= 0 ? 100 : state.RewardMultiplierPercent,
            ActiveBoonIds = state.ActiveBoonIds.ToList(),
            ActiveBoonSummaries = state.ActiveBoonSummaries.Select(boon => new DungeonActiveBoonSummaryDto
            {
                Id = boon.Id,
                Name = boon.Name,
                Description = boon.Description,
                Rarity = boon.Rarity,
                Count = boon.Count,
                EffectSummaries = boon.EffectSummaries.ToList()
            }).ToList(),
            ActiveBoonEffectSummaries = state.ActiveBoonEffectSummaries.Select(effect => new DungeonBoonEffectSummaryDto
            {
                Id = effect.Id,
                Label = effect.Label,
                Value = effect.Value,
                Category = effect.Category
            }).ToList(),
            Flags = new Dictionary<string, int>(state.Flags),
            SecuredLoot = new DungeonLootBagDto
            {
                Experience = state.SecuredLoot.Experience,
                Cinders = state.SecuredLoot.Cinders,
                Soulstones = state.SecuredLoot.Soulstones,
                Items = new Dictionary<string, int>(state.SecuredLoot.Items)
            },
            UnsecuredLoot = new DungeonLootBagDto
            {
                Experience = state.UnsecuredLoot.Experience,
                Cinders = state.UnsecuredLoot.Cinders,
                Soulstones = state.UnsecuredLoot.Soulstones,
                Items = new Dictionary<string, int>(state.UnsecuredLoot.Items)
            },
            CurrentRouteOptions = state.CurrentRouteOptions.Select(route => new DungeonRouteOptionDto
            {
                Id = route.Id,
                RoomIndex = route.RoomIndex,
                DisplayName = route.DisplayName,
                RoomType = route.RoomType,
                RiskLevel = route.RiskLevel,
                PressureDelta = route.PressureDelta,
                IsUnknown = route.IsUnknown,
                Tags = route.Tags.ToList(),
                PossibleRewards = route.PossibleRewards.ToList(),
                Requirements = route.Requirements.ToList()
            }).ToList(),
            CurrentEventChoices = state.CurrentEventChoices.Select(choice => new DungeonEventChoiceOptionDto
            {
                Id = choice.Id,
                Label = choice.Label,
                Description = choice.Description,
                PressureDelta = choice.PressureDelta,
                RewardMultiplierDeltaPercent = choice.RewardMultiplierDeltaPercent,
                AddFlags = choice.AddFlags.ToList(),
                RemoveFlags = choice.RemoveFlags.ToList(),
                MissingRequirements = choice.MissingRequirements.ToList(),
                GrantsBoonChoice = choice.GrantsBoonChoice,
                GrantsLoot = choice.GrantsLoot,
                AmbushChancePercent = choice.AmbushChancePercent,
                RevealsHiddenRoute = choice.RevealsHiddenRoute
            }).ToList(),
            CurrentCheckpointChoices = state.CurrentCheckpointChoices.Select(choice => new DungeonCheckpointChoiceOptionDto
            {
                Id = choice.Id,
                Label = choice.Label,
                Description = choice.Description,
                PressureDelta = choice.PressureDelta,
                RewardMultiplierDeltaPercent = choice.RewardMultiplierDeltaPercent
            }).ToList(),
            CurrentBoonChoices = state.CurrentBoonChoices.Select(choice => new DungeonBoonChoiceOptionDto
            {
                Id = choice.Id,
                Name = choice.Name,
                Description = choice.Description,
                Rarity = choice.Rarity,
                EffectSummaries = choice.EffectSummaries.ToList()
            }).ToList(),
            CurrentBossModifiers = state.CurrentBossModifiers.Select(modifier => new DungeonBossModifierDto
            {
                Id = modifier.Id,
                Name = modifier.Name,
                Description = modifier.Description,
                Source = modifier.Source,
                AttributeType = modifier.AttributeType.ToString(),
                Amount = modifier.Amount,
                ModifierType = modifier.ModifierType.ToString(),
                IsHelpfulToPlayer = modifier.IsHelpfulToPlayer
            }).ToList(),
            CurrentMechanicThresholds = state.CurrentMechanicThresholds.Select(threshold => new DungeonMechanicThresholdStateDto
            {
                Id = threshold.Id,
                Value = threshold.Value,
                Description = threshold.Description,
                RewardMultiplierBonusPercent = threshold.RewardMultiplierBonusPercent
            }).ToList(),
            MasteryAwardReasons = state.MasteryAwardReasons.Select(reason => new DungeonMasteryAwardReasonDto
            {
                Id = reason.Id,
                Description = reason.Description,
                Experience = reason.Experience
            }).ToList()
        };
    }
}

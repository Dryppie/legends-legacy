using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Prophecies;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class ProphecyInstanceDto : IMapFrom<PlayerProphecyInstance>
{
    public Guid Id { get; set; }
    public string DefinitionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FlavorText { get; set; } = string.Empty;
    public string ObjectiveText { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string SlotType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string ObjectiveType { get; set; } = string.Empty;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public int TargetValue { get; set; }
    public int CurrentValue { get; set; }
    public ProphecyRewardSnapshotDto Reward { get; set; } = new();

    public void Mapping(Profile profile)
    {
        profile.CreateMap<PlayerProphecyInstance, ProphecyInstanceDto>()
            .ForMember(dest => dest.DefinitionId, opt => opt.MapFrom(src => src.ProphecyDefinitionId))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => ProphecyMappingHelpers.Definition(src).Title))
            .ForMember(dest => dest.FlavorText, opt => opt.MapFrom(src => ProphecyMappingHelpers.Definition(src).FlavorText))
            .ForMember(dest => dest.ObjectiveText, opt => opt.MapFrom(src =>
                ProphecyMappingHelpers.Definition(src).ObjectiveText.Replace("{target}", src.TargetValue.ToString())))
            .ForMember(dest => dest.Scope, opt => opt.MapFrom(src => src.Scope.ToString()))
            .ForMember(dest => dest.SlotType, opt => opt.MapFrom(src => src.SlotType.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => ProphecyMappingHelpers.Definition(src).Category.ToString()))
            .ForMember(dest => dest.Difficulty, opt => opt.MapFrom(src => ProphecyMappingHelpers.Definition(src).Difficulty.ToString()))
            .ForMember(dest => dest.ObjectiveType, opt => opt.MapFrom(src => ProphecyMappingHelpers.Definition(src).ObjectiveType))
            .ForMember(dest => dest.Reward, opt => opt.MapFrom((src, _, _, context) =>
                context.Mapper.Map<ProphecyRewardSnapshotDto>(ProphecyMappingHelpers.ReadReward(src.RewardSnapshotJson))));
    }
}

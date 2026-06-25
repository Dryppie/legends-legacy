using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Prophecies;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class ProphecyRewardSnapshotDto : IMapFrom<ProphecyRewardSnapshot>
{
    public long Cinders { get; set; }
    public long CharacterExperience { get; set; }
    public long EssenceExperience { get; set; }
    public int Soulstones { get; set; }
    public int SigilFragments { get; set; }
    public int AscensionStoneFragments { get; set; }
    public int PropheticFavor { get; set; }
    public int FateEcho { get; set; }
    public string? CacheItemId { get; set; }
    public List<RewardItemSnapshotDto> Items { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ProphecyRewardSnapshot, ProphecyRewardSnapshotDto>();
    }
}

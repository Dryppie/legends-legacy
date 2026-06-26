using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Prophecies;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class RewardItemSnapshotDto : IMapFrom<RewardItemSnapshot>
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<RewardItemSnapshot, RewardItemSnapshotDto>();
    }
}

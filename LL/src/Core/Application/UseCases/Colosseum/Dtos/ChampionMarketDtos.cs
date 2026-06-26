using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Colosseum;
using Application.UseCases.Colosseum.Models;
using AutoMapper;

namespace Application.UseCases.Colosseum.Dtos;

public sealed class ChampionMarketItemDto : IMapFrom<ChampionMarketItemModel>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int GloryCost { get; set; }
    public int? WeeklyPurchaseLimit { get; set; }
    public int? LifetimePurchaseLimit { get; set; }
    public int RemainingWeeklyPurchases { get; set; }
    public int RemainingLifetimePurchases { get; set; }
    public int? RequiredRating { get; set; }
    public string? RequiredRankTier { get; set; }
    public bool CanPurchase { get; set; }
    public string? CannotPurchaseReason { get; set; }
    public int SortOrder { get; set; }
    public int CindersGranted { get; set; }
    public int SoulstonesGranted { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ChampionMarketItemModel, ChampionMarketItemDto>();
    }
}

public sealed class ChampionMarketDto : IMapFrom<ChampionMarketModel>
{
    public int Glory { get; set; }
    public DateTimeOffset WeeklyResetAt { get; set; }
    public List<ChampionMarketItemDto> Items { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ChampionMarketModel, ChampionMarketDto>();
    }
}

public sealed class PurchaseChampionMarketItemRequestDto : IMapFrom<PurchaseChampionMarketItemRequestModel>
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<PurchaseChampionMarketItemRequestModel, PurchaseChampionMarketItemRequestDto>();
    }
}

public sealed class PurchaseChampionMarketItemResponseDto : IMapFrom<ChampionMarketPurchaseResult>
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int GlorySpent { get; set; }
    public int GloryRemaining { get; set; }
    public int CindersGranted { get; set; }
    public int SoulstonesGranted { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ChampionMarketPurchaseResult, PurchaseChampionMarketItemResponseDto>()
            .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.Item.Id));
    }
}

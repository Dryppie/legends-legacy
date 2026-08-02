using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Colosseum.Models;
using AutoMapper;
using Domain.Models.Colosseum;
using MediatR;

namespace Application.UseCases.Colosseum.Queries.GetChampionMarket;

public record GetChampionMarketQuery(Guid CharacterId) : IQuery<ChampionMarketDto>;

public sealed class GetChampionMarketQueryHandler : IRequestHandler<GetChampionMarketQuery, ChampionMarketDto>
{
    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;

    public GetChampionMarketQueryHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
    }

    public async Task<ChampionMarketDto> Handle(GetChampionMarketQuery request, CancellationToken cancellationToken)
    {
        var character = await _colosseumService.GetArenaCharacterAsync(request.CharacterId, cancellationToken)
            ?? throw new InvalidOperationException("Character was not found.");
        var arena = character.ArenaProfile;
        var weekStart = ArenaCalendar.GetCurrentWeeklyResetStart(DateTimeOffset.UtcNow);
        var weeklyResetAt = weekStart.AddDays(7);

        var items = new List<ChampionMarketItemModel>();
        foreach (var item in _colosseumService.GetChampionMarketItems().Where(x => x.IsEnabled).OrderBy(x => x.SortOrder))
        {
            var weeklyPurchased = await _colosseumService.CountChampionMarketPurchasesAsync(request.CharacterId, item.Id, weekStart, cancellationToken);
            var lifetimePurchased = await _colosseumService.CountChampionMarketPurchasesAsync(request.CharacterId, item.Id, null, cancellationToken);
            items.Add(MapItem(character, item, weeklyPurchased, lifetimePurchased));
        }

        return _mapper.Map<ChampionMarketDto>(new ChampionMarketModel(
            arena.Glory,
            weeklyResetAt,
            items));
    }

    private static ChampionMarketItemModel MapItem(Domain.Models.Entities.Characters.Character character, ChampionMarketItem item, int weeklyPurchased, int lifetimePurchased)
    {
        var remainingWeekly = item.WeeklyPurchaseLimit.HasValue
            ? Math.Max(0, item.WeeklyPurchaseLimit.Value - weeklyPurchased)
            : int.MaxValue;
        var remainingLifetime = item.LifetimePurchaseLimit.HasValue
            ? Math.Max(0, item.LifetimePurchaseLimit.Value - lifetimePurchased)
            : int.MaxValue;
        var reason = GetCannotPurchaseReason(character, item, remainingWeekly, remainingLifetime);

        return new ChampionMarketItemModel(
            item.Id,
            item.Name,
            item.Description,
            item.Category,
            item.GloryCost,
            item.WeeklyPurchaseLimit,
            item.LifetimePurchaseLimit,
            remainingWeekly,
            remainingLifetime,
            item.RequiredRating,
            item.RequiredRankTier,
            reason is null,
            reason,
            item.SortOrder,
            item.CindersGranted,
            item.SoulstonesGranted,
            item.SigilFragmentsGranted,
            item.RewardItemId,
            item.RewardItemName,
            item.RewardItemQuantity);
    }

    private static string? GetCannotPurchaseReason(Domain.Models.Entities.Characters.Character character, ChampionMarketItem item, int remainingWeekly, int remainingLifetime)
    {
        var arena = character.ArenaProfile;
        if (remainingWeekly <= 0) return "Weekly limit reached";
        if (remainingLifetime <= 0) return "Already purchased";
        if (arena.Glory < item.GloryCost) return "Not enough Glory";
        if (item.RequiredRating.HasValue && arena.Rating < item.RequiredRating.Value) return $"Requires {item.RequiredRating.Value} rating";

        if (!string.IsNullOrWhiteSpace(item.RequiredRankTier))
        {
            var current = ArenaRank.GetTier(arena.Rating);
            var required = ArenaRank.Tiers.FirstOrDefault(x => x.Id == item.RequiredRankTier);
            if (required is not null && current.SortOrder < required.SortOrder)
            {
                return $"Requires {required.Name}";
            }
        }

        return null;
    }
}

namespace Domain.Models.MarketPlaces;

public sealed record MarketPlaceItemSummary(
    string ItemBaseId,
    long? LowestSellUnitPrice,
    long TotalSellQuantity,
    long? HighestBuyUnitPrice,
    long TotalBuyQuantity,
    long? LastTradeUnitPrice,
    decimal? MedianUnitPrice7Days,
    long TradeVolume24Hours);

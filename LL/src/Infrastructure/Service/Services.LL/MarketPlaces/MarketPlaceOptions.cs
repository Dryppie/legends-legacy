namespace Services.LL.MarketPlaces;

public sealed class MarketPlaceOptions
{
    public const string SectionName = "Marketplace";

    public int MaximumListingsPerCharacter { get; set; } = 10;
    public int MaximumBuyOrdersPerCharacter { get; set; } = 10;
    public int MaximumStackQuantity { get; set; } = 1_000_000;
    public long MaximumUnitPrice { get; set; } = 1_000_000_000;
    public int SellerFeeBasisPoints { get; set; } = 300;
    public long MinimumSellerFee { get; set; } = 1;
    public int OrderLifetimeDays { get; set; } = 7;
    public int ExpirationSweepIntervalMinutes { get; set; } = 5;
    public int ExpirationBatchSize { get; set; } = 500;
}

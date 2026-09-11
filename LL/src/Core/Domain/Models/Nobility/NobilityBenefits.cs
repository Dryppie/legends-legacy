namespace Domain.Models.Nobility;

public sealed record NobilityBenefits(bool IsNoble)
{
    public const string SignetItemId = "signet";
    public const int PolicyVersion = 2;
    public static readonly NobilityBenefits Free = new(false);
    public static readonly NobilityBenefits Noble = new(true);
    public int OfflineHours => IsNoble ? 168 : 24;
    public int EssenceLoadouts => IsNoble ? 6 : 3;
    public int EquipmentLoadouts => IsNoble ? 6 : 3;
    public int ArenaTicketCap => IsNoble ? 8 : 5;
    public int FocusCooldownHours => IsNoble ? 2 : 8;
    public int MarketSellLimit => IsNoble ? 30 : 10;
    public int MarketBuyLimit => IsNoble ? 30 : 10;
    public int FreeProphecyRerolls => IsNoble ? 2 : 1;
    public int TotalProphecyRerolls => IsNoble ? 4 : 3;
}

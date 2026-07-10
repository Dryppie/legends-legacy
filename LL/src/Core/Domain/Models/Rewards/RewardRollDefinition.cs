namespace Domain.Models.Rewards;

public sealed class RewardRollDefinition
{
    public string Id { get; set; } = string.Empty;
    public RewardRollType Type { get; set; }
    public int Rolls { get; set; } = 1;
    public double Chance { get; set; } = 1;
    public double NoDropWeight { get; set; }
    public List<RewardEntryDefinition> Entries { get; set; } = [];
}

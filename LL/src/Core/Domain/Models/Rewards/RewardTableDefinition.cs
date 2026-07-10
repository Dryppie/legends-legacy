namespace Domain.Models.Rewards;

public sealed class RewardTableDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<RewardRollDefinition> Rolls { get; set; } = [];
}

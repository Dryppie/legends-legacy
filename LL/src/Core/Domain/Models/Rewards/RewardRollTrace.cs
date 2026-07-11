namespace Domain.Models.Rewards;

public sealed record RewardRollTrace(
    string TableId,
    string RollId,
    string? EntryId,
    string Outcome);

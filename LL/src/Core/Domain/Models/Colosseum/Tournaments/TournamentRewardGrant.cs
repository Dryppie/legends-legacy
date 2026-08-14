namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentRewardGrant
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentInstance Tournament { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public string RewardKey { get; set; } = null!;
    public int? Placement { get; set; }
    public int ArenaGlory { get; set; }
    public int Cinders { get; set; }
    public int Soulstones { get; set; }
    public int CatalystSelectionCaches { get; set; }
    public int BlueprintSelectionBoxes { get; set; }
    public int SigilFragments { get; set; }
    public TournamentRewardStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ClaimedAtUtc { get; set; }
}

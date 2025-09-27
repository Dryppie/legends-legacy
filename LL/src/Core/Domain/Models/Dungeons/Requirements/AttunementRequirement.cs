namespace Domain.Models.Dungeons.Requirements;
public sealed class AttunementRequirement : Requirement
{
    public Guid AttunementId { get; private set; }
    public string Title { get; private set; } = "";
    public AttunementRequirement(Guid attunementId, string title) { Discriminator = nameof(AttunementRequirement); AttunementId = attunementId; Title = title; }
    //public override bool IsSatisfiedBy(PlayerContext p) => p.HasAttunement(AttunementId);
}

namespace Domain.Models.Dungeons.Requirements;
public abstract class Requirement
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Discriminator { get; protected set; } = default!;
    public abstract bool IsSatisfiedBy(PlayerContext player);
}

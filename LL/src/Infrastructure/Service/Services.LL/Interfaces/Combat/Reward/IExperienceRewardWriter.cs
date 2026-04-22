namespace Services.LL.Interfaces.Combat.Reward;

public interface IExperienceRewardWriter
{
    Task AddSplitExperienceAsync(
        IReadOnlyCollection<Guid> recipientCharacterIds,
        int totalExperience,
        CancellationToken cancellationToken);
}
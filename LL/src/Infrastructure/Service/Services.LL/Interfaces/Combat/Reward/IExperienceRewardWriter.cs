namespace Services.LL.Interfaces.Combat.Reward;

public interface IExperienceRewardWriter
{
    Task AddSplitExperienceAsync(
        IReadOnlyCollection<Guid> recipientCharacterIds,
        int totalExperience,
        CancellationToken cancellationToken);

    Task AddSplitExperienceAsync(
        IReadOnlyCollection<Guid> recipientCharacterIds,
        int totalExperience,
        Domain.Models.Essences.EssenceCombatActivity activity,
        CancellationToken cancellationToken) =>
        AddSplitExperienceAsync(recipientCharacterIds, totalExperience, cancellationToken);
}

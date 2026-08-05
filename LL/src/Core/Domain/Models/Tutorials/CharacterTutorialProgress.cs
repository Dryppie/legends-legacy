namespace Domain.Models.Tutorials;

public sealed class CharacterTutorialProgress
{
    public Guid CharacterId { get; set; }
    public string TutorialId { get; set; } = TutorialConstants.FirstStepsTutorialId;
    public string CurrentStep { get; set; } = TutorialConstants.StepDefeatTrainingCreature;
    public int CraftedTierOneEquipmentCount { get; set; }
    public int EquippedTierOneEquipmentCount { get; set; }
    public bool TrainingEssenceRewardGranted { get; set; }
    public bool CompletionRewardGranted { get; set; }
    public DateTimeOffset? TrainingCombatWonAt { get; set; }
    public DateTimeOffset? EssenceAbsorbedAt { get; set; }
    public DateTimeOffset? EssenceEquippedAt { get; set; }
    public DateTimeOffset? WelcomeAcknowledgedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsCompleted => CurrentStep == TutorialConstants.StepComplete || CompletedAt.HasValue;
}

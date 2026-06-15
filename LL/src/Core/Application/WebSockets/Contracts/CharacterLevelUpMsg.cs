namespace Application.WebSockets.Contracts;

public record CharacterLevelUpMsg(
    Guid CharacterId,
    int Level,
    float Experience,
    float ExperienceUntilNextLevel) : GameEventMsg;

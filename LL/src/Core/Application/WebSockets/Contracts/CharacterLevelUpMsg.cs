namespace Application.WebSockets.Contracts;

public record CharacterLevelUpMsg(
    Guid CharacterId,
    int Level,
    long Experience,
    long ExperienceUntilNextLevel) : GameEventMsg;

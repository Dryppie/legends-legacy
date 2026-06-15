namespace Application.WebSockets.Contracts;

public record SoulstoneDropMsg(
    Guid CharacterId,
    int SoulstonesEarned,
    long TotalSoulstones) : GameEventMsg;

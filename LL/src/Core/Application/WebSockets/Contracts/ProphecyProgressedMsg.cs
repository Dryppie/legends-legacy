namespace Application.WebSockets.Contracts;

public sealed record ProphecyProgressedMsg(
    Guid CharacterId,
    Guid ProphecyId,
    string Title,
    string Scope,
    string SlotType,
    string Status,
    int CurrentValue,
    int TargetValue,
    int AmountGained,
    bool Completed) : GameEventMsg;

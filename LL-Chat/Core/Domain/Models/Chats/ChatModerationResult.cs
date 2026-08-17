namespace Domain.Models.Chats;

public sealed record ChatModerationResult(
    bool IsSuccess,
    ChatRestriction? Restriction,
    bool WasAlreadyProcessed,
    string ErrorMessage)
{
    public static ChatModerationResult Success(
        ChatRestriction restriction,
        bool replay = false) =>
        new(true, restriction, replay, string.Empty);

    public static ChatModerationResult Fail(string error) =>
        new(false, null, false, error);
}

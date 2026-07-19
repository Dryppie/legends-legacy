using System.Text;

namespace Domain.Models.Leaderboards;

public static class LeaderboardCursor
{
    public static string Encode(
        string boardKey,
        LeaderboardCursorDirection direction,
        Guid anchorParticipantId)
    {
        var directionCode = direction == LeaderboardCursorDirection.After ? "a" : "b";
        var payload = $"1|{boardKey}|{directionCode}|{anchorParticipantId:N}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(
        string boardKey,
        string? cursor,
        out LeaderboardCursorPosition position)
    {
        position = default;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        try
        {
            var normalized = cursor.Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(
                normalized.Length + ((4 - normalized.Length % 4) % 4),
                '=');
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
            var parts = payload.Split('|');
            if (parts.Length != 4 ||
                parts[0] != "1" ||
                !parts[1].Equals(boardKey, StringComparison.OrdinalIgnoreCase) ||
                !Guid.TryParseExact(parts[3], "N", out var anchorParticipantId))
            {
                return false;
            }

            var direction = parts[2] switch
            {
                "a" => LeaderboardCursorDirection.After,
                "b" => LeaderboardCursorDirection.Before,
                _ => (LeaderboardCursorDirection?)null
            };
            if (direction is null)
            {
                return false;
            }

            position = new LeaderboardCursorPosition(
                direction.Value,
                anchorParticipantId);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public enum LeaderboardCursorDirection
{
    After,
    Before
}

public readonly record struct LeaderboardCursorPosition(
    LeaderboardCursorDirection Direction,
    Guid AnchorParticipantId);

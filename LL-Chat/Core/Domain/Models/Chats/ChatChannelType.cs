namespace Domain.Models.Chats;
public enum ChatChannelType
{
    General,     // e.g., "trade", "help"
    Trade,     // e.g., "trade", "help"
    Help,     // e.g., "trade", "help"
    Guild,      // based on user's guild ID
    Whisper,     // direct player-to-player
    System
}

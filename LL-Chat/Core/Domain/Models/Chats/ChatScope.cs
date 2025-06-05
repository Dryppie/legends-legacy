namespace Domain.Models.Chats;
public enum ChatScope
{
    Global,       // public to the whole realm / shard
    Trade,        // public but to a “Trade” tab only
    Help,         // likewise, “Help” tab
    Guild,        // one guild – identified by GuildId
    Whisper,       // exactly 2 users – FromUserId & ToUserId
    //Party,        // one party – PartyId
    ShadowRealm,
}

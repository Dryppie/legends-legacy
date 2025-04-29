namespace Common.Authorization.Security;
public record AuthInfo
{
    public bool IsValid { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterEId { get; set; } = string.Empty;
    /// <summary>
    /// False if Guest account, true if Registered Account
    /// </summary>
    public bool IsPlayer { get; set; } = false;
    public AuthInfo()
    {

    }
}
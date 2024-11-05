namespace Common.Authorization.Security;
public record AuthInfo
{
    public bool IsValid { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public bool IsPlayer { get; set; } = false;
    public AuthInfo()
    {

    }
}
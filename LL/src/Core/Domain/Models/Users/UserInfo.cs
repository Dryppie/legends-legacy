namespace Domain.Models.Users;
public class UserInfo
{
    public string Email { get; set; } = string.Empty;
    public bool IsRegisteredUser { get; set; }
    public bool IsGmailBound { get; set; }
    public bool IsNameEdited { get; set; }
}

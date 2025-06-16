namespace Application.UseCases.Users.Dtos;

public class UserInfoDto
{
    public string Email { get; set; } = string.Empty;
    public bool IsRegisteredUser { get; set; }
    public bool IsGmailBound { get; set; }
    public bool IsNameEdited { get; set; }
}

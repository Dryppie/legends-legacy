namespace Application.WebSockets.Contracts;
public abstract record AudienceDto
{
    public sealed record World : AudienceDto;
    public sealed record Guild(Guid GuildId) : AudienceDto;
}
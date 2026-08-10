using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos.Requests;

public sealed record ChangeGuildMemberRoleDto(Guid CharacterId, GuildRole Role);

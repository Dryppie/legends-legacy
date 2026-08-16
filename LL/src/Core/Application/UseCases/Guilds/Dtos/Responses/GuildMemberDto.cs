using Application.Common.Mappings;
using AutoMapper;
using Domain.Helpers.Constants;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos.Responses;
public class GuildMemberDto : IMapFrom<GuildMember>
{
    public Guid CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public GuildRole Role { get; set; } = GuildRole.Member;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<GuildMember, GuildMemberDto>()
            .ConvertUsing<GuildMemberConverter>();
    }
}

public sealed class GuildMemberConverter : ITypeConverter<GuildMember, GuildMemberDto>
{
    private readonly TimeProvider _timeProvider;

    public GuildMemberConverter(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public GuildMemberDto Convert(GuildMember source, GuildMemberDto destination, ResolutionContext context)
    {
        var lastSeenAt = source.Character?.CharacterAction?.UpdatedAt;

        return new GuildMemberDto
        {
            CharacterId = source.CharacterId,
            Name = source.Character?.Name ?? string.Empty,
            Level = source.Character?.Level ?? 0,
            Role = source.Role,
            JoinedAt = source.JoinedAt,
            LastSeenAt = lastSeenAt,
            IsOnline = lastSeenAt > _timeProvider.GetUtcNow().Subtract(PlayerActivityConstants.OnlineWindow)
        };
    }
}

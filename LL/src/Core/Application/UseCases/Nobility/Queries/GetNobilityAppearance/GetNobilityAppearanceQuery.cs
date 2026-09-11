using Application.MediatR.Markers;
using Domain.Models.Nobility;
using MediatR;

namespace Application.UseCases.Nobility.Queries.GetNobilityAppearance;

public sealed record GetNobilityAppearanceQuery(Guid CharacterId) : IQuery<NobilityAppearanceDto>;
public sealed record NobilityAppearanceDto(DateTimeOffset ServerTime, DateTimeOffset? ExpiresAt,
    bool ShowBadge);

public sealed class GetNobilityAppearanceQueryHandler(INobilityRepository repository, TimeProvider time)
    : IRequestHandler<GetNobilityAppearanceQuery, NobilityAppearanceDto>
{
    public async Task<NobilityAppearanceDto> Handle(GetNobilityAppearanceQuery request, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var character = await repository.GetCharacterAsync(request.CharacterId, ct);
        var membership = character is null ? null : await repository.GetMembershipAsync(character.UserId, ct);
        var active = membership?.At(now);
        return active is null ? new(now, null, false) :
            new(now, active.EndsAt, membership!.ShowBadge);
    }
}

using Application.Interfaces.Services.LL.Prophecies;
using Application.MediatR.Markers;
using Application.UseCases.Prophecies.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Prophecies.Commands.ClaimProphecy;

public sealed record ClaimProphecyCommand(Guid PlayerId, Guid CharacterId, Guid ProphecyId) : ICommand<Response<ProphecyClaimResponseDto>>;

public sealed class ClaimProphecyCommandHandler : IRequestHandler<ClaimProphecyCommand, Response<ProphecyClaimResponseDto>>
{
    private readonly IProphecyService _prophecyService;

    public ClaimProphecyCommandHandler(IProphecyService prophecyService)
    {
        _prophecyService = prophecyService;
    }

    public async Task<Response<ProphecyClaimResponseDto>> Handle(ClaimProphecyCommand request, CancellationToken cancellationToken)
    {
        var result = await _prophecyService.ClaimAsync(
            request.PlayerId,
            request.CharacterId,
            request.ProphecyId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            return Response<ProphecyClaimResponseDto>.Fail(result.Error ?? "Could not claim prophecy.");
        }

        return Response<ProphecyClaimResponseDto>.Success(new ProphecyClaimResponseDto(
            ProphecyDtoMapper.ToDto(result.Value.Prophecy),
            ProphecyDtoMapper.ToDto(result.Value.Reward),
            ProphecyDtoMapper.ToDto(result.Value.WeeklyRevelation, result.Value.WeeklyMilestones)));
    }
}

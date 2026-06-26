using Application.Interfaces.Services.LL.Prophecies;
using Application.MediatR.Markers;
using Application.UseCases.Prophecies.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Prophecies.Commands.ClaimWeeklyRevelationMilestone;

public sealed record ClaimWeeklyRevelationMilestoneCommand(Guid PlayerId, Guid CharacterId, int FavorRequired) : ICommand<Response<ClaimWeeklyRevelationMilestoneResponseDto>>;

public sealed class ClaimWeeklyRevelationMilestoneCommandHandler : IRequestHandler<ClaimWeeklyRevelationMilestoneCommand, Response<ClaimWeeklyRevelationMilestoneResponseDto>>
{
    private readonly IProphecyService _prophecyService;
    private readonly IMapper _mapper;

    public ClaimWeeklyRevelationMilestoneCommandHandler(IProphecyService prophecyService, IMapper mapper)
    {
        _prophecyService = prophecyService;
        _mapper = mapper;
    }

    public async Task<Response<ClaimWeeklyRevelationMilestoneResponseDto>> Handle(ClaimWeeklyRevelationMilestoneCommand request, CancellationToken cancellationToken)
    {
        var result = await _prophecyService.ClaimWeeklyMilestoneAsync(
            request.PlayerId,
            request.CharacterId,
            request.FavorRequired,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            return Response<ClaimWeeklyRevelationMilestoneResponseDto>.Fail(result.Error ?? "Could not claim weekly milestone.");
        }

        return Response<ClaimWeeklyRevelationMilestoneResponseDto>.Success(_mapper.Map<ClaimWeeklyRevelationMilestoneResponseDto>(result.Value));
    }
}

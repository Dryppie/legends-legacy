using Application.Interfaces.Services.LL.Prophecies;
using Application.MediatR.Markers;
using Application.UseCases.Prophecies.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Prophecies.Commands.GetPropheciesOverview;

public sealed record GetPropheciesOverviewCommand(Guid PlayerId, Guid CharacterId) : ICommand<Response<PropheciesOverviewDto>>;

public sealed class GetPropheciesOverviewCommandHandler : IRequestHandler<GetPropheciesOverviewCommand, Response<PropheciesOverviewDto>>
{
    private readonly IProphecyService _prophecyService;

    public GetPropheciesOverviewCommandHandler(IProphecyService prophecyService)
    {
        _prophecyService = prophecyService;
    }

    public async Task<Response<PropheciesOverviewDto>> Handle(GetPropheciesOverviewCommand request, CancellationToken cancellationToken)
    {
        var overview = await _prophecyService.GetOverviewAsync(
            request.PlayerId,
            request.CharacterId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Response<PropheciesOverviewDto>.Success(ProphecyDtoMapper.ToDto(overview));
    }
}

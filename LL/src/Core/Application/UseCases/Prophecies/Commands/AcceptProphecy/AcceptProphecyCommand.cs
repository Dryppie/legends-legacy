using Application.Interfaces.Services.LL.Prophecies;
using Application.MediatR.Markers;
using Application.UseCases.Prophecies.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Prophecies.Commands.AcceptProphecy;

public sealed record AcceptProphecyCommand(Guid PlayerId, Guid CharacterId, Guid ProphecyId) : ICommand<Response<PropheciesOverviewDto>>;

public sealed class AcceptProphecyCommandHandler : IRequestHandler<AcceptProphecyCommand, Response<PropheciesOverviewDto>>
{
    private readonly IProphecyService _prophecyService;

    public AcceptProphecyCommandHandler(IProphecyService prophecyService)
    {
        _prophecyService = prophecyService;
    }

    public async Task<Response<PropheciesOverviewDto>> Handle(AcceptProphecyCommand request, CancellationToken cancellationToken)
    {
        var result = await _prophecyService.AcceptAsync(
            request.PlayerId,
            request.CharacterId,
            request.ProphecyId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return result.Succeeded && result.Value is not null
            ? Response<PropheciesOverviewDto>.Success(ProphecyDtoMapper.ToDto(result.Value))
            : Response<PropheciesOverviewDto>.Fail(result.Error ?? "Could not accept prophecy.");
    }
}

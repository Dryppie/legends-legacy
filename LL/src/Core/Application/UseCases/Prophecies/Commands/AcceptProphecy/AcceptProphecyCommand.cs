using Application.Interfaces.Services.LL.Prophecies;
using Application.MediatR.Markers;
using Application.UseCases.Prophecies.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Prophecies.Commands.AcceptProphecy;

public sealed record AcceptProphecyCommand(Guid PlayerId, Guid CharacterId, Guid ProphecyId) : ICommand<Response<PropheciesOverviewDto>>;

public sealed class AcceptProphecyCommandHandler : IRequestHandler<AcceptProphecyCommand, Response<PropheciesOverviewDto>>
{
    private readonly IProphecyService _prophecyService;
    private readonly IMapper _mapper;

    public AcceptProphecyCommandHandler(IProphecyService prophecyService, IMapper mapper)
    {
        _prophecyService = prophecyService;
        _mapper = mapper;
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
            ? Response<PropheciesOverviewDto>.Success(_mapper.Map<PropheciesOverviewDto>(result.Value))
            : Response<PropheciesOverviewDto>.Fail(result.Error ?? "Could not accept prophecy.");
    }
}

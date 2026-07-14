using Application.Interfaces.Services.LL.Prophecies;
using Application.MediatR.Markers;
using Application.UseCases.Prophecies.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Prophecies.Commands.RerollProphecy;

public sealed record RerollProphecyCommand(
    Guid PlayerId,
    Guid CharacterId) : ICommand<Response<PropheciesOverviewDto>>;

public sealed class RerollProphecyCommandHandler : IRequestHandler<RerollProphecyCommand, Response<PropheciesOverviewDto>>
{
    private readonly IProphecyService _prophecyService;
    private readonly IMapper _mapper;

    public RerollProphecyCommandHandler(IProphecyService prophecyService, IMapper mapper)
    {
        _prophecyService = prophecyService;
        _mapper = mapper;
    }

    public async Task<Response<PropheciesOverviewDto>> Handle(
        RerollProphecyCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _prophecyService.RerollAsync(
            request.PlayerId,
            request.CharacterId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return result.Succeeded && result.Value is not null
            ? Response<PropheciesOverviewDto>.Success(_mapper.Map<PropheciesOverviewDto>(result.Value))
            : Response<PropheciesOverviewDto>.Fail(result.Error ?? "Could not reroll prophecy.");
    }
}

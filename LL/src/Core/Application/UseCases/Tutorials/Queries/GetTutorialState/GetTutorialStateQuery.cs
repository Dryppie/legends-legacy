using Application.Interfaces.Services.LL.Tutorials;
using Application.MediatR.Markers;
using Application.UseCases.Tutorials.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Tutorials.Queries.GetTutorialState;

public sealed record GetTutorialStateQuery(Guid CharacterId) : IQuery<TutorialStateDto?>;

public sealed class GetTutorialStateQueryHandler : IRequestHandler<GetTutorialStateQuery, TutorialStateDto?>
{
    private readonly IMapper _mapper;
    private readonly ITutorialService _tutorialService;

    public GetTutorialStateQueryHandler(
        IMapper mapper,
        ITutorialService tutorialService)
    {
        _mapper = mapper;
        _tutorialService = tutorialService;
    }

    public async Task<TutorialStateDto?> Handle(GetTutorialStateQuery request, CancellationToken cancellationToken)
    {
        var state = await _tutorialService.GetStateAsync(request.CharacterId, cancellationToken);
        return _mapper.Map<TutorialStateDto?>(state);
    }
}

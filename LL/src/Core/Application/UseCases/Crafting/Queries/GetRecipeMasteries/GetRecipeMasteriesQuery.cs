using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.Crafting.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Crafting.Queries.GetRecipeMasteries;

public record GetRecipeMasteriesQuery(Guid CharacterId) : IQuery<Response<IReadOnlyList<RecipeMasteryDto>>>;

public class GetRecipeMasteriesQueryHandler : IRequestHandler<GetRecipeMasteriesQuery, Response<IReadOnlyList<RecipeMasteryDto>>>
{
    private readonly ICraftingProgressionService _progressionService;
    private readonly IMapper _mapper;

    public GetRecipeMasteriesQueryHandler(ICraftingProgressionService progressionService, IMapper mapper)
    {
        _progressionService = progressionService;
        _mapper = mapper;
    }

    public async Task<Response<IReadOnlyList<RecipeMasteryDto>>> Handle(GetRecipeMasteriesQuery request, CancellationToken cancellationToken)
    {
        var masteries = await _progressionService.GetRecipeMasteriesAsync(request.CharacterId, cancellationToken);
        var mapped = _mapper.Map<IReadOnlyList<RecipeMasteryDto>>(masteries);

        return Response<IReadOnlyList<RecipeMasteryDto>>.Success(mapped);
    }
}

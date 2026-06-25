using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.Crafting.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Crafting.Queries.GetBlueprintLearningOptions;

public record GetBlueprintLearningOptionsQuery(Guid CharacterId, Guid BlueprintItemInstanceId)
    : IQuery<Response<IReadOnlyList<BlueprintLearningOptionDto>>>;

public class GetBlueprintLearningOptionsQueryHandler
    : IRequestHandler<GetBlueprintLearningOptionsQuery, Response<IReadOnlyList<BlueprintLearningOptionDto>>>
{
    private readonly ICraftingService _craftingService;

    public GetBlueprintLearningOptionsQueryHandler(ICraftingService craftingService)
    {
        _craftingService = craftingService;
    }

    public async Task<Response<IReadOnlyList<BlueprintLearningOptionDto>>> Handle(
        GetBlueprintLearningOptionsQuery request,
        CancellationToken cancellationToken)
    {
        return await _craftingService.GetBlueprintLearningOptionsAsync(
            request.CharacterId,
            request.BlueprintItemInstanceId,
            cancellationToken);
    }
}

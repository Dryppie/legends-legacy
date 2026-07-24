using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.Crafting.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Crafting.Commands.LearnBlueprint;

public record LearnBlueprintCommand(
    Guid CharacterId,
    Guid BlueprintItemInstanceId,
    string RecipeId) : ICommand<Response<LearnBlueprintResultDto>>;

public class LearnBlueprintCommandHandler : IRequestHandler<LearnBlueprintCommand, Response<LearnBlueprintResultDto>>
{
    private readonly ICraftingService _craftingService;
    private readonly IMapper _mapper;

    public LearnBlueprintCommandHandler(ICraftingService craftingService, IMapper mapper)
    {
        _craftingService = craftingService;
        _mapper = mapper;
    }

    public async Task<Response<LearnBlueprintResultDto>> Handle(LearnBlueprintCommand request, CancellationToken cancellationToken)
    {
        var result = await _craftingService.LearnBlueprintAsync(
            request.CharacterId,
            request.BlueprintItemInstanceId,
            request.RecipeId,
            cancellationToken);

        return result.IsSuccess
            ? Response<LearnBlueprintResultDto>.Success(_mapper.Map<LearnBlueprintResultDto>(result.Data!))
            : Response<LearnBlueprintResultDto>.Fail(result.ErrorMessage);
    }
}

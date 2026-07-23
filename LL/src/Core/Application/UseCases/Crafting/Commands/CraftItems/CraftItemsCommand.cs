using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.Crafting.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Crafting.Commands.CraftItems;

public record CraftItemsCommand(
    Guid CharacterId,
    string RecipeId,
    string? BlueprintId,
    int TargetTier,
    int Quantity) : ICommand<Response<CraftItemsResultDto>>;

public class CraftItemsCommandHandler : IRequestHandler<CraftItemsCommand, Response<CraftItemsResultDto>>
{
    private readonly ICraftingService _craftingService;
    private readonly IMapper _mapper;

    public CraftItemsCommandHandler(ICraftingService craftingService, IMapper mapper)
    {
        _craftingService = craftingService;
        _mapper = mapper;
    }

    public async Task<Response<CraftItemsResultDto>> Handle(CraftItemsCommand request, CancellationToken cancellationToken)
    {
        var result = await _craftingService.CraftItemsAsync(
            request.CharacterId,
            request.RecipeId,
            request.BlueprintId,
            request.TargetTier,
            request.Quantity,
            cancellationToken);

        return result.IsSuccess
            ? Response<CraftItemsResultDto>.Success(_mapper.Map<CraftItemsResultDto>(result.Data!))
            : Response<CraftItemsResultDto>.Fail(result.ErrorMessage);
    }
}

using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Professions.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Professions.Crafting;
using MediatR;

namespace Application.UseCases.Professions.Commands.MoveCraftingQueueItem;

public record MoveCraftingQueueItemCommand(
    Guid CharacterId,
    Guid QueueItemId,
    CraftingQueueMoveDirection Direction)
    : ICommand<Response<MoveCraftingQueueItemResponseDto>>;

public sealed class MoveCraftingQueueItemCommandHandler
    : IRequestHandler<MoveCraftingQueueItemCommand, Response<MoveCraftingQueueItemResponseDto>>
{
    private readonly ICraftingService _craftingService;
    private readonly ICharacterActionService _characterActionService;
    private readonly IMapper _mapper;

    public MoveCraftingQueueItemCommandHandler(
        ICraftingService craftingService,
        ICharacterActionService characterActionService,
        IMapper mapper)
    {
        _craftingService = craftingService;
        _characterActionService = characterActionService;
        _mapper = mapper;
    }

    public async Task<Response<MoveCraftingQueueItemResponseDto>> Handle(
        MoveCraftingQueueItemCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Direction))
        {
            return Response<MoveCraftingQueueItemResponseDto>.Fail(
                "Invalid crafting queue move direction.");
        }

        var moved = await _craftingService.MoveCraftingQueueItemAsync(
            request.CharacterId,
            request.QueueItemId,
            request.Direction,
            cancellationToken);
        if (!moved)
        {
            return Response<MoveCraftingQueueItemResponseDto>.Fail(
                "The crafting queue item cannot be moved in that direction.");
        }

        var action = await _characterActionService.PeekCharacterActionAsync(
            request.CharacterId,
            cancellationToken);
        if (action?.ActionDetails is not Domain.Models.CharacterActions.CharacterActionDetails.CraftingActionDetails)
        {
            return Response<MoveCraftingQueueItemResponseDto>.Fail(
                "Failed to load the updated crafting queue.");
        }

        return Response<MoveCraftingQueueItemResponseDto>.Success(
            new MoveCraftingQueueItemResponseDto
            {
                CurrentAction = _mapper.Map<CharacterActionDto>(action)
            });
    }
}

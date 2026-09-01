using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Professions.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Professions.Commands.SetTemperingRarityStop;

public sealed record SetTemperingRarityStopCommand(
    Guid CharacterId,
    Guid QueueItemId,
    bool Enabled) : ICommand<Response<MoveCraftingQueueItemResponseDto>>;

public sealed class SetTemperingRarityStopCommandHandler
    : IRequestHandler<SetTemperingRarityStopCommand, Response<MoveCraftingQueueItemResponseDto>>
{
    private readonly ICraftingService _craftingService;
    private readonly ICharacterActionService _characterActionService;
    private readonly IMapper _mapper;

    public SetTemperingRarityStopCommandHandler(
        ICraftingService craftingService,
        ICharacterActionService characterActionService,
        IMapper mapper)
    {
        _craftingService = craftingService;
        _characterActionService = characterActionService;
        _mapper = mapper;
    }

    public async Task<Response<MoveCraftingQueueItemResponseDto>> Handle(
        SetTemperingRarityStopCommand request,
        CancellationToken cancellationToken)
    {
        var updated = await _craftingService.SetRemoveAfterNextRarityUpgradeAsync(
            request.CharacterId,
            request.QueueItemId,
            request.Enabled,
            cancellationToken);
        if (!updated)
        {
            return Response<MoveCraftingQueueItemResponseDto>.Fail(
                "The Tempering queue item could not be updated.");
        }

        var action = await _characterActionService.PeekCharacterActionAsync(
            request.CharacterId,
            cancellationToken);
        if (action is null)
        {
            return Response<MoveCraftingQueueItemResponseDto>.Fail(
                "Failed to load the updated Tempering queue.");
        }

        return Response<MoveCraftingQueueItemResponseDto>.Success(
            new MoveCraftingQueueItemResponseDto
            {
                CurrentAction = _mapper.Map<CharacterActionDto>(action)
            });
    }
}

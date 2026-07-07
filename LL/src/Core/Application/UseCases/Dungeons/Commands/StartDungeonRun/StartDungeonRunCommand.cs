using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Outbox;
using AutoMapper;
using Common.Primitives;
using Domain.Components.Attributes;
using Domain.Models.Dungeons.Definitions;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.StartDungeonRun;

public record StartDungeonRunCommand(Guid CharacterId, string DungeonId, DungeonTier DungeonTier) : ICommand<Response<StartDungeonRunResponseDto>>;

public class StartDungeonRunCommandHandler : IRequestHandler<StartDungeonRunCommand, Response<StartDungeonRunResponseDto>>
{
    private readonly IMapper _mapper;
    private readonly IDungeonRunService _dungeonRunService;
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IDungeonAccessPolicy _dungeonAccess;
    private readonly ICharacterService _characters;
    private readonly IInventoryService _inventoryService;
    private readonly IGameEventOutbox _outbox;

    public StartDungeonRunCommandHandler(
        IMapper mapper,
        IDungeonRunService dungeonRunService,
        IDungeonDefinitions dungeonDefinitions,
        IDungeonAccessPolicy dungeonAccess,
        ICharacterService characters,
        IInventoryService inventoryService,
        IGameEventOutbox outbox)
    {
        _mapper = mapper;
        _dungeonRunService = dungeonRunService;
        _dungeonDefinitions = dungeonDefinitions;
        _dungeonAccess = dungeonAccess;
        _characters = characters;
        _inventoryService = inventoryService;
        _outbox = outbox;
    }

    public async Task<Response<StartDungeonRunResponseDto>> Handle(StartDungeonRunCommand request, CancellationToken cancellationToken)
    {
        var dungeonDefinition = _dungeonDefinitions.GetByKey(request.DungeonId);
        var character = await _characters.GetMyCharacterOverviewAsync(request.CharacterId, cancellationToken);
        if (character is null)
            return Response<StartDungeonRunResponseDto>.Fail("Character was not found.");

        var combatRating = CombatRatingCalculator.Calculate(character.BaseCombatAttributes, character.Level);
        var access = await _dungeonAccess.EvaluateAsync(
            request.CharacterId,
            dungeonDefinition,
            combatRating,
            cancellationToken);

        if (!access.CanEnter)
            return Response<StartDungeonRunResponseDto>.Fail(string.Join(" ", access.MissingRequirements));

        var dungeon = await _dungeonRunService.StartRunAsync(request.CharacterId, request.DungeonId, cancellationToken);

        if (dungeon == null)
            return Response<StartDungeonRunResponseDto>.Fail("You already have an ongoing dungeon run.");

        await _outbox.EnqueueAsync(
            GameEventTypes.DungeonRunStarted,
            new DungeonRunStartedPayload(request.CharacterId),
            request.CharacterId,
            null,
            cancellationToken);

        var inventory = await _inventoryService.GetInventoryByIdAsync(request.CharacterId, cancellationToken);

        var result = new StartDungeonRunResponseDto
        {
            Run = _mapper.Map<DungeonRunDto>(dungeon),
            InventoryItems = inventory == null
                ? null
                : _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems)
        };

        return Response<StartDungeonRunResponseDto>.Success(result);
    }
}

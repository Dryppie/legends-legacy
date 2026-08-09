using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.ClaimDungeonRewards;

public record ClaimDungeonRewardsCommand(Guid CharacterId) : ICommand<Response<ClaimDungeonRewardsResponseDto>>;

public class ClaimDungeonRewardsCommandHandler : IRequestHandler<ClaimDungeonRewardsCommand, Response<ClaimDungeonRewardsResponseDto>>
{
    private readonly IDungeonRunService _dungeonRunService;
    private readonly IInventoryService _inventoryService;
    private readonly ICharacterService _characterService;
    private readonly IGameRealtimeBroadcaster _gameRealtime;
    private readonly IMapper _mapper;
    private readonly IGameEventOutbox _outbox;

    public ClaimDungeonRewardsCommandHandler(
        IDungeonRunService dungeonRunService,
        IInventoryService inventoryService,
        ICharacterService characterService,
        IGameRealtimeBroadcaster gameRealtime,
        IMapper mapper,
        IGameEventOutbox outbox)
    {
        _dungeonRunService = dungeonRunService;
        _inventoryService = inventoryService;
        _characterService = characterService;
        _gameRealtime = gameRealtime;
        _mapper = mapper;
        _outbox = outbox;
    }

    public async Task<Response<ClaimDungeonRewardsResponseDto>> Handle(ClaimDungeonRewardsCommand request, CancellationToken cancellationToken)
    {
        var result = await _dungeonRunService.ClaimRewardsAsync(request.CharacterId, cancellationToken);
        if (result == null)
            return Response<ClaimDungeonRewardsResponseDto>.Fail("No completed dungeon run found.");

        if (result.WasCompleted)
        {
            await _outbox.EnqueueAsync(
                GameEventTypes.DungeonRunCompleted,
                new DungeonRunCompletedPayload(
                    request.CharacterId,
                    result.DungeonDefinitionId,
                    result.CompletedWithoutDefeat,
                    result.CompletedWithoutRetreat,
                    result.CompletedWithoutWeapon,
                    result.DefeatedBossKeys),
                request.CharacterId,
                null,
                cancellationToken);
        }

        var inventory = await _inventoryService.GetInventoryByIdAsync(request.CharacterId, cancellationToken);
        var character = await _characterService.GetCharacterByCharacterIdAsync(request.CharacterId, cancellationToken);
        if (inventory == null || character == null)
            return Response<ClaimDungeonRewardsResponseDto>.Fail("Failed to load claimed dungeon rewards.");

        var inventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems);
        var claimedLoot = _mapper.Map<List<InventoryItemDto>>(result.ClaimedLoot);
        var characterDto = _mapper.Map<CharacterDto>(character);

        await _gameRealtime.PublishAsync(
            new Audience.Character(request.CharacterId),
            new DungeonRewardsClaimed(request.CharacterId, claimedLoot),
            nameof(ClaimDungeonRewardsCommandHandler),
            cancellationToken);

        await _gameRealtime.PublishAsync(
            new Audience.Character(request.CharacterId),
            new InventorySnapshot(request.CharacterId, inventoryItems, "dungeon-reward-claim"),
            nameof(ClaimDungeonRewardsCommandHandler),
            cancellationToken);

        await _gameRealtime.PublishAsync(
            new Audience.Character(request.CharacterId),
            new CharacterSnapshot(request.CharacterId, characterDto, "dungeon-reward-claim"),
            nameof(ClaimDungeonRewardsCommandHandler),
            cancellationToken);

        return Response<ClaimDungeonRewardsResponseDto>.Success(new ClaimDungeonRewardsResponseDto
        {
            ActiveRun = null,
            InventoryItems = inventoryItems,
            ClaimedLoot = claimedLoot,
            Character = characterDto
        });
    }
}

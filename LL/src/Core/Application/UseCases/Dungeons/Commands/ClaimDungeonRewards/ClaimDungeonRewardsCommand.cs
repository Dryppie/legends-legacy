using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Inventories.Dtos;
using Application.WebSockets.Contracts;
using Application.WebSockets.Contracts.V2;
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
    private readonly IGameRealtimeBroadcasterV2 _gameRealtimeV2;
    private readonly IMapper _mapper;

    public ClaimDungeonRewardsCommandHandler(
        IDungeonRunService dungeonRunService,
        IInventoryService inventoryService,
        ICharacterService characterService,
        IGameRealtimeBroadcasterV2 gameRealtimeV2,
        IMapper mapper)
    {
        _dungeonRunService = dungeonRunService;
        _inventoryService = inventoryService;
        _characterService = characterService;
        _gameRealtimeV2 = gameRealtimeV2;
        _mapper = mapper;
    }

    public async Task<Response<ClaimDungeonRewardsResponseDto>> Handle(ClaimDungeonRewardsCommand request, CancellationToken cancellationToken)
    {
        var result = await _dungeonRunService.ClaimRewardsAsync(request.CharacterId, cancellationToken);
        if (result == null)
            return Response<ClaimDungeonRewardsResponseDto>.Fail("No completed dungeon run found.");

        var inventory = await _inventoryService.GetInventoryByIdAsync(request.CharacterId, cancellationToken);
        var character = await _characterService.GetCharacterByCharacterIdAsync(request.CharacterId, cancellationToken);
        if (inventory == null || character == null)
            return Response<ClaimDungeonRewardsResponseDto>.Fail("Failed to load claimed dungeon rewards.");

        var inventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems);
        var claimedLoot = _mapper.Map<List<InventoryItemDto>>(result.ClaimedLoot);
        var characterDto = _mapper.Map<CharacterDto>(character);

        await _gameRealtimeV2.PublishAsync(
            new Audience.Character(request.CharacterId),
            new DungeonRewardsClaimedV2(request.CharacterId, claimedLoot),
            nameof(ClaimDungeonRewardsCommandHandler),
            cancellationToken);

        await _gameRealtimeV2.PublishAsync(
            new Audience.Character(request.CharacterId),
            new InventorySnapshotV2(request.CharacterId, inventoryItems, "dungeon-reward-claim"),
            nameof(ClaimDungeonRewardsCommandHandler),
            cancellationToken);

        await _gameRealtimeV2.PublishAsync(
            new Audience.Character(request.CharacterId),
            new CharacterSnapshotV2(request.CharacterId, characterDto, "dungeon-reward-claim"),
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

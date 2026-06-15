using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Inventories.Dtos;
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
    private readonly IMapper _mapper;

    public ClaimDungeonRewardsCommandHandler(
        IDungeonRunService dungeonRunService,
        IInventoryService inventoryService,
        ICharacterService characterService,
        IMapper mapper)
    {
        _dungeonRunService = dungeonRunService;
        _inventoryService = inventoryService;
        _characterService = characterService;
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

        return Response<ClaimDungeonRewardsResponseDto>.Success(new ClaimDungeonRewardsResponseDto
        {
            ActiveRun = null,
            InventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems),
            ClaimedLoot = _mapper.Map<List<InventoryItemDto>>(result.ClaimedLoot),
            Character = _mapper.Map<CharacterDto>(character)
        });
    }
}

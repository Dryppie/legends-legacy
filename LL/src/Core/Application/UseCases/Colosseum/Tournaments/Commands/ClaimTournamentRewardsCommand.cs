using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Inventories;
using Application.Interfaces.WebSockets;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Commands;

[NonTransactional]
public sealed record ClaimTournamentRewardsCommand(Guid CharacterId, Guid? TournamentId)
    : ICommand<Response<ClaimTournamentRewardsResponseDto>>;

public sealed class ClaimTournamentRewardsCommandHandler(
    ITournamentGroundsService service,
    ILootHistoryService lootHistory,
    IGameRealtimeBroadcaster gameRealtime,
    IMapper mapper)
    : IRequestHandler<ClaimTournamentRewardsCommand, Response<ClaimTournamentRewardsResponseDto>>
{
    public async Task<Response<ClaimTournamentRewardsResponseDto>> Handle(
        ClaimTournamentRewardsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.ClaimRewardsAsync(request.CharacterId, request.TournamentId, cancellationToken);
        if (result.InventoryRewards.Count > 0 && result.InventoryGrantId.HasValue)
        {
            var inventoryRewards = mapper.Map<List<InventoryItemDto>>(result.InventoryRewards);
            const string source = "tournament-reward";
            const string location = "Tournament Grounds";

            await lootHistory.RecordAsync(
                request.CharacterId,
                inventoryRewards,
                source,
                location,
                cancellationToken);
            await gameRealtime.PublishAsync(
                new Audience.Character(request.CharacterId),
                new LootReceived(
                    request.CharacterId,
                    inventoryRewards,
                    source,
                    location,
                    result.InventoryGrantId),
                nameof(ClaimTournamentRewardsCommandHandler),
                cancellationToken);
        }

        return Response<ClaimTournamentRewardsResponseDto>.Success(mapper.Map<ClaimTournamentRewardsResponseDto>(result));
    }
}

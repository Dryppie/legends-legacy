using Application.Interfaces.Services.LL.Inventories;
using Application.Interfaces.Services.LL.Prophecies;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Prophecies.Dtos;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Prophecies.Commands.OpenProphecyCache;

public sealed record OpenProphecyCacheCommand(Guid CharacterId, string CacheItemId) : ICommand<Response<OpenProphecyCacheResponseDto>>;

public sealed class OpenProphecyCacheCommandHandler : IRequestHandler<OpenProphecyCacheCommand, Response<OpenProphecyCacheResponseDto>>
{
    private readonly IProphecyService _prophecyService;
    private readonly ILootHistoryService _lootHistory;
    private readonly IGameEventPublisher _legacyEvents;
    private readonly IGameRealtimeBroadcaster _gameRealtime;
    private readonly IMapper _mapper;

    public OpenProphecyCacheCommandHandler(
        IProphecyService prophecyService,
        ILootHistoryService lootHistory,
        IGameEventPublisher legacyEvents,
        IGameRealtimeBroadcaster gameRealtime,
        IMapper mapper)
    {
        _prophecyService = prophecyService;
        _lootHistory = lootHistory;
        _legacyEvents = legacyEvents;
        _gameRealtime = gameRealtime;
        _mapper = mapper;
    }

    public async Task<Response<OpenProphecyCacheResponseDto>> Handle(OpenProphecyCacheCommand request, CancellationToken cancellationToken)
    {
        var result = await _prophecyService.OpenCacheAsync(
            request.CharacterId,
            request.CacheItemId,
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            return Response<OpenProphecyCacheResponseDto>.Fail(result.Error ?? "Could not open prophecy cache.");
        }

        var rewards = _mapper.Map<List<InventoryItemDto>>(result.Value.Rewards);
        if (rewards.Count > 0)
        {
            var grantId = Guid.NewGuid();
            const string source = "container-reward";

            await _lootHistory.RecordAsync(
                request.CharacterId,
                rewards,
                source,
                result.Value.CacheTitle,
                cancellationToken);

            await _legacyEvents.PublishAsync(
                new Audience.Character(request.CharacterId),
                new LootReceivedMsg(request.CharacterId, rewards, source, result.Value.CacheTitle, grantId));
            await _gameRealtime.PublishAsync(
                new Audience.Character(request.CharacterId),
                new LootReceived(request.CharacterId, rewards, source, result.Value.CacheTitle, grantId),
                nameof(OpenProphecyCacheCommandHandler),
                cancellationToken);
        }

        return Response<OpenProphecyCacheResponseDto>.Success(_mapper.Map<OpenProphecyCacheResponseDto>(result.Value));
    }
}

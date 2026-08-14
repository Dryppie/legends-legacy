using Application.Interfaces.Services.LL.Inventories;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Inventories.Commands.OpenCatalystSelectionCrate;

public sealed record OpenCatalystSelectionCrateCommand(
    Guid CharacterId,
    Guid CrateItemInstanceId,
    string OptionId) : ICommand<Response<OpenSelectionCrateResultDto>>;

public sealed class OpenCatalystSelectionCrateCommandHandler
    : IRequestHandler<OpenCatalystSelectionCrateCommand, Response<OpenSelectionCrateResultDto>>
{
    private readonly ISelectionCrateService _selectionCrates;
    private readonly ILootHistoryService _lootHistory;
    private readonly IGameEventPublisher _legacyEvents;
    private readonly IGameRealtimeBroadcaster _gameRealtime;
    private readonly IMapper _mapper;

    public OpenCatalystSelectionCrateCommandHandler(
        ISelectionCrateService selectionCrates,
        ILootHistoryService lootHistory,
        IGameEventPublisher legacyEvents,
        IGameRealtimeBroadcaster gameRealtime,
        IMapper mapper)
    {
        _selectionCrates = selectionCrates;
        _lootHistory = lootHistory;
        _legacyEvents = legacyEvents;
        _gameRealtime = gameRealtime;
        _mapper = mapper;
    }

    public async Task<Response<OpenSelectionCrateResultDto>> Handle(
        OpenCatalystSelectionCrateCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _selectionCrates.OpenSelectionContainerAsync(
            request.CharacterId,
            request.CrateItemInstanceId,
            request.OptionId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Response<OpenSelectionCrateResultDto>.Fail(
                result.ErrorMessage ?? "The selection container could not be opened.");
        }

        var rewards = _mapper.Map<List<InventoryItemDto>>(result.Rewards);
        var grantId = Guid.NewGuid();
        const string source = "container-reward";

        await _lootHistory.RecordAsync(
            request.CharacterId,
            rewards,
            source,
            result.ContainerName,
            cancellationToken);

        await _legacyEvents.PublishAsync(
            new Audience.Character(request.CharacterId),
            new LootReceivedMsg(request.CharacterId, rewards, source, result.ContainerName, grantId));
        await _gameRealtime.PublishAsync(
            new Audience.Character(request.CharacterId),
            new LootReceived(request.CharacterId, rewards, source, result.ContainerName, grantId),
            nameof(OpenCatalystSelectionCrateCommandHandler),
            cancellationToken);

        return Response<OpenSelectionCrateResultDto>.Success(new OpenSelectionCrateResultDto
        {
            ConsumedItemInstanceId = request.CrateItemInstanceId,
            GrantId = grantId,
            Rewards = rewards
        });
    }
}

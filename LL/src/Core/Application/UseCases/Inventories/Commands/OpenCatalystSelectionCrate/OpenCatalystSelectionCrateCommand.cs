using Application.Interfaces.Services.LL;
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
    private readonly IInventoryService _inventory;
    private readonly IGameRealtimeBroadcaster _gameRealtime;
    private readonly IMapper _mapper;

    public OpenCatalystSelectionCrateCommandHandler(
        ISelectionCrateService selectionCrates,
        IInventoryService inventory,
        ILootHistoryService lootHistory,
        IGameRealtimeBroadcaster gameRealtime,
        IMapper mapper)
    {
        _selectionCrates = selectionCrates;
        _inventory = inventory;
        _lootHistory = lootHistory;
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

        await _gameRealtime.PublishAsync(
            new Audience.Character(request.CharacterId),
            new LootReceived(request.CharacterId, rewards, source, result.ContainerName, grantId),
            nameof(OpenCatalystSelectionCrateCommandHandler),
            cancellationToken);

        var inventory = await _inventory.GetInventoryByIdAsync(
            request.CharacterId,
            cancellationToken);
        if (inventory is null)
            return Response<OpenSelectionCrateResultDto>.Fail(
                "The inventory could not be loaded.");

        return Response<OpenSelectionCrateResultDto>.Success(new OpenSelectionCrateResultDto
        {
            ConsumedItemInstanceId = request.CrateItemInstanceId,
            GrantId = grantId,
            Rewards = rewards,
            InventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems)
        });
    }
}

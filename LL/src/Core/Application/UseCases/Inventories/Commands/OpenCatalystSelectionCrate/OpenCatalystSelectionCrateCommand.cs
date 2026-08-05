using Application.Interfaces.Services.LL.Inventories;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
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
    private readonly IMapper _mapper;

    public OpenCatalystSelectionCrateCommandHandler(
        ISelectionCrateService selectionCrates,
        IMapper mapper)
    {
        _selectionCrates = selectionCrates;
        _mapper = mapper;
    }

    public async Task<Response<OpenSelectionCrateResultDto>> Handle(
        OpenCatalystSelectionCrateCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _selectionCrates.OpenCatalystSelectionCrateAsync(
            request.CharacterId,
            request.CrateItemInstanceId,
            request.OptionId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Response<OpenSelectionCrateResultDto>.Fail(
                result.ErrorMessage ?? "The Catalyst Selection Crate could not be opened.");
        }

        return Response<OpenSelectionCrateResultDto>.Success(new OpenSelectionCrateResultDto
        {
            ConsumedItemInstanceId = request.CrateItemInstanceId,
            Rewards = _mapper.Map<List<InventoryItemDto>>(result.Rewards)
        });
    }
}

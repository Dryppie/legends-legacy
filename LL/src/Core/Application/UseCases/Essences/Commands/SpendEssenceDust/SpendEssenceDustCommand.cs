using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.SpendEssenceDust;

public record SpendEssenceDustCommand(Guid CharacterId, Guid PlayerEssenceId, int DustAmount) : ICommand<Response<EssenceMutationResponseDto>>;

public class SpendEssenceDustCommandHandler : IRequestHandler<SpendEssenceDustCommand, Response<EssenceMutationResponseDto>>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;
    private readonly IInventoryService _inventoryService;

    public SpendEssenceDustCommandHandler(
        IMapper mapper,
        IEssenceService service,
        IInventoryService inventoryService)
    {
        _mapper = mapper;
        _service = service;
        _inventoryService = inventoryService;
    }

    public async Task<Response<EssenceMutationResponseDto>> Handle(SpendEssenceDustCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.SpendEssenceDustAsync(request.CharacterId, request.PlayerEssenceId, request.DustAmount, cancellationToken);
        if (!result.Succeeded)
            return Response<EssenceMutationResponseDto>.Fail(result.Message);

        var archive = await _service.GetSoulArchiveAsync(request.CharacterId, cancellationToken);
        var inventory = await _inventoryService.GetInventoryByIdAsync(request.CharacterId, cancellationToken);
        if (inventory == null)
            return Response<EssenceMutationResponseDto>.Fail("Failed to load updated Essence state.");

        return Response<EssenceMutationResponseDto>.Success(new EssenceMutationResponseDto
        {
            Succeeded = result.Succeeded,
            Message = result.Message,
            Archive = _mapper.Map<SoulArchiveDto>(archive),
            InventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems),
            DustSpent = result.DustSpent,
            XpGained = result.XpGained,
            LevelsGained = result.LevelsGained,
            ReachedTierCap = result.ReachedTierCap
        });
    }
}

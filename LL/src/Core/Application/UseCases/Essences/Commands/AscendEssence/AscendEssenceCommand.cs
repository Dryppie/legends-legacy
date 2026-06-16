using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.AscendEssence;

public record AscendEssenceCommand(Guid CharacterId, Guid PlayerEssenceId) : ICommand<Response<EssenceMutationResponseDto>>;

public class AscendEssenceCommandHandler : IRequestHandler<AscendEssenceCommand, Response<EssenceMutationResponseDto>>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;
    private readonly IInventoryService _inventoryService;

    public AscendEssenceCommandHandler(
        IMapper mapper,
        IEssenceService service,
        IInventoryService inventoryService)
    {
        _mapper = mapper;
        _service = service;
        _inventoryService = inventoryService;
    }

    public async Task<Response<EssenceMutationResponseDto>> Handle(AscendEssenceCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.AscendEssenceAsync(request.CharacterId, request.PlayerEssenceId, cancellationToken);
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
            InventoryItems = _mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems)
        });
    }
}

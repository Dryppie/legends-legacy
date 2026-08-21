using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Commands;
using Application.UseCases.Essences.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.DismantleUnboundEssence;

public record DismantleUnboundEssenceCommand(Guid CharacterId, Guid InventoryItemId) : ICommand<Response<EssenceMutationResponseDto>>;

public class DismantleUnboundEssenceCommandHandler : IRequestHandler<DismantleUnboundEssenceCommand, Response<EssenceMutationResponseDto>>
{
    private readonly IEssenceService _service;
    private readonly EssenceMutationResponseFactory _responses;

    public DismantleUnboundEssenceCommandHandler(
        IEssenceService service,
        EssenceMutationResponseFactory responses)
    {
        _service = service;
        _responses = responses;
    }

    public async Task<Response<EssenceMutationResponseDto>> Handle(DismantleUnboundEssenceCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.DismantleUnboundEssenceAsync(request.CharacterId, request.InventoryItemId, cancellationToken);
        if (!result.Succeeded)
            return Response<EssenceMutationResponseDto>.Fail(result.Message);

        var response = await _responses.CreateAsync(
            request.CharacterId,
            result.Succeeded,
            result.Message,
            cancellationToken,
            dustGained: result.DustGained);
        if (response is null)
            return Response<EssenceMutationResponseDto>.Fail("Failed to load updated Essence state.");

        return Response<EssenceMutationResponseDto>.Success(response);
    }
}

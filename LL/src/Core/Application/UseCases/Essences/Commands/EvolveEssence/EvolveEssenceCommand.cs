using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Commands;
using Application.UseCases.Essences.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.EvolveEssence;

public record EvolveEssenceCommand(Guid CharacterId, Guid PlayerEssenceId) : ICommand<Response<EssenceMutationResponseDto>>;

public class EvolveEssenceCommandHandler : IRequestHandler<EvolveEssenceCommand, Response<EssenceMutationResponseDto>>
{
    private readonly IEssenceService _service;
    private readonly EssenceMutationResponseFactory _responses;

    public EvolveEssenceCommandHandler(
        IEssenceService service,
        EssenceMutationResponseFactory responses)
    {
        _service = service;
        _responses = responses;
    }

    public async Task<Response<EssenceMutationResponseDto>> Handle(EvolveEssenceCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.EvolveEssenceAsync(request.CharacterId, request.PlayerEssenceId, cancellationToken);
        if (!result.Succeeded)
            return Response<EssenceMutationResponseDto>.Fail(result.Message);

        var response = await _responses.CreateAsync(
            request.CharacterId,
            result.Succeeded,
            result.Message,
            cancellationToken);
        if (response is null)
            return Response<EssenceMutationResponseDto>.Fail("Failed to load updated Essence state.");

        return Response<EssenceMutationResponseDto>.Success(response);
    }
}

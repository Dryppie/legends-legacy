using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Commands;
using Application.UseCases.Essences.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.AscendEssence;

public record AscendEssenceCommand(Guid CharacterId, Guid PlayerEssenceId) : ICommand<Response<EssenceMutationResponseDto>>;

public class AscendEssenceCommandHandler : IRequestHandler<AscendEssenceCommand, Response<EssenceMutationResponseDto>>
{
    private readonly IEssenceService _service;
    private readonly EssenceMutationResponseFactory _responses;

    public AscendEssenceCommandHandler(
        IEssenceService service,
        EssenceMutationResponseFactory responses)
    {
        _service = service;
        _responses = responses;
    }

    public async Task<Response<EssenceMutationResponseDto>> Handle(AscendEssenceCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.AscendEssenceAsync(request.CharacterId, request.PlayerEssenceId, cancellationToken);
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

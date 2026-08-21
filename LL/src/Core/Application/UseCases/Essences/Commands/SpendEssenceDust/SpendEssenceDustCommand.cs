using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Commands;
using Application.UseCases.Essences.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.SpendEssenceDust;

public record SpendEssenceDustCommand(Guid CharacterId, Guid PlayerEssenceId, int DustAmount) : ICommand<Response<EssenceMutationResponseDto>>;

public class SpendEssenceDustCommandHandler : IRequestHandler<SpendEssenceDustCommand, Response<EssenceMutationResponseDto>>
{
    private readonly IEssenceService _service;
    private readonly EssenceMutationResponseFactory _responses;

    public SpendEssenceDustCommandHandler(
        IEssenceService service,
        EssenceMutationResponseFactory responses)
    {
        _service = service;
        _responses = responses;
    }

    public async Task<Response<EssenceMutationResponseDto>> Handle(SpendEssenceDustCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.SpendEssenceDustAsync(request.CharacterId, request.PlayerEssenceId, request.DustAmount, cancellationToken);
        if (!result.Succeeded)
            return Response<EssenceMutationResponseDto>.Fail(result.Message);

        var response = await _responses.CreateAsync(
            request.CharacterId,
            result.Succeeded,
            result.Message,
            cancellationToken,
            dustSpent: result.DustSpent,
            xpGained: result.XpGained,
            levelsGained: result.LevelsGained,
            reachedTierCap: result.ReachedTierCap);
        if (response is null)
            return Response<EssenceMutationResponseDto>.Fail("Failed to load updated Essence state.");

        return Response<EssenceMutationResponseDto>.Success(response);
    }
}

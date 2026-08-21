using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Commands;
using Application.UseCases.Essences.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.FavoriteEssence;

public record FavoriteEssenceCommand(Guid CharacterId, Guid PlayerEssenceId, bool IsFavorite) : ICommand<Response<EssenceStateResponseDto>>;

public class FavoriteEssenceCommandHandler : IRequestHandler<FavoriteEssenceCommand, Response<EssenceStateResponseDto>>
{
    private readonly IEssenceService _service;
    private readonly EssenceMutationResponseFactory _responses;

    public FavoriteEssenceCommandHandler(
        IEssenceService service,
        EssenceMutationResponseFactory responses)
    {
        _service = service;
        _responses = responses;
    }

    public async Task<Response<EssenceStateResponseDto>> Handle(FavoriteEssenceCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.SetFavoriteAsync(request.CharacterId, request.PlayerEssenceId, request.IsFavorite, cancellationToken);
        if (!result.Succeeded)
            return Response<EssenceStateResponseDto>.Fail(result.Message);

        return Response<EssenceStateResponseDto>.Success(await _responses.CreateStateAsync(
            request.CharacterId,
            result.Succeeded,
            result.Message,
            cancellationToken));
    }
}

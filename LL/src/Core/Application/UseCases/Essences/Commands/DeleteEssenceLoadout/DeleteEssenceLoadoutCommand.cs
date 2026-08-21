using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Commands;
using Application.UseCases.Essences.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.DeleteEssenceLoadout;

public record DeleteEssenceLoadoutCommand(Guid CharacterId, Guid LoadoutId) : ICommand<Response<EssenceStateResponseDto>>;

public class DeleteEssenceLoadoutCommandHandler : IRequestHandler<DeleteEssenceLoadoutCommand, Response<EssenceStateResponseDto>>
{
    private readonly IEssenceService _service;
    private readonly EssenceMutationResponseFactory _responses;

    public DeleteEssenceLoadoutCommandHandler(
        IEssenceService service,
        EssenceMutationResponseFactory responses)
    {
        _service = service;
        _responses = responses;
    }

    public async Task<Response<EssenceStateResponseDto>> Handle(DeleteEssenceLoadoutCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteLoadoutAsync(request.CharacterId, request.LoadoutId, cancellationToken);
        if (!result.Succeeded)
            return Response<EssenceStateResponseDto>.Fail(result.Message);

        return Response<EssenceStateResponseDto>.Success(await _responses.CreateStateAsync(
            request.CharacterId,
            result.Succeeded,
            result.Message,
            cancellationToken,
            removedLoadoutId: request.LoadoutId));
    }
}

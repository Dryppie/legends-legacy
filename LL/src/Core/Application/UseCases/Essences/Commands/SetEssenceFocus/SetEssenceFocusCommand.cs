using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Commands;
using Application.UseCases.Essences.Dtos;
using MediatR;

namespace Application.UseCases.Essences.Commands.SetEssenceFocus;

public record SetEssenceFocusCommand(Guid CharacterId, string? CreatureId) : ICommand<EssenceStateResponseDto>;

public sealed class SetEssenceFocusCommandHandler : IRequestHandler<SetEssenceFocusCommand, EssenceStateResponseDto>
{
    private readonly ICreatureArchiveService _service;
    private readonly EssenceMutationResponseFactory _responses;

    public SetEssenceFocusCommandHandler(
        ICreatureArchiveService service,
        EssenceMutationResponseFactory responses)
    {
        _service = service;
        _responses = responses;
    }

    public async Task<EssenceStateResponseDto> Handle(SetEssenceFocusCommand request, CancellationToken cancellationToken)
    {
        await _service.SetEssenceFocusAsync(request.CharacterId, request.CreatureId, cancellationToken);
        return await _responses.CreateStateAsync(
            request.CharacterId,
            true,
            "Essence Focus updated.",
            cancellationToken);
    }
}

using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Commands;
using Application.UseCases.Essences.Dtos;
using MediatR;

namespace Application.UseCases.Essences.Commands.SetCreatureFocus;

public record SetCreatureFocusCommand(Guid CharacterId, string? CreatureId) : ICommand<EssenceStateResponseDto>;

public sealed class SetCreatureFocusCommandHandler : IRequestHandler<SetCreatureFocusCommand, EssenceStateResponseDto>
{
    private readonly ICreatureArchiveService _service;
    private readonly EssenceMutationResponseFactory _responses;
    private readonly ICharacterActionService _actions;

    public SetCreatureFocusCommandHandler(
        ICreatureArchiveService service,
        EssenceMutationResponseFactory responses,
        ICharacterActionService actions)
    {
        _service = service;
        _responses = responses;
        _actions = actions;
    }

    public async Task<EssenceStateResponseDto> Handle(SetCreatureFocusCommand request, CancellationToken cancellationToken)
    {
        // Elapsed combat belongs to the previous focus. Settle it before switching.
        var action = await _actions.GetCharacterActionAsync(request.CharacterId, cancellationToken);
        if (action?.HasMoreDueWork == true)
        {
            return await _responses.CreateStateAsync(
                request.CharacterId,
                false,
                "Combat progress is still catching up. Try changing Creature Focus again once it finishes.",
                cancellationToken);
        }

        await _service.SetCreatureFocusAsync(request.CharacterId, request.CreatureId, cancellationToken);
        return await _responses.CreateStateAsync(
            request.CharacterId,
            true,
            "Creature Focus updated.",
            cancellationToken);
    }
}

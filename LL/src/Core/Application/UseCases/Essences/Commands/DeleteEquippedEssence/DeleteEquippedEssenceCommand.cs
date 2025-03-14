using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases.Essences.Commands.DeleteEquippedEssence;
public record DeleteEquippedEssenceCommand(Guid CharacterId, string EssenceId) : IRequest<bool>;

public class DeleteEquippedEssenceCommandHandler : IRequestHandler<DeleteEquippedEssenceCommand, bool>
{
    private readonly IEssenceService _essenceService;
    public DeleteEquippedEssenceCommandHandler(IEssenceService essenceService)
    {
        _essenceService = essenceService;
    }

    public async Task<bool> Handle(DeleteEquippedEssenceCommand request, CancellationToken cancellationToken)
    {
        return await _essenceService.DeleteEquippedEssence(request.CharacterId, Guid.Parse(request.EssenceId), cancellationToken);
    }
}

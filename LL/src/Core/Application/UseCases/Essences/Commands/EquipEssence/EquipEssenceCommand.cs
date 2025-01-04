using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases.Essences.Commands.EquipEssence;
public record EquipEssenceCommand(Guid CharacterId, string EssenceItemId) : IRequest;

public class EquipEssenceCommandHandler : IRequestHandler<EquipEssenceCommand>
{
    private readonly IEssenceService _essenceService;
    public EquipEssenceCommandHandler(IEssenceService essenceService)
    {
        _essenceService = essenceService;
    }

    public Task Handle(EquipEssenceCommand request, CancellationToken cancellationToken)
    {
        return _essenceService.EquipEssence(request.CharacterId, Guid.Parse(request.EssenceItemId), cancellationToken);
    }
}

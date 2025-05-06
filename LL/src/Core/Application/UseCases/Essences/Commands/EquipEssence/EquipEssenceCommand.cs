using Application.Interfaces.Services.LL.Essences;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.EquipEssence;
public record EquipEssenceCommand(Guid CharacterId, string EssenceItemId) : IRequest<Response<bool>>;

public class EquipEssenceCommandHandler : IRequestHandler<EquipEssenceCommand, Response<bool>>
{
    private readonly IEssenceService _essenceService;
    public EquipEssenceCommandHandler(IEssenceService essenceService)
    {
        _essenceService = essenceService;
    }

    public async Task<Response<bool>> Handle(EquipEssenceCommand request, CancellationToken cancellationToken)
    {
        return await _essenceService.EquipEssence(request.CharacterId, Guid.Parse(request.EssenceItemId), cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to absorb essence.");
    }
}

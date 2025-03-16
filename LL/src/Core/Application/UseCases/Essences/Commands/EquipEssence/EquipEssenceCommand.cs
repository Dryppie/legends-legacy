using Application.Common.Responses;
using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases.Essences.Commands.EquipEssence;
public record EquipEssenceCommand(Guid CharacterId, string EssenceItemId) : IRequest<Response<Unit>>;

public class EquipEssenceCommandHandler : IRequestHandler<EquipEssenceCommand, Response<Unit>>
{
    private readonly IEssenceService _essenceService;
    public EquipEssenceCommandHandler(IEssenceService essenceService)
    {
        _essenceService = essenceService;
    }

    public Task<Response<Unit>> Handle(EquipEssenceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _essenceService.EquipEssence(request.CharacterId, Guid.Parse(request.EssenceItemId), cancellationToken);
            return Task.FromResult(Response<Unit>.Success(Unit.Value));
        }
        catch (Exception)
        {
            return Task.FromResult(Response<Unit>.Fail("Error equipping essence: " + request.EssenceItemId));
        }
    }
}

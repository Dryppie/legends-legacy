using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Equipments.Commands.CopyEquipmentLoadout;

public sealed record CopyEquipmentLoadoutCommand(Guid CharacterId, Guid SourceId, Guid TargetId) : ICommand<Response<bool>>;
public sealed class CopyEquipmentLoadoutCommandHandler(IEquipmentLoadoutService service) : IRequestHandler<CopyEquipmentLoadoutCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(CopyEquipmentLoadoutCommand request, CancellationToken ct)
    {
        var result = await service.CopyAsync(request.CharacterId, request.SourceId, request.TargetId, ct);
        return result.Succeeded ? Response<bool>.Success(true) : Response<bool>.Fail(result.ErrorMessage ?? "Preset could not be copied.");
    }
}

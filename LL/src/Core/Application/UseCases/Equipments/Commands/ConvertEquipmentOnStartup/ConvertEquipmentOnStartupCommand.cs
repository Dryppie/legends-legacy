using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.ConvertEquipmentOnStartup;

// Internal startup command; intentionally not exposed by the operator HTTP controller.
public sealed record ConvertEquipmentOnStartupCommand(Guid OperationId, EquipmentMigrationTarget Target,
    int TargetBalanceVersion) : ICommand<bool>;

public sealed class ConvertEquipmentOnStartupCommandHandler(IEquipmentMigrationService service)
    : IRequestHandler<ConvertEquipmentOnStartupCommand, bool>
{
    public Task<bool> Handle(ConvertEquipmentOnStartupCommand request, CancellationToken ct) =>
        service.ConvertOnStartupAsync(request.OperationId, request.Target, request.TargetBalanceVersion, ct);
}

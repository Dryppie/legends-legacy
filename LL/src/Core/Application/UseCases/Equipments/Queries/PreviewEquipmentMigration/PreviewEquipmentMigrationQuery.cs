using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Queries.PreviewEquipmentMigration;

public sealed record PreviewEquipmentMigrationQuery(EquipmentMigrationTarget Target, string? DefinitionId = null, int? TargetBalanceVersion = null) : IQuery<Response<EquipmentMigrationPreview>>;

public sealed class PreviewEquipmentMigrationQueryHandler(IEquipmentMigrationService service) : IRequestHandler<PreviewEquipmentMigrationQuery, Response<EquipmentMigrationPreview>>
{
    public async Task<Response<EquipmentMigrationPreview>> Handle(PreviewEquipmentMigrationQuery request, CancellationToken ct)
    {
        try
        {
            return Response<EquipmentMigrationPreview>.Success(await service.PreviewAsync(request.Target, request.DefinitionId, ct, request.TargetBalanceVersion));
        }
        catch (InvalidOperationException exception)
        {
            return Response<EquipmentMigrationPreview>.Fail(exception.Message, "equipment_migration_preview_blocked");
        }
    }
}

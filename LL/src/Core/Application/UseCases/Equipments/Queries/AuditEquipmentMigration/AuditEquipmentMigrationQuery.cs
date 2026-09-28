using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Queries.AuditEquipmentMigration;

public sealed record AuditEquipmentMigrationQuery(int Page = 0, int PageSize = 100, int SourceBalanceVersion = 1) : IQuery<Response<EquipmentMigrationAudit>>;

public sealed class AuditEquipmentMigrationQueryHandler(IEquipmentMigrationRepository repository) : IRequestHandler<AuditEquipmentMigrationQuery, Response<EquipmentMigrationAudit>>
{
    public async Task<Response<EquipmentMigrationAudit>> Handle(AuditEquipmentMigrationQuery request, CancellationToken ct)
    {
        return Response<EquipmentMigrationAudit>.Success(await repository.AuditAsync(request.Page, request.PageSize, ct, request.SourceBalanceVersion));
    }
}

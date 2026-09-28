using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Commands.ChooseMigratedSpecialization;

public sealed record ChooseMigratedSpecializationCommand(Guid CharacterId, Guid MigrationId, Guid ChoiceOperationId, string DefinitionId) : ICommand<Response<EquipmentData>>;

public sealed class ChooseMigratedSpecializationCommandHandler(IEquipmentMigrationService service) : IRequestHandler<ChooseMigratedSpecializationCommand, Response<EquipmentData>>
{
    public async Task<Response<EquipmentData>> Handle(ChooseMigratedSpecializationCommand request, CancellationToken ct)
    {
        return Response<EquipmentData>.Success(await service.ChooseAsync(request.CharacterId, request.MigrationId, request.ChoiceOperationId, request.DefinitionId, ct));
    }
}

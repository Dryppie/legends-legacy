namespace Domain.Models.Items.Equipments.Progression;

public sealed record EquipmentGrantRequest(string DefinitionId, int Tier, int Rank, string? ActiveStyleId);

/// <summary>Support awards never manufacture refundable spending or discovery ownership.</summary>
public static class EquipmentProgressionAdministrativeEquipment
{
    public const int MaximumQuantity = 100;

    public static EquipmentData Create(EquipmentEvaluator evaluator, EquipmentGrantRequest request,
        Guid ownerId, Guid instanceId, Guid operationId)
    {
        var state = EquipmentState.Award(instanceId, evaluator, request.DefinitionId, request.Tier, request.Rank,
            new(EquipmentAwardKind.Administrative, "admin-compensation", operationId.ToString("N")),
            new(EquipmentOwnershipKind.BoundPersonal, ownerId));
        // An explicit support style does not grant character-wide Blueprint knowledge or paid investment.
        evaluator.Evaluate(request.DefinitionId, request.Tier, request.Rank, request.ActiveStyleId);
        state = EquipmentState.Restore(state.ToSnapshot() with { ActiveStyleId = request.ActiveStyleId });
        return EquipmentData.Create(state, evaluator);
    }
}

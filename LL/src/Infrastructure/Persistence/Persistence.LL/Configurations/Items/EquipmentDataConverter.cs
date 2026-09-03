using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Persistence.LL.Configurations.Items;

// Immutable descriptors are replaced as a whole; EF handles database nulls separately.
public sealed class EquipmentDataConverter() : ValueConverter<EquipmentData, string>(
    data => data.Serialize(), json => EquipmentData.Deserialize(json));

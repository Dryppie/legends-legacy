using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Snapshots;

public sealed class EquipmentSnapshotConfiguration : IEntityTypeConfiguration<EquipmentSnapshot>
{
    public void Configure(EntityTypeBuilder<EquipmentSnapshot> builder)
    {
        builder.Property(snapshot => snapshot.ProgressionData).HasColumnName("ModelEData")
            .HasConversion<Items.EquipmentDataConverter>().HasColumnType("jsonb");
    }
}

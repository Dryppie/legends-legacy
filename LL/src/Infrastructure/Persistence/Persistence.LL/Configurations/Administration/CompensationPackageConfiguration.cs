using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Persistence.LL.Configurations.Administration;
public sealed class CompensationPackageConfiguration : IEntityTypeConfiguration<CompensationPackageVersion>
{
    public void Configure(EntityTypeBuilder<CompensationPackageVersion> b)
    {
        b.ToTable("CompensationPackageVersions"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PackageId, x.Version }).IsUnique();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Purpose).HasMaxLength(1000).IsRequired();
        b.Property(x => x.ItemsJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.ActorSubject).HasMaxLength(320).IsRequired();
    }
}

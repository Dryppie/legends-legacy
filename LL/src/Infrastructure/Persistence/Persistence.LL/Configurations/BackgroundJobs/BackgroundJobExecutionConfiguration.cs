using Domain.Models.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.BackgroundJobs;

public sealed class BackgroundJobExecutionConfiguration : IEntityTypeConfiguration<BackgroundJobExecution>
{
    public void Configure(EntityTypeBuilder<BackgroundJobExecution> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.BusinessKey)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.ConcurrencyStamp)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.JobName, x.BusinessKey })
            .IsUnique();
    }
}

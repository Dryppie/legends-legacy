using Domain.Models.Colosseum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum;
public class ArenaTicketStatusConfiguration : IEntityTypeConfiguration<ArenaTicketStatus>
{
    public void Configure(EntityTypeBuilder<ArenaTicketStatus> builder)
    {
        builder.HasKey(b => b.CharacterId);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ArenaTicketStatus_CurrentTickets_Range",
            "\"CurrentTickets\" >= 0 AND \"CurrentTickets\" <= 5"));
    }
}

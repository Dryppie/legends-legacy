using Domain.Models.Attributes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.LL.Configurations;
public class EntityAttributeConfiguration : IEntityTypeConfiguration<EntityAttribute>
{
    public void Configure(EntityTypeBuilder<EntityAttribute> builder)
    {
        builder.HasKey(ea => new { ea.EntityId, ea.AttributeType });
    }
}
using Domain.Models.Prophecies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Persistence.LL.Configurations.Prophecies;

public sealed class ProphecyDefinitionConfiguration : IEntityTypeConfiguration<ProphecyDefinition>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ValueComparer<List<string>> StringListComparer = new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        value => value.ToList());

    public void Configure(EntityTypeBuilder<ProphecyDefinition> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(128);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.FlavorText).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ObjectiveText).HasMaxLength(240).IsRequired();
        builder.Property(x => x.ObjectiveType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ObjectiveParameterJson).HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.RewardProfileId).HasMaxLength(80).IsRequired();

        ConfigureStringList(builder.Property(x => x.AllowedSlots));
        ConfigureStringList(builder.Property(x => x.RequiredFeatures));
        ConfigureStringList(builder.Property(x => x.RequiredTags));
        ConfigureStringList(builder.Property(x => x.ExcludedTags));

        builder.HasIndex(x => new { x.Scope, x.Category, x.Difficulty, x.IsEnabled });
    }

    private static void ConfigureStringList(PropertyBuilder<List<string>> property)
    {
        property.HasConversion(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => JsonSerializer.Deserialize<List<string>>(value, JsonOptions) ?? new List<string>());
        property.Metadata.SetValueComparer(StringListComparer);
        property.HasColumnType("jsonb");
    }
}

using Domain.Models.Bonuses;

namespace Domain.Models.Soulstones.UpgradeDefinition;
public record UpgradeEffect(string Stat, double PerLevel, BonusMode BonusMode);

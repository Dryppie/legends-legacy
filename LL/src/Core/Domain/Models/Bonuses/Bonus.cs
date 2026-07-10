namespace Domain.Models.Bonuses;

public readonly record struct Bonus(
    BonusKind Kind,
    double Value // Most Soulstone constellation values are basis points: 100 = 1%.
);

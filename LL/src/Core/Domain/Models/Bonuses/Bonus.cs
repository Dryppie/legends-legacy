namespace Domain.Models.Bonuses;
public readonly record struct Bonus(
    BonusKind Kind,
    double Value   // e.g. 15  means “+15 %” for Additive or “×1.15” for Multiplicative
);
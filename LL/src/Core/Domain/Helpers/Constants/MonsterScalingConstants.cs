namespace Domain.Helpers.Constants;

public static class MonsterScalingConstants
{
    // HP curve: HP = base_hp * (1 + A * progression_tier)^B
    public const double HpA = 0.22;
    public const double HpB = 1.12;

    // MP curve: MP = base_mp * (1 + A * D)^B
    public const double MpA = 0.50;
    public const double MpB = 1.05;

    // Offense curve: AP/SP = base * (1 + C * progression_tier)^Exp
    public const double OffenseC = 0.18;
    public const double OffenseExp = 1.10;

    // Defenses: slower than HP so fights don't become immortal walls.
    public const double DefenseA = 0.16;
    public const double DefenseB = 1.08;

    // Resistances: similar to defense, slightly weaker if needed
    public const double ResistA = 0.16;
    public const double ResistB = 1.08;

    // Linear-ish scaling for some secondaries
    public const double AccuracyPerTier = 0.08;     // +8% per D (scaled on base)
    public const double PenPerTier = 0.10;     // +10% per D (scaled on base)
    public const double CritChancePerTier = 0.1;    // +0.1 percentage points per D
    public const double CritDamagePerTier = 0.25;   // +0.25 percentage points per D

    public const float CritChanceCap = 45f;         // 45% max on mobs
    public const float CritDamageCap = 200f;        // +200% critical damage max

    public const double AttackSpeedPerTier = 0.04;  // +4% per D

}

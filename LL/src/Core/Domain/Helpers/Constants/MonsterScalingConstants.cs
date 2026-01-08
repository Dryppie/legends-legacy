namespace Domain.Helpers.Constants;

public static class MonsterScalingConstants
{
    // HP curve: HP = base_hp * (1 + A * D)^B
    public const double HpA = 0.50;
    public const double HpB = 1.05;

    // MP curve: MP = base_mp * (1 + A * D)^B
    public const double MpA = 0.50;
    public const double MpB = 1.05;

    // Offense curve: AP/SP = base * (1 + C * D)^Exp
    public const double OffenseC = 0.50;
    public const double OffenseExp = 1.05;

    // Defenses: slower than HP so fights don't become immortal walls.
    public const double DefenseA = 0.50;
    public const double DefenseB = 1.05;

    // Resistances: similar to defense, slightly weaker if needed
    public const double ResistA = 0.50;
    public const double ResistB = 1.05;

    // Linear-ish scaling for some secondaries
    public const double AccuracyPerTier = 0.08;     // +8% per D (scaled on base)
    public const double PenPerTier = 0.10;     // +10% per D (scaled on base)
    public const double CritChancePerTier = 0.03;   // +3% per D, but capped
    public const double CritDamagePerTier = 0.04;   // +4% per D, capped

    public const float CritChanceCap = 0.45f;       // 45% max on mobs
    public const float CritDamageCap = 2.0f;        // 200% max

    public const double AttackSpeedPerTier = 0.04;  // +4% per D

    // Rubber banding vs PS band
    public const double OvergearedClampDeltaD = 2;       // cap effective D when PS >> band
    public const double UndergearedMinTtkHpMultiplier = 0.6; // reduce HP when player undergeared
}
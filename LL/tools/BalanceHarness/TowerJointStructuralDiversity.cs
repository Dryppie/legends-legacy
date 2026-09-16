namespace BalanceHarness;

/// <summary>Isolates between-candidate coverage while retaining the v1 structural eligibility rules.</summary>
public static class TowerJointStructuralDiversity
{
    public const string Version = "independent-joint-structural-diverse-v1";
    public const string Method = "joint-structural-diverse";
    public const string Operator = "fresh-joint-structural-diverse";

    // These are opt-in search restrictions, not gameplay legality or combat-strength guarantees.
    public static IReadOnlyList<string> RequiredKindsPerCharacter => TowerPartyCoverage.Kinds;
    public const int MaximumUsesPerRecipe = 1;
    public static int MinimumDistinctRecipes(int characters) => characters;
}

using System.Collections;
using System.Globalization;
using System.Reflection;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Essences;
using Domain.Models.Items;

namespace BalanceHarness;

public sealed record TowerRuntimeState(IReadOnlyDictionary<string, string?> Values,
    IReadOnlyDictionary<string, string?> Bookkeeping, string Hash);
public sealed record TowerRuntimeDifference(string Path, bool LeftPresent, string? Left,
    bool RightPresent, string? Right);

/// <summary>Diagnostic state comparison only. Does not mutate preparation, actors or gameplay.</summary>
public static class TowerRuntimeComparison
{
    public const string Version = "tower-runtime-fields-v2";

    // Captured snapshot rehydration creates these clock/GUID values. Keep the excluded
    // values in evidence; never exclude actor, equipped-Essence or equipment identities.
    private static bool IsBookkeeping(FieldInfo field) =>
        field.DeclaringType == typeof(PlayerEssence) && field.Name is "<AbsorbedAt>k__BackingField" or "<UpdatedAt>k__BackingField"
        || field.DeclaringType == typeof(InstanceAttributeModifier) && field.Name == "<Id>k__BackingField"
        || field.DeclaringType == typeof(ItemAttributeModifier) && field.Name == "<Id>k__BackingField"
        || field.DeclaringType == typeof(ItemInstance) && field.Name == "<AcquiredAtUtc>k__BackingField";

    public static TowerRuntimeState Capture(IReadOnlyList<CombatEntity> combatants, CancellationToken token = default)
    {
        var values = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        var bookkeeping = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        var ancestors = new HashSet<object>(ReferenceEqualityComparer.Instance); var nodes = 0;
        void Visit(object? value, string path, int depth, bool metadata = false)
        {
            token.ThrowIfCancellationRequested();
            if (++nodes > 500000 || depth > 100) throw new InvalidDataException("Runtime comparison graph exceeds its diagnostic bound.");
            var output = metadata ? bookkeeping : values;
            if (value is null) { output[path] = null; return; }
            var type = value.GetType(); output[path + "/@type"] = type.FullName;
            if (value is string || type.IsPrimitive || value is decimal || value is Guid || type.IsEnum)
            { output[path] = Convert.ToString(value, CultureInfo.InvariantCulture); return; }
            if (value is DateTimeOffset dto) { output[path] = dto.ToString("O", CultureInfo.InvariantCulture); return; }
            if (value is DateTime dt) { output[path] = dt.ToString("O", CultureInfo.InvariantCulture); return; }
            if (value is Delegate) throw new InvalidDataException("Runtime comparison does not support dynamic delegates.");
            if (!type.IsValueType && !ancestors.Add(value)) { output[path + "/@cycle"] = type.FullName; return; }
            try
            {
                if (value is IEnumerable sequence)
                {
                    var index = 0;
                    foreach (var item in sequence) Visit(item, path + "/[" + index++ + "]", depth + 1, metadata);
                    output[path + "/@count"] = index.ToString(CultureInfo.InvariantCulture); return;
                }
                for (var current = type; current is not null; current = current.BaseType)
                    foreach (var field in current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .OrderBy(f => f.Name, StringComparer.Ordinal))
                        Visit(field.GetValue(value), path + "/" + current.FullName + "::" + field.Name, depth + 1, metadata || IsBookkeeping(field));
            }
            finally { if (!type.IsValueType) ancestors.Remove(value); }
        }
        Visit(combatants, "$", 0);
        return new(values, bookkeeping, HarnessJson.Hash(values));
    }

    // All differences, including absent versus explicit-null fields, are retained.
    public static IReadOnlyList<TowerRuntimeDifference> Differences(IReadOnlyDictionary<string, string?> left,
        IReadOnlyDictionary<string, string?> right) => left.Keys.Union(right.Keys, StringComparer.Ordinal)
        .Order(StringComparer.Ordinal).Select(path => {
            var lp = left.TryGetValue(path, out var l); var rp = right.TryGetValue(path, out var r);
            return new TowerRuntimeDifference(path, lp, l, rp, r);
        }).Where(d => d.LeftPresent != d.RightPresent || d.Left != d.Right).ToArray();
}

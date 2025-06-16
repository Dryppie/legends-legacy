using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Application.UsesCases.Chats.Commands.SendMessage;
public static class SendMessageValidator
{
    // ─────────────── tunables ───────────────
    public const int MaxVisibleLength = 4000;
    public const int MaxCombiningChars = 20;
    public const double MaxCombiningRatio = 0.20;   // 20 %

    // ─────────────── regex pre-compiles (thread-safe) ───────────────
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex ControlCharsPattern = new(@"[\u0000-\u001F\u007F-\u009F]", RegexOptions.Compiled);
    private static readonly Regex ForbiddenLiteral = new(@"\uFDFD", RegexOptions.Compiled); // ﷽ U+FDFD
    private static readonly Regex RepeatedCharPattern = new(@"([\p{L}\p{N}\p{S}\p{P}])\1{19,}", RegexOptions.Compiled);
    private static readonly Regex CombiningMarksPattern = new(@"\p{M}", RegexOptions.Compiled);

    /// <summary>
    /// Returns <c>true</c> if the message should be accepted, <c>false</c> if it must be rejected.
    /// </summary>
    public static bool IsValid(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // 1. Collapse whitespace so we can measure the “visual” length
        var visible = WhitespacePattern.Replace(text, string.Empty);

        if (visible.Length == 0 || visible.Length > MaxVisibleLength)
            return false;

        // 2. ASCII / Latin-1 control characters
        if (ControlCharsPattern.IsMatch(text))
            return false;

        // 3. Explicitly banned code points (﷽)
        if (ForbiddenLiteral.IsMatch(text))
            return false;

        // 4. 20+ identical glyphs in a row
        if (RepeatedCharPattern.IsMatch(text))
            return false;

        // 5. Excessive combining-mark density  (Z̸̠͑͟a̸͉ͩͨl̑͜g̍͘o͍̓…)
        var combiningCount = CombiningMarksPattern.Matches(text).Count;
        var density = combiningCount / (double)visible.Length;

        if (combiningCount > MaxCombiningChars && density > MaxCombiningRatio)
            return false;

        return true;    // ✅ good to save and broadcast
    }
}
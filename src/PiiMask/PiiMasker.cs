using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace PiiMask;

/// <summary>An immutable, thread-safe masker with snapshotted configuration.</summary>
public sealed class PiiMasker
{
    private readonly char mask;
    private readonly int emailPrefix, phoneSuffix, cardSuffix, partialSuffix, tokenPrefix, tokenMaskLength;
    private readonly PiiTypes detectTypes;
    private readonly bool barePhones;
    private readonly Regex[] customPatterns;

    /// <summary>Constructs a masker and validates its configuration.</summary>
    public PiiMasker(PiiMaskOptions? options = null)
    {
        options ??= new();
        if (char.IsLetterOrDigit(options.MaskCharacter) || char.IsWhiteSpace(options.MaskCharacter) ||
            char.IsControl(options.MaskCharacter) || char.IsSurrogate(options.MaskCharacter) ||
            options.MaskCharacter is '"' or '\'' or '\\' || !char.IsPunctuation(options.MaskCharacter) && !char.IsSymbol(options.MaskCharacter))
            throw new ArgumentOutOfRangeException(nameof(options), "Use a visible punctuation mask character, such as * or #.");
        ValidateCount(options.EmailVisiblePrefix, 64, nameof(options.EmailVisiblePrefix));
        ValidateCount(options.PhoneVisibleSuffix, 4, nameof(options.PhoneVisibleSuffix));
        ValidateCount(options.CardVisibleSuffix, 4, nameof(options.CardVisibleSuffix));
        ValidateCount(options.PartialVisibleSuffix, 256, nameof(options.PartialVisibleSuffix));
        ValidateCount(options.TokenVisiblePrefix, 16, nameof(options.TokenVisiblePrefix));
        if (options.TokenMaskLength is < 1 or > 256)
            throw new ArgumentOutOfRangeException(nameof(options), "Token mask length must be between 1 and 256.");
        if ((options.DetectTypes & ~PiiTypes.All) != 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Unknown detector flags.");
        if (options.CustomRegexTimeout < TimeSpan.FromMilliseconds(1) || options.CustomRegexTimeout > TimeSpan.FromSeconds(1))
            throw new ArgumentOutOfRangeException(nameof(options), "Custom regex timeout must be between 1 ms and 1 second.");
        ArgumentNullException.ThrowIfNull(options.CustomPatterns);

        mask = options.MaskCharacter;
        emailPrefix = options.EmailVisiblePrefix;
        phoneSuffix = options.PhoneVisibleSuffix;
        cardSuffix = options.CardVisibleSuffix;
        partialSuffix = options.PartialVisibleSuffix;
        tokenPrefix = options.TokenVisiblePrefix;
        tokenMaskLength = options.TokenMaskLength;
        detectTypes = options.DetectTypes;
        barePhones = options.DetectBarePhoneNumbers;
        customPatterns = options.CustomPatterns.Select(pattern =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
            return new Regex(pattern, RegexOptions.CultureInvariant, options.CustomRegexTimeout);
        }).ToArray();
    }

    /// <summary>Masks a known sensitive value. Invalid typed values are fully masked.</summary>
    [return: NotNullIfNotNull(nameof(value))]
    public string? Mask(string? value, SensitiveKind kind = SensitiveKind.Full)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (string.IsNullOrEmpty(value))
            return value;

        try
        {
            return MaskValue(value, kind);
        }
        catch (RegexMatchTimeoutException)
        {
            return Full(value);
        }
    }

    /// <summary>
    /// Masks recognized patterns in text. All detectors read the original text; overlapping
    /// matches are resolved before replacement. Any regex timeout masks the entire input.
    /// </summary>
    [return: NotNullIfNotNull(nameof(text))]
    public string? MaskText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        try
        {
            var spans = new List<Redaction>();
            if (Enabled(PiiTypes.Email))
                Collect(Patterns.Email(), SensitiveKind.Email);
            if (Enabled(PiiTypes.Token))
            {
                Collect(Patterns.Credential(), SensitiveKind.Token, "value");
                Collect(Patterns.Bearer(), SensitiveKind.Token, "value");
                Collect(Patterns.Token(), SensitiveKind.Token, "value");
            }
            if (Enabled(PiiTypes.CardNumber))
                Collect(Patterns.Card(), SensitiveKind.CardNumber, accept: m => IsLuhnValid(m.Value));
            if (Enabled(PiiTypes.Phone))
                Collect(Patterns.Phone(), SensitiveKind.Phone, accept: m => IsPhone(m.Value));
            foreach (var regex in customPatterns)
                Collect(regex, SensitiveKind.Full);
            if (spans.Count == 0)
                return text;

            spans.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : b.Length.CompareTo(a.Length));
            var merged = new List<Redaction>();
            foreach (var span in spans)
            {
                if (merged.Count == 0 || span.Start >= merged[^1].End)
                    merged.Add(span);
                else
                {
                    // Full masking the union is deliberate: an email inside a password,
                    // or a custom rule inside a card, must not expose the outer secret.
                    var previous = merged[^1];
                    merged[^1] = new(previous.Start, Math.Max(previous.End, span.End) - previous.Start,
                        previous.Start == span.Start && previous.Length == span.Length &&
                        previous.Kind == span.Kind ? previous.Kind : SensitiveKind.Full);
                }
            }
            var result = new StringBuilder(text.Length);
            var cursor = 0;
            foreach (var span in merged)
            {
                result.Append(text, cursor, span.Start - cursor);
                // Let timeouts reach this method's catch so the whole message is
                // hidden, including when a typed formatter's validation times out.
                result.Append(MaskValue(text.Substring(span.Start, span.Length), span.Kind));
                cursor = span.End;
            }
            result.Append(text, cursor, text.Length - cursor);
            return result.ToString();

            void Collect(Regex regex, SensitiveKind kind, string? groupName = null, Func<Match, bool>? accept = null)
            {
                foreach (Match match in regex.Matches(text))
                {
                    if (accept is not null && !accept(match))
                        continue;
                    Group group = groupName is null ? match : match.Groups[groupName];
                    if (group.Success && group.Length > 0)
                    {
                        var length = group.Length;
                        if (kind == SensitiveKind.Token)
                            while (group.Index + length < text.Length && text[group.Index + length] == mask)
                                length++;
                        spans.Add(new(group.Index, length, kind));
                    }
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return Full(text);
        }
    }

    private string Full(string value) => new(mask, value.Length);
    private bool Enabled(PiiTypes type) => (detectTypes & type) != 0;

    private string MaskValue(string value, SensitiveKind kind)
    {
        if (value.All(c => c == mask))
            return value;
        return kind switch
        {
            SensitiveKind.Full => Full(value),
            SensitiveKind.Partial => value.Length <= partialSuffix
                ? Full(value) : new string(mask, value.Length - partialSuffix) + value[^partialSuffix..],
            SensitiveKind.Email => MaskEmail(value),
            SensitiveKind.Phone => MaskDigits(value, phoneSuffix, 8, 15),
            SensitiveKind.CardNumber => MaskDigits(value, cardSuffix, 13, 19),
            SensitiveKind.Token => MaskToken(value),
            SensitiveKind.Auto => MaskAuto(value),
            _ => Full(value)
        };
    }

    private string MaskAuto(string value)
    {
        var result = MaskText(value);
        return result == value ? Full(value) : result;
    }

    private string MaskEmail(string value)
    {
        var at = value.LastIndexOf('@');
        if (at <= 0)
            return Full(value);
        var local = value[..at];
        var match = Patterns.Email().Match(value);
        if (!(match.Success && match.Index == 0 && match.Length == value.Length))
        {
            if (!local.Contains(mask))
                return Full(value);
            // Validate an already-redacted local part without rejecting legitimate
            // visible punctuation such as the dot in "a.*@example.com".
            // Keep the original domain so malformed suffixes still fail closed.
            var candidate = local.Replace(mask, 'a') + value[at..];
            var maskedMatch = Patterns.Email().Match(candidate);
            if (!(maskedMatch.Success && maskedMatch.Index == 0 && maskedMatch.Length == candidate.Length))
                return Full(value);
        }
        var visible = local.Length <= emailPrefix ? 0 : emailPrefix;
        return local[..visible] + new string(mask, local.Length - visible) + value[at..];
    }

    private string MaskDigits(string value, int visible, int min, int max)
    {
        var count = value.Count(c => char.IsAsciiDigit(c) || c == mask);
        if (count < min || count > max || value.Any(c => !char.IsAsciiDigit(c) && c != mask && c is not (' ' or '-' or '(' or ')' or '+')))
            return Full(value);
        var remaining = count;
        return new string(value.Select(c => char.IsAsciiDigit(c) || c == mask ? (--remaining >= visible ? mask : c) : c).ToArray());
    }

    private string MaskToken(string value)
    {
        if (value.Length == tokenPrefix + tokenMaskLength && value.AsSpan(tokenPrefix).IndexOfAnyExcept(mask) < 0)
            return value;
        var visible = value.Length <= tokenPrefix ? 0 : tokenPrefix;
        return value[..visible] + new string(mask, tokenMaskLength);
    }

    private bool IsPhone(string value)
    {
        var digits = value.Count(char.IsAsciiDigit);
        if (digits > 15 || Patterns.Date().IsMatch(value))
            return false;
        if (value.StartsWith('+'))
            return digits >= 8;
        if (digits < 10)
            return false;
        // A failed card candidate must not get reclassified as a phone by default.
        if (digits >= 13 && !value.Contains('('))
            return barePhones && value.All(char.IsAsciiDigit);
        return barePhones || value.Any(c => c is ' ' or '-' or '(');
    }

    private static bool IsLuhnValid(string value)
    {
        var digits = value.Where(char.IsAsciiDigit).Select(c => c - '0').ToArray();
        if (digits.Length is < 13 or > 19 || digits.All(d => d == digits[0]))
            return false;
        var sum = 0;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i];
            if ((digits.Length - 1 - i) % 2 == 1)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
        }
        return sum % 10 == 0;
    }

    private static void ValidateCount(int value, int maximum, string name)
    {
        if (value < 0 || value > maximum)
            throw new ArgumentOutOfRangeException(name, $"Visible count must be between 0 and {maximum}.");
    }

    private readonly record struct Redaction(int Start, int Length, SensitiveKind Kind)
    {
        internal int End => Start + Length;
    }
}

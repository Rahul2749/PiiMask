namespace PiiMask;

/// <summary>Configuration snapshotted by <see cref="PiiMasker"/> during construction.</summary>
public sealed class PiiMaskOptions
{
    /// <summary>Mask character. Must be visible punctuation other than a quote or backslash.</summary>
    public char MaskCharacter { get; init; } = '*';

    /// <summary>Maximum local-part prefix to retain. Short local parts are fully masked.</summary>
    public int EmailVisiblePrefix { get; init; } = 2;

    /// <summary>Trailing phone digits to retain.</summary>
    public int PhoneVisibleSuffix { get; init; } = 4;

    /// <summary>Trailing card digits to retain.</summary>
    public int CardVisibleSuffix { get; init; } = 4;

    /// <summary>Trailing characters to retain for partial masking.</summary>
    public int PartialVisibleSuffix { get; init; } = 4;

    /// <summary>Token prefix length to retain.</summary>
    public int TokenVisiblePrefix { get; init; } = 4;

    /// <summary>Fixed token mask length, independent of the original token length.</summary>
    public int TokenMaskLength { get; init; } = 8;

    /// <summary>Built-in detectors to run. Explicit masking ignores this filter.</summary>
    public PiiTypes DetectTypes { get; init; } = PiiTypes.All;

    /// <summary>Whether 10–15 digit bare phone candidates may be detected.</summary>
    public bool DetectBarePhoneNumbers { get; init; }

    /// <summary>Timeout for each custom regex operation. Built-in regexes use 100 ms.</summary>
    public TimeSpan CustomRegexTimeout { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Additional regex patterns. Each complete nonempty match is fully masked.</summary>
    public IReadOnlyList<string> CustomPatterns { get; init; } = Array.Empty<string>();
}

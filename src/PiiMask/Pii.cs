using System.Diagnostics.CodeAnalysis;

namespace PiiMask;

/// <summary>Convenient masking entry points using safe default settings.</summary>
public static class Pii
{
    private static readonly PiiMasker Default = new();

    /// <summary>Masks an explicitly sensitive value, fully by default.</summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Mask(string? value, SensitiveKind kind = SensitiveKind.Full) => Default.Mask(value, kind);

    /// <summary>Finds and masks recognized PII in free text; unrecognized text is unchanged.</summary>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? MaskText(string? text) => Default.MaskText(text);
}

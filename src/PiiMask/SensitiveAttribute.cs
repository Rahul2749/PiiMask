namespace PiiMask;

/// <summary>Marks a property or included field for masking during JSON serialization.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public sealed class SensitiveAttribute : Attribute
{
    /// <summary>Creates an attribute; an unspecified kind fully masks strings.</summary>
    public SensitiveAttribute(SensitiveKind kind = SensitiveKind.Full) => Kind = kind;

    /// <summary>Gets the requested masking strategy.</summary>
    public SensitiveKind Kind { get; }
}

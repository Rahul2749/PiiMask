namespace PiiMask;

/// <summary>Specifies how an explicitly sensitive value is masked.</summary>
public enum SensitiveKind
{
    /// <summary>Masks every character.</summary>
    Full,
    /// <summary>Preserves only the configured trailing characters.</summary>
    Partial,
    /// <summary>Preserves a short local-part prefix and the email domain.</summary>
    Email,
    /// <summary>Preserves formatting and a short suffix of phone digits.</summary>
    Phone,
    /// <summary>Preserves formatting and a short suffix of card digits.</summary>
    CardNumber,
    /// <summary>Preserves a short prefix followed by a fixed-size mask.</summary>
    Token,
    /// <summary>Scans for known patterns; fully masks values with no detected match.</summary>
    Auto
}

/// <summary>Built-in detectors enabled for free-text scanning.</summary>
[Flags]
public enum PiiTypes
{
    /// <summary>No built-in detectors.</summary>
    None = 0,
    /// <summary>Email addresses.</summary>
    Email = 1,
    /// <summary>Credentials and recognizable tokens.</summary>
    Token = 2,
    /// <summary>Luhn-valid card candidates.</summary>
    CardNumber = 4,
    /// <summary>Conservative phone candidates.</summary>
    Phone = 8,
    /// <summary>All built-in detectors.</summary>
    All = Email | Token | CardNumber | Phone
}

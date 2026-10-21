# PiiMask 0.1.0

First stable release for .NET 10, .NET 9, and .NET 8.

## Changes since 0.1.0-preview.1

- Fixed repeated email masking when the visible local-part prefix includes punctuation, such as `a.*@example.com` or `o'******@example.com`. Reapplying the same masker preserves the masked address and domain.
- Kept malformed partially masked emails fully hidden; the original domain and trailing content must still validate.
- Ensured typed-formatter regex timeouts propagate to free-text scanning's whole-message redaction fallback.
- Added 25 release regression cases for email punctuation, configurable masking, zero visible counts, quoted/short credentials, custom rules, timeout fallback, required JSON members, ignored/null values, and sensitive objects/collections.
- Updated NuGet installation and usage documentation for the stable version; prerelease selection is no longer required.

## Included

- `Pii.Mask`, `Pii.MaskText`, and an immutable configurable `PiiMasker`.
- Full, partial, email, phone, card, token, and auto masking.
- Free-text detection with Luhn-validated cards and conservative phone rules.
- Common credential-pair, Bearer, JWT-like, AWS, GitHub, Stripe, and Slack recognition.
- Custom regex rules with finite timeouts; timeout fallback masks the entire input.
- Overlap handling based on original-text spans.
- `[Sensitive]` and `AddPiiMasking()` for reflection-based System.Text.Json.
- No runtime NuGet dependencies; XML API docs, README, runnable usage guide, MIT license, and symbols.

## Scope and limitations

Detection is heuristic. Email domains and configured prefixes/suffixes remain visible. Choose full masking for values that must be completely hidden. JSON integration does not support source-generated contexts, trimming, or NativeAOT. Provider-specific and regional formats are incomplete. Dedicated logging integrations are not included. No measured throughput or compliance guarantee is claimed. The API may evolve during the 0.x release series.

Source: [Rahul2749/PiiMask](https://github.com/Rahul2749/PiiMask). Portable symbols include SourceLink information for the exact committed build revision.

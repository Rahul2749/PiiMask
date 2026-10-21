# PiiMask

Mask sensitive values, log text, and annotated JSON members on **.NET 10, .NET 9, and .NET 8**, with **zero runtime package dependencies**.

```text
Before: Email rahul@gmail.com; card=4111 1111 1111 1111
After:  Email ra***@gmail.com; card=**** **** **** 1111
```

## Install

```shell
dotnet add package PiiMask --version 0.1.0
```

In Visual Studio, open **Manage NuGet Packages**, search for **PiiMask**, and select `0.1.0`. Use nuget.org as the package source.

Version `0.1.0` is the first stable release. Validate detection with your application's own log formats. The public API may evolve during the 0.x release series.

## Quick start: mask known values

```csharp
using PiiMask;

Console.WriteLine(Pii.Mask("hunter2"));
Console.WriteLine(Pii.Mask("rahul@gmail.com", SensitiveKind.Email));
Console.WriteLine(Pii.Mask("+1 415-555-2671", SensitiveKind.Phone));
Console.WriteLine(Pii.Mask("4111111111111111", SensitiveKind.CardNumber));
Console.WriteLine(Pii.Mask("abcdefghijklmnop", SensitiveKind.Token));
Console.WriteLine(Pii.Mask("customer-1234", SensitiveKind.Partial));
```

Output:

```text
*******
ra***@gmail.com
+* ***-***-2671
************1111
abcd********
*********1234
```

`Pii.Mask(value)` fully masks a value you know is sensitive. Select a `SensitiveKind` to preserve part of its format. Null and empty values remain null and empty. Short values are fully masked when retaining the requested prefix or suffix would expose the entire value. Invalid email/phone/card-shaped values passed to an explicit typed masker are fully masked.

## Scan mixed log text

```csharp
using PiiMask;

Console.WriteLine(Pii.MaskText("Email rahul@gmail.com; card=4111 1111 1111 1111"));
Console.WriteLine(Pii.MaskText("Authorization: Bearer abcdefghijklmnop"));
Console.WriteLine(Pii.MaskText("password=hunter2"));
```

Output:

```text
Email ra***@gmail.com; card=**** **** **** 1111
Authorization: Bearer abcd********
password=hunt********
```

Pass the masked result to your logger. For structured logging, mask each known sensitive argument before logging it. The core package does not automatically intercept `ILogger`, Serilog, or HTTP request/response logging.

`MaskText` preserves content it cannot recognize. For a password or an entire value that must be hidden, use `Pii.Mask(value)` with its default `Full` behavior. Credential detection in mixed text uses token-style masking and can retain a prefix, as shown above.

`SensitiveKind.Auto` scans a known sensitive value and fully masks it if scanning makes no change. Unrecognized content beside a detected match can remain visible; use `Full` when the whole value must be hidden.

## Mask System.Text.Json output

```csharp
using PiiMask;
using System.Text.Json;

var customer = new Customer("rahul@gmail.com", "hunter2", 42);
var logOptions = new JsonSerializerOptions().AddPiiMasking();
Console.WriteLine(JsonSerializer.Serialize(customer, logOptions));

public sealed record Customer(
    [property: Sensitive(SensitiveKind.Email)] string Email,
    [property: Sensitive] string Password,
    [property: Sensitive] int InternalId);
```

Output:

```text
{"Email":"ra***@gmail.com","Password":"*******"}
```

- `[Sensitive]` fully masks strings by default; specify a kind for email, phone, card, token, partial, or auto masking.
- Annotated non-string members are omitted, including numbers, dates, objects, and collections. Their serialization getters are not evaluated.
- Unannotated nested objects and collections are traversed normally, so annotations on their members apply.
- Included fields are supported when `IncludeFields` is enabled. Positional records need the `property:` attribute target shown above.
- Configure `AddPiiMasking()` before first using the options, then reuse the options. Source objects and deserialized input are unchanged.
- Use a dedicated options instance for logs if ordinary API responses should retain original values.

This integration supports reflection-based `DefaultJsonTypeInfoResolver` and retains its existing modifiers. Source-generated contexts, custom resolver chains, trimming, and NativeAOT are not supported by the JSON integration. Unsupported resolvers are rejected on registration. Custom converters that write objects themselves must handle masking themselves. Do not replace the resolver after registration.

## Configure a reusable masker

```csharp
using PiiMask;

var masker = new PiiMasker(new PiiMaskOptions
{
    MaskCharacter = '#',
    EmailVisiblePrefix = 1,
    CardVisibleSuffix = 2,
    DetectTypes = PiiTypes.All,
    DetectBarePhoneNumbers = false,
    CustomPatterns = new[] { @"EMP-\d{6}" },
    CustomRegexTimeout = TimeSpan.FromMilliseconds(100)
});

Console.WriteLine(masker.Mask("rahul@gmail.com", SensitiveKind.Email));
Console.WriteLine(masker.MaskText("Employee EMP-123456"));
```

Output:

```text
r####@gmail.com
Employee ##########
```

A masker snapshots its options and pattern list at construction and can be shared between threads. Pass your masker to `new JsonSerializerOptions().AddPiiMasking(masker)` to use the same rules for annotated JSON members.

| Option | Default | Allowed values / behavior |
| --- | --- | --- |
| `MaskCharacter` | `'*'` | Visible punctuation such as `*` or `#`; letters, digits, whitespace, quotes, and backslash rejected |
| `EmailVisiblePrefix` | `2` | 0–64 local-part characters; domain preserved |
| `PhoneVisibleSuffix` | `4` | 0–4 trailing digits |
| `CardVisibleSuffix` | `4` | 0–4 trailing digits |
| `PartialVisibleSuffix` | `4` | 0–256 trailing characters |
| `TokenVisiblePrefix` | `4` | 0–16 prefix characters |
| `TokenMaskLength` | `8` | 1–256 mask characters, independent of token length |
| `DetectTypes` | `PiiTypes.All` | Combine `Email`, `Token`, `CardNumber`, and `Phone` with bitwise OR; `None` disables built-ins |
| `DetectBarePhoneNumbers` | `false` | Opt in to bare 10–15 digit phone candidates; increases false positives for IDs/timestamps |
| `CustomPatterns` | Empty | Every complete nonempty regex match is fully masked, even if built-ins are disabled |
| `CustomRegexTimeout` | 100 ms | Finite timeout of 1–1,000 ms per custom regex operation |

Invalid options or patterns throw at construction. To change settings, construct another masker.

## What free-text detection recognizes

| Kind | Recognition rules |
| --- | --- |
| Email | Common ASCII addresses with a dotted domain |
| Card | 13–19 digits with optional spaces/hyphens; Luhn checksum required; repeated identical digits excluded |
| Phone | International `+` numbers with 8–15 digits; formatted national numbers generally 10–12 digits, or up to 15 when parentheses are present |
| Token | JWT-like values, Bearer credentials, common AWS access-key IDs, GitHub, Stripe secret/restricted, and Slack token formats |
| Credential pair | `password`, `passwd`, `pwd`, `secret`, `client_secret`, `api_key`, access/refresh tokens; quoted or unquoted values |
| Custom rule | Entire nonempty regex matches |

Unprefixed 13–15 digit formatted numbers without parentheses are ambiguous with cards and are not treated as phones by default. Explicit `CardNumber` masking does not require a valid checksum: a value already marked sensitive still gets masked.

Built-in regexes are source generated with a 100 ms timeout. Any regex timeout fully masks the input. Detectors examine original text; intersecting detections of different kinds are fully masked across their union. An ambiguous unquoted `password=email` assignment may therefore be fully masked, including its key.

## Limits and troubleshooting

- Detection is heuristic and cannot find every secret, personal name, address, provider format, or encoded/escaped value. It does not establish compliance.
- Email masking reveals the domain; phone/card masking reveals suffix digits. Full/partial masking reveals input length. Token masking reveals a prefix for sufficiently long values. Use `Full` where those details must be hidden.
- Free-text scanning is not a JSON parser. Prefer attributes or explicitly mask values before serialization.
- Unicode/quoted email local parts, internationalized domains, and many regional phone formats are outside the built-in detection scope.
- Regex timeouts are per operation, not a total execution budget. Bound message sizes and custom-rule counts in your application.
- Explicit typed masking and common scan outputs support repeated use; `Auto` may mask more on another pass.
- Masking does not erase original strings from memory or remove earlier logs. Mask before sending data to the logger.
- If JSON configuration throws after serialization has started, configure a new options instance before use.
- If NuGet cannot find the release, specify `--version 0.1.0` and use nuget.org as the package source.

## Included documentation and release scope

This NuGet package includes this README, XML API documentation, the MIT license, `docs/usage-guide.md` with a complete runnable example, and `docs/release-notes.md`.

The release includes direct masking, free-text detection, custom rules, and System.Text.Json integration. Dedicated logging integrations, ASP.NET Core middleware, and regional rule packs are planned separately. Benchmarks are not yet published; no comparative performance or accuracy claim is made.

NuGet's framework badge shows the lowest supported .NET version, so it displays .NET 8 for this package. The Frameworks tab lists the included .NET 8, 9, and 10 assets.

## Documentation and support

- [Complete usage guide with a runnable example](https://github.com/Rahul2749/PiiMask/blob/v0.1.0/docs/usage-guide.md)
- [Release notes](https://github.com/Rahul2749/PiiMask/blob/v0.1.0/docs/release-notes.md)
- [Source code and contributions](https://github.com/Rahul2749/PiiMask)
- [Report an issue](https://github.com/Rahul2749/PiiMask/issues)

Portable debugging symbols include SourceLink information for the exact source revision used to build the package.

# How to use PiiMask

This guide covers `PiiMask 0.1.0` on .NET 8, 9, and 10. The core package has no runtime NuGet dependencies.

## 1. Install the package

For a new application, run these commands in PowerShell. The example uses .NET 8; replace `net8.0` with `net9.0` or `net10.0` as needed. Use a new folder name if `PiiMaskDemo` already exists.

```powershell
dotnet new console --name PiiMaskDemo --framework net8.0
Set-Location PiiMaskDemo
dotnet add package PiiMask --version 0.1.0
```

For an existing application, run only the `dotnet add package` command from the folder containing its `.csproj`. Use nuget.org as the source. Version `0.1.0` is stable and does not require enabling prerelease packages in Visual Studio. Building this repository requires a .NET 10 SDK; consuming the package requires an SDK appropriate for your application.

Before publication, or when testing a local build, append `--source C:\Github\PiiMask\artifacts`. On another computer, copy the `.nupkg` into a local feed folder and use that folder as `--source`.

## 2. Choose the right operation

| Your input | Use | Behavior |
| --- | --- | --- |
| A value you know is sensitive | `Pii.Mask(value)` | Masks every character by default |
| A known email, phone, card, token, or identifier | `Pii.Mask(value, SensitiveKind.Email)` or another kind | Uses the selected format |
| A log message with mixed content | `Pii.MaskText(message)` | Detects supported patterns and preserves other text |
| A sensitive value of uncertain type | `Pii.Mask(value, SensitiveKind.Auto)` | Scans first; fully masks if scanning makes no change |
| An object being serialized for logs | `[Sensitive]` and `AddPiiMasking()` | Masks annotated string members and omits annotated non-string members |
| Application-specific rules or formatting | One reusable `PiiMasker` | Applies your options and custom regex rules |

`MaskText` preserves secrets it cannot recognize. `Auto` can leave unrecognized content beside a detected match. For a password or an entire value that must be hidden, use `Full`.

## 3. Run this complete example

Replace your application's entire `Program.cs` with the following code, then run `dotnet run`. Each `Check` compares the actual result to the documented expected value and throws if it differs. All identifiers and credentials below are synthetic examples.

```csharp
using System.Runtime.InteropServices;
using System.Text.Json;
using PiiMask;

var passed = 0;

// A. Known sensitive values: the second argument is the expected masked output.
Check("Full", "*******", Pii.Mask("hunter2"));
Check("Email", "ra***@gmail.com", Pii.Mask("rahul@gmail.com", SensitiveKind.Email));
Check("Short email", "*@gmail.com", Pii.Mask("a@gmail.com", SensitiveKind.Email));
Check("Phone", "+* ***-***-2671", Pii.Mask("+1 415-555-2671", SensitiveKind.Phone));
Check("Card", "**** **** **** 1111", Pii.Mask("4111 1111 1111 1111", SensitiveKind.CardNumber));
Check("Token", "abcd********", Pii.Mask("abcdefghijklmnop", SensitiveKind.Token));
Check("Partial", "*********1234", Pii.Mask("customer-1234", SensitiveKind.Partial));
Check("Short partial", "***", Pii.Mask("abc", SensitiveKind.Partial));
Check("Auto fallback", "*******", Pii.Mask("unknown", SensitiveKind.Auto));
Check("Malformed email", "*********", Pii.Mask("not-email", SensitiveKind.Email));
Check("Null", (string?)null, Pii.Mask(null));
Check("Empty", "", Pii.MaskText(""));

// B. Log text: mask before passing the result to a logger or console.
const string message = "Email rahul@gmail.com; card=4111 1111 1111 1111";
var safeMessage = Pii.MaskText(message);
Check("Mixed log", "Email ra***@gmail.com; card=**** **** **** 1111", safeMessage);
Console.WriteLine(safeMessage);
Check("Bearer", "Authorization: Bearer abcd********",
    Pii.MaskText("Authorization: Bearer abcdefghijklmnop"));
Check("Credential pair", "password=hunt********", Pii.MaskText("password=hunter2"));
Check("Invalid card unchanged", "4111 1111 1111 1112", Pii.MaskText("4111 1111 1111 1112"));
Check("Date unchanged", "2026-10-20", Pii.MaskText("2026-10-20"));
Check("Bare phone unchanged by default", "4155552671", Pii.MaskText("4155552671"));
Check("Repeated scan", safeMessage, Pii.MaskText(safeMessage));

// C. Configure a reusable masker. Custom regexes redact the complete match.
var masker = new PiiMasker(new PiiMaskOptions
{
    MaskCharacter = '#',
    EmailVisiblePrefix = 1,
    CardVisibleSuffix = 2,
    DetectBarePhoneNumbers = true,
    CustomPatterns = new[] { @"EMP-\d{6}" },
    CustomRegexTimeout = TimeSpan.FromMilliseconds(100)
});
Check("Custom email", "r####@gmail.com", masker.Mask("rahul@gmail.com", SensitiveKind.Email));
Check("Custom card", "#### #### #### ##11", masker.Mask("4111 1111 1111 1111", SensitiveKind.CardNumber));
Check("Custom rule", "Employee ##########", masker.MaskText("Employee EMP-123456"));
Check("Bare phone opt-in", "######2671", masker.MaskText("4155552671"));

// Detector flags affect scanning; explicit masking still uses the requested kind.
var emailOnly = new PiiMasker(new PiiMaskOptions { DetectTypes = PiiTypes.Email });
Check("Email-only scan", "ra***@gmail.com; +1 415-555-2671",
    emailOnly.MaskText("rahul@gmail.com; +1 415-555-2671"));

// D. JSON: use a dedicated options instance when serializing for logs.
var customer = new Customer("rahul@gmail.com", "hunter2", 42);
var logOptions = new JsonSerializerOptions().AddPiiMasking();
var safeJson = JsonSerializer.Serialize(customer, logOptions);
Check("JSON", "{\"Email\":\"ra***@gmail.com\",\"Password\":\"*******\"}", safeJson);
Console.WriteLine(safeJson);
Check("Original email preserved", "rahul@gmail.com", customer.Email);
Check("Original password preserved", "hunter2", customer.Password);
Check("JSON options reuse", safeJson, JsonSerializer.Serialize(customer, logOptions));

// Unannotated collections are traversed; each Customer's annotations still apply.
var nested = JsonSerializer.Serialize(new { Customers = new[] { customer } }, logOptions);
Check("Nested JSON", "{\"Customers\":[" + safeJson + "]}", nested);

// Deserialization retains the original supplied values.
var restored = JsonSerializer.Deserialize<Customer>(
    "{\"Email\":\"rahul@gmail.com\",\"Password\":\"hunter2\",\"InternalId\":42}", logOptions)!;
Check("Deserialized email", "rahul@gmail.com", restored.Email);
Check("Deserialized number", 42, restored.InternalId);

var customLogOptions = new JsonSerializerOptions().AddPiiMasking(masker);
Check("Custom JSON", "{\"Email\":\"r####@gmail.com\",\"Password\":\"#######\"}",
    JsonSerializer.Serialize(customer, customLogOptions));

Console.WriteLine($"All {passed} usage checks passed on {RuntimeInformation.FrameworkDescription}.");

void Check<T>(string label, T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Usage example failed: {label}");
    passed++;
}

public sealed record Customer(
    [property: Sensitive(SensitiveKind.Email)] string Email,
    [property: Sensitive] string Password,
    [property: Sensitive] int InternalId);
```

The output includes the masked message, masked JSON, and a final success count. The `Check` helper is there to demonstrate and verify behavior; it is not required by PiiMask.

## 4. Understand the options and defaults

| Option | Default | Meaning |
| --- | --- | --- |
| `MaskCharacter` | `'*'` | Use visible punctuation such as `*` or `#`; letters, digits, whitespace, quotes, and backslash are rejected |
| `EmailVisiblePrefix` | `2` | Local-part prefix retained; 0–64 allowed; short local parts are fully masked |
| `PhoneVisibleSuffix` | `4` | Phone digits retained at the end; 0–4 allowed |
| `CardVisibleSuffix` | `4` | Card digits retained at the end; 0–4 allowed |
| `PartialVisibleSuffix` | `4` | Trailing characters retained; 0–256 allowed; short values are fully masked |
| `TokenVisiblePrefix` | `4` | Token prefix retained; 0–16 allowed; short tokens retain no prefix |
| `TokenMaskLength` | `8` | Fixed token mask length; 1–256 allowed |
| `DetectTypes` | `PiiTypes.All` | Combine `Email`, `Phone`, `CardNumber`, and `Token` with bitwise OR; `None` disables built-ins |
| `DetectBarePhoneNumbers` | `false` | Opt in to 10–15 digit bare phone candidates; may also match IDs or timestamps |
| `CustomPatterns` | Empty | Complete regex matches are fully masked, even when `DetectTypes` is `None` |
| `CustomRegexTimeout` | 100 ms | Finite timeout for custom rules; 1–1,000 ms allowed |

Options and pattern lists are snapshotted when the masker is constructed. Reuse the masker across calls and threads; construct a new one when settings change. Invalid options and regex patterns throw at construction.

## 5. JSON and logging rules

Annotate string properties with `[Sensitive]` for full masking, or specify a `SensitiveKind`. For positional records, use `[property: Sensitive(...)]` as shown above. Included fields work when `JsonSerializerOptions.IncludeFields` is enabled.

Annotated non-string members, including numbers, dates, objects, and collections, are omitted from serialization. An unannotated nested object or collection is traversed normally. Original objects and deserialized input values are unchanged.

Call `AddPiiMasking()` before first using the options and reuse those options afterward. Configure naming policies and a default reflection resolver before registration. The current JSON integration supports `DefaultJsonTypeInfoResolver`; source-generated contexts, custom resolver chains, trimming, and NativeAOT are outside its supported scope. Custom converters that write an object themselves must handle masking themselves.

For application logging, pass `Pii.MaskText(message)` for mixed text, or `JsonSerializer.Serialize(value, logOptions)` for an annotated object. Mask individual structured-log values with `Pii.Mask(...)` when their type is known. The current core package has no automatic `ILogger`, Serilog, or HTTP middleware integration. Use separate JSON options for logs and ordinary API responses when they need different output.

## 6. Detection limits and troubleshooting

- Email detection covers common ASCII addresses with dotted domains. It intentionally preserves the domain.
- Card detection accepts 13–19 digits with optional spaces/hyphens and requires a valid Luhn checksum. Explicit `CardNumber` masking still masks invalid numbers.
- Phones with `+` accept 8–15 digits. National formatted numbers generally require 10–12 digits; parentheses permit longer candidates up to 15 digits. Plain longer candidates are treated conservatively to avoid masking failed card candidates as phones.
- Token detection covers common Bearer, JWT-like, AWS access-key ID, GitHub, Stripe secret/restricted, Slack, and credential-pair formats. It cannot recognize every provider or arbitrary secret.
- Overlapping different detections are fully masked across their combined span. This can hide surrounding text such as an ambiguous unquoted `password=email` assignment.
- Any regex timeout masks the entire input. Bound message sizes and custom-rule counts in your application.
- `Mask` and `MaskText` preserve null/empty values. Known typed masking and common scan outputs support repeated use; `Auto` may mask more on another pass.
- If a log still contains an unknown secret, mask that known value with `Full`, annotate its property, or add a custom rule. Detection is heuristic and does not establish compliance.
- If JSON configuration throws after first serialization, create a new options instance and register masking before use. If a package cannot be found, verify the local `.nupkg` exists and `--source` points to its directory.

## 7. Reproduce this guide's tests

From `C:\Github\PiiMask`, run:

```powershell
pwsh -File .\scripts\Test-UsageGuide.ps1
```

The script extracts the exact C# block above into a fresh consumer project, installs the generated `.nupkg` from `artifacts` into a fresh package cache, builds it, and runs every example on .NET 8, 9, and 10. It fails if any expected output differs. Install all three runtimes to run this verification.

For the library regression suite, run:

```powershell
dotnet test .\tests\PiiMask.Tests\PiiMask.Tests.csproj -c Release
```

## 8. What the verification covers

The complete example above contains 32 checks. Running it on .NET 8, 9, and 10 performs 96 checks against the packaged library. The library regression suite contains 100 cases per framework, including repeated email masking with punctuation, short credentials, malformed values, custom-rule timeouts, overlapping matches, false positives, concurrency, and JSON serialization/deserialization.

The sample checks masking outputs, custom rules, detector options, JSON serialization, nested collections, options reuse, source-object preservation, and deserialization. Verification scripts create a fresh consumer project and package cache so the examples run against the requested package version rather than a project reference.

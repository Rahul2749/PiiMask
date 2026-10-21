# Implementation plan review

Reviewed against the supplied draft on 20 October 2026.

## Assessment

The draft is a practical v0.1 scope: small public API, dependency-free runtime, conservative detection,
attribute-based JSON masking, tests, packaging, and a separate benchmark project.
The repository was empty, with no existing Git history or source to preserve.
The implementation targets `net8.0;net9.0;net10.0` immediately, per the user's request.

## Decisions made during implementation

1. **Distinguish explicit values from arbitrary text.** `Pii.Mask` defaults to full masking; `Pii.MaskText`
   preserves unrecognized text. `Auto` fully masks if scanning makes no change, but is not a whole-text secrecy guarantee.
2. **Do not perform successive regex replacements.** Detectors collect spans from the original text.
   Different overlapping detections are merged and fully masked. Sequential replacement can hide a larger secret
   from a later detector, such as an email inside a password.
3. **Ambiguous phone/card candidates need a policy.** Unprefixed formatted phone detection is limited to 10–12 digits
   unless parentheses are present. Longer unprefixed candidates that fail Luhn remain unchanged.
   This deliberately narrows the draft's 10–15 digit rule to protect its invalid-card negative cases.
4. **Typed card masking and card detection differ.** Detection requires Luhn. Explicit card masking does not:
   marking an invalid number sensitive must still redact it.
5. **JSON limitations must be enforced.** Registration rejects source-generated contexts and custom resolver chains.
   It retains modifiers from a default reflection resolver and preserves deserialization behavior.
   Sensitive non-string members are omitted without evaluating their getters.
6. **Custom patterns cannot be source generated.** Built-ins use generated regexes with finite timeouts;
   user-provided patterns compile at runtime with validated finite timeouts. Any timeout fully masks the input.
7. **Idempotency has limits.** Explicit typed masking and normal scanner outputs are tested.
   `Auto` may further redact text on a second pass; retaining arbitrary previously masked text could leak unknown sensitive values.
8. **Zero dependencies means runtime dependencies.** Tests use xUnit and benchmarks use BenchmarkDotNet.
   The core package has no package references.
9. **SourceLink requires repository metadata.** .NET SDK support is enabled through packaging properties,
   but meaningful links require a real Git repository and remote. No repository URL is invented.
10. **Release tasks are separate from implementation.** The document's suggestions to reserve IDs,
    create a remote repository, set account secrets, and publish are not treated as authorization to perform those account actions.

The JSON approach is based on Microsoft's
[contract customization documentation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/custom-contracts),
which supports getter and serialization-condition customization through metadata contracts.

## Release work still required

- Confirm NuGet package-ID availability and account ownership at publication time. Search results alone do not reserve an ID.
- Create/connect the intended Git remote and verify SourceLink in a package built from a committed revision.
- Decide whether to apply for a NuGet prefix reservation; reservation eligibility is a separate account process.
- Configure GitHub's `nuget` environment and scoped `NUGET_API_KEY` secret.
- Run representative benchmarks and review the public API before promoting the preview.
- Try the preview with real application log formats, including region-specific phone formats.
- Review maintained alternatives before making competitive claims; the draft's popularity and comparison claims are unverified.

This implementation does not claim GDPR compliance, comprehensive secret detection, or production-proven accuracy.

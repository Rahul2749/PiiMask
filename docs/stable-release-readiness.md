# PiiMask 0.1.0 — first stable release readiness

Prepared on 21 October 2026. **Ready for NuGet publication; not published yet.**

Version `0.1.0` promotes the existing `0.1.0-preview.1` release line to its first stable version. It supports .NET 10, .NET 9, and .NET 8. The public API signatures are unchanged; the implementation fixes and expanded tests are described below.

## Changes from the preview

- Fixed repeat masking of email addresses when retained local-part characters include punctuation. For example, `a.b@example.com` becomes `a.*@example.com` and remains that way on another pass, rather than becoming fully masked.
- Preserved full masking for malformed partially redacted email values, including invalid domains and trailing content.
- Moved typed formatting into a shared internal path so a formatter regex timeout during free-text scanning reaches the scanner's whole-message redaction fallback.
- Preserved stable output when rescanning fully masked short credentials.
- Added 25 regression cases; the suite now has 100 cases per target framework.
- Updated the NuGet README, complete guide, release notes, test-script defaults, and package consumer project for stable version `0.1.0`.
- Added package/documentation/symbol verification to CI and release workflows.

## Verification performed

Windows; .NET SDK 10.0.303. Dependency restore completed successfully with the SDK's default NuGet checks enabled. All builds below completed with zero warnings and errors.

| Check | .NET 8.0.30 | .NET 9.0.19 | .NET 10.0.11 |
| --- | --- | --- | --- |
| Stable unit/regression suite | 100 passed | 100 passed | 100 passed |
| Stable package's complete guide | 32 checks passed | 32 checks passed | 32 checks passed |
| Stable package's NuGet README examples | 4 programs passed | 4 programs passed | 4 programs passed |
| Fresh stable package consumer, including email fix | Passed | Passed | Passed |
| Existing published preview, restored from nuget.org | 32 guide checks passed | 32 guide checks passed | 32 guide checks passed |

The stable package consumers restore from the local `.nupkg` into fresh package caches, not a source project reference. README programs are extracted from the exact Markdown and compared against their documented output.

`Test-ReleasePackage.ps1` verified package ID/version, MIT license expression, NuGet README metadata, release notes, zero runtime dependencies, and bundled documentation. Each packaged assembly/XML file and symbol PDB matches its Release build by SHA-256.

NuGet's public registry listed only `0.1.0-preview.1` when checked during preparation. Stable version `0.1.0` was available for a new publication; it has not been pushed by this preparation task.

## Release artifacts

The files for publication are copied together into `artifacts/releases/0.1.0/`:

- `PiiMask.0.1.0.nupkg`
- `PiiMask.0.1.0.snupkg`
- `PiiMask.0.1.0.verification.json` with their SHA-256 hashes
- A copy of this readiness report

Use the exact versioned archives when publishing, rather than a wildcard that might also include the previous preview.

## Scope and remaining limits

- Initial preparation testing was on Windows across all three runtimes. The GitHub repository is now connected; the Windows/Linux workflows run when the source is pushed.
- SourceLink is configured for `https://github.com/Rahul2749/PiiMask` and the committed build revision. Matching portable symbols and source maps are verified before publication.
- Detection remains heuristic; this release does not claim to detect all secrets or establish compliance. Existing README limits still apply.
- JSON integration remains reflection-only. Source-generated contexts, trimming, and NativeAOT are outside its supported scope.
- Benchmarks compile, but no throughput measurements were run or claimed.

These limitations are documented in the distributed README/release notes where relevant; this preparation does not remove .NET 8 or 9 compatibility or make .NET 10 the minimum framework.

## Commands to reproduce

```powershell
dotnet restore PiiMask.slnx
dotnet build PiiMask.slnx -c Release --no-restore
dotnet test tests/PiiMask.Tests/PiiMask.Tests.csproj -c Release --no-build
dotnet pack src/PiiMask/PiiMask.csproj -c Release --no-build -o artifacts
pwsh -File scripts/Test-ReleasePackage.ps1
pwsh -File scripts/Test-NuGetReadme.ps1
pwsh -File scripts/Test-UsageGuide.ps1
```

This environment required unrestricted local test-host communication and a single MSBuild worker (`-m:1`) with compiler sharing disabled (`-p:UseSharedCompilation=false`). These were execution-environment workarounds, not package requirements.

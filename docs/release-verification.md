# Prerelease publication verification

- Release: `PiiMask 0.1.0-preview.1`
- Date: 20 October 2026
- NuGet owner: `rahulnagrikar`
- Package page: https://www.nuget.org/packages/PiiMask/0.1.0-preview.1

## Submitted artifacts

NuGet's web portal confirmed successful upload of both the package and the symbols package.

| Artifact | Local SHA-256 |
| --- | --- |
| `PiiMask.0.1.0-preview.1.nupkg` | `CD7F1B13FE482F368BAB5329F5D65337814D6B4AA3981CA11DCEDAD8C6DBA6E5` |
| `PiiMask.0.1.0-preview.1.snupkg` | `5518ACD4FB5AE566A5982BC924B40E08D9841C47DEBC28058458634295DB9176` |

These identify the submitted local archives. NuGet repository signing can change the distributed archive hash.

## Validation before submission

- Release build: zero warnings and errors for .NET 8, 9, and 10.
- Regression tests: 75 passed per framework, 225 total.
- Exact NuGet README examples: all four compiled and matched documented output on all three frameworks, 12 executions total.
- Exact usage-guide program: 32 checks passed per framework, 96 total, using a fresh cache and the generated package.
- Package contents: README, full usage guide, release notes, MIT license, XML API docs, and all three framework assemblies.
- Manifest: embedded `README.md` configured for the NuGet page, release notes present, and no runtime package dependencies.
- NuGet upload preview: verified package ID, preview version, owner, MIT license, framework dependency groups, and rendered usage instructions.

The last archive rebuild changed only the prose for bitwise OR in the options table; all executable examples and library assemblies were unchanged.

## Public-feed verification

NuGet package validation has passed, the rendered README is visible on the package page, and the public feed lists `0.1.0-preview.1`.

A new consumer project restored `PiiMask 0.1.0-preview.1` exclusively from `https://api.nuget.org/v3/index.json` into a fresh package cache. The exact guide program compiled with zero warnings/errors and passed all 32 checks on .NET 8.0.30, 9.0.19, and 10.0.11: 96 successful checks against the published package.

The public download's README, usage guide, release notes, and three assemblies matched the submitted archive's file hashes. The final NuGet package page shows the prerelease, installation command, and rendered README with no pending validation, package indexing, or symbol indexing warnings.

Screenshot: `artifacts/nuget-published.png`.

Reproduce installation verification from the source workspace:

```powershell
pwsh -File ./scripts/Test-UsageGuide.ps1 -Source https://api.nuget.org/v3/index.json
```

Users can install the preview with:

```shell
dotnet add package PiiMask --version 0.1.0-preview.1
```

## Release scope

The source workspace has no Git remote, so this build has no working SourceLink repository URLs. GitHub workflows are prepared, including documentation-example checks, but were not used for this first web-portal release. No GitHub repository or API credentials were created.

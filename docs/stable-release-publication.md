# PiiMask 0.1.0 publication

Date: 21 October 2026.

## Release identity

- Version: `0.1.0`, the first stable release.
- Source: https://github.com/Rahul2749/PiiMask
- Tag: https://github.com/Rahul2749/PiiMask/tree/v0.1.0
- Package source commit: `d26ff547424b6d24e5d3ae5a976c5ba114d2f914`.
- NuGet page: https://www.nuget.org/packages/PiiMask/0.1.0
- NuGet owner: `rahulnagrikar`.

## Completed checks

- Source committed and pushed to the supplied repository; `v0.1.0` points to that release revision.
- Windows and Linux CI passed: https://github.com/Rahul2749/PiiMask/actions/runs/39638831242
- Tag-triggered release build passed: https://github.com/Rahul2749/PiiMask/actions/runs/39640069075
- Local final build: zero warnings/errors; 100 tests passed per framework on .NET 8, 9, and 10.
- Final package: all 12 NuGet README example executions and 96 complete-guide checks passed from fresh local package caches.
- Package metadata, license, bundled documents, XML docs, assemblies, and matching PDBs verified.
- SourceLink JSON embedded in each portable PDB maps to the exact release commit.
- The checksums of all seven original source documents in each PDB matched both the working files and their GitHub downloads: 21 PDB source checks verified.

## Submitted archive hashes

| Artifact | SHA-256 |
| --- | --- |
| `PiiMask.0.1.0.nupkg` | `9751D437F06C47285F8B7F6A00525C5FE2BC4C19E9492CD811BBDC92F99BFBC7` |
| `PiiMask.0.1.0.snupkg` | `C18B67BE97285EDDC4AC6BF36C0428AF157375060C09AF920B006E5C68F3F6E6` |

The NuGet service may repository-sign the distributed archive, changing its container hash. Public package file contents are checked separately.

## Publication status

**Published:** https://www.nuget.org/packages/PiiMask/0.1.0

**GitHub stable release:** https://github.com/Rahul2749/PiiMask/releases/tag/v0.1.0

The GitHub release is public, is not marked prerelease, and includes the package, symbols, and `SHA256SUMS.txt`. The NuGet package is listed and installable as stable `0.1.0`.

Fresh NuGet HTTP and package caches were used to restore exclusively from `https://api.nuget.org/v3/index.json`. The complete guide passed 32 checks per runtime, 96 total. Separate public-package consumer checks for free-text masking, JSON, and repeated email masking passed on all three runtimes. All consumer builds completed with zero warnings/errors.

The downloaded README, license, full guide, release notes, and three assemblies matched the submitted archive by file SHA-256. The distributed manifest identifies the exact GitHub commit above. NuGet's README now links to the tagged guide/release notes; those tagged documents also matched their packaged counterparts.

NuGet accepted the matching symbols archive, and the final live page showed no remaining package or symbol validation/indexing warnings. The PDBs are also downloadable as a GitHub release asset, and their SourceLink maps/checksums were verified as described above.

The publication-record commit also passed Windows/Linux CI: https://github.com/Rahul2749/PiiMask/actions/runs/39642089931

```shell
dotnet add package PiiMask --version 0.1.0
```

## Future releases

The GitHub release workflow currently builds/verifies tag artifacts. Automated NuGet pushing is gated by `NUGET_PUBLISH_ENABLED=true` and a scoped `NUGET_API_KEY` in the `nuget` environment. This first stable release uses the existing signed-in NuGet web portal; no new API key was created or committed.

See [publishing instructions](publishing.md) for future release setup.

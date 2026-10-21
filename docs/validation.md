# Local validation

Validated on Windows on 20 October 2026 using SDK 10.0.303.

| Check | Result |
| --- | --- |
| Release build of library, tests, benchmarks on all three targets | Passed; zero warnings and errors |
| Unit/regression tests on .NET 8.0.30 | 75 passed, 0 failed |
| Unit/regression tests on .NET 9.0.19 | 75 passed, 0 failed |
| Unit/regression tests on .NET 10.0.11 | 75 passed, 0 failed |
| NuGet and symbol package generation | Passed: `0.1.0-preview.1` |
| Package inspection | Three target assemblies and XML docs, README, MIT license, empty dependency groups |
| Package consumption on all three runtimes | Passed; isolated local package cache, text and JSON checks |

The test suite covers direct masking, null/empty/short values, repeated masking, options,
positive and negative detection cases, overlapping secrets, custom-pattern timeouts,
concurrent use, JSON members, nested objects and records, inheritance, non-string omission,
deserialization, resolver modifiers, and rejection of source-generated resolvers.

Environment-specific workarounds: restore needed network access outside the restricted sandbox,
and the test host required unrestricted local process communication. The successful build used
`-m:1 -p:UseSharedCompilation=false`; the successful test run used
`-m:1 -p:TestTfmsInParallel=false`. These are local execution restrictions, not package requirements.

Benchmarks were compiled, but throughput was not measured. Linux CI and GitHub release workflows
were initially prepared before a Git remote was connected. The later stable publication passed both Windows/Linux GitHub CI and SourceLink verification; see the stable publication receipt below.

`PiiMask 0.1.0-preview.1` has since been published to NuGet under `rahulnagrikar`.
Its NuGet README examples passed 12 executions before upload; the complete guide passed 96 checks
against the local release package and another 96 after restoring the published package from nuget.org
into a fresh cache. See [release verification](release-verification.md) for the publication receipt and content checks.

## Stable candidate prepared on 21 October 2026

`0.1.0` is prepared locally with an email masking fix and 25 added regression cases. All 300 test executions, 96 stable-guide checks, 12 stable README example runs, and three fresh package consumer runs passed. Package contents and matching symbols were verified. See [stable release readiness](stable-release-readiness.md) for the artifacts, changes, scope, and exact publication status.

Stable `0.1.0` was subsequently published to [NuGet](https://www.nuget.org/packages/PiiMask/0.1.0) and [GitHub Releases](https://github.com/Rahul2749/PiiMask/releases/tag/v0.1.0). Public-feed guide and consumer checks passed on .NET 8, 9, and 10. SourceLink was verified against GitHub and CI passed on Windows and Linux. See [stable publication verification](stable-release-publication.md) for the final receipt.

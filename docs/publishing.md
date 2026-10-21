# Release process

The source repository is https://github.com/Rahul2749/PiiMask.

The first stable release uses the NuGet web portal. Tag pushes build/test the package and upload GitHub Actions artifacts. The automated NuGet publication job is deliberately opt-in to avoid failed or duplicate pushes before a repository credential is configured.

For future automated releases:

1. Configure a scoped `NUGET_API_KEY` secret in the GitHub `nuget` environment.
2. Configure any desired environment protection rules.
3. Set the repository Actions variable `NUGET_PUBLISH_ENABLED` to `true`.
4. Update the package version, documentation installation examples, script defaults, and package-consumer project together.
5. Commit changes, verify CI, then push the matching `v...` tag.

The release workflow builds, runs tests, checks package contents and matching symbols, and executes the exact NuGet README/guide examples before the gated publish job. The .NET 10 SDK reads the API key from the environment rather than a command-line argument. No credential is committed to source.

Source files use LF line endings on every platform so portable PDB checksums correspond to the files served by GitHub SourceLink URLs. Build official artifacts from a clean committed revision with `ContinuousIntegrationBuild=true`.

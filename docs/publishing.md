# Release process

The source repository is https://github.com/Rahul2749/PiiMask.

Tag pushes build and test the package and upload GitHub Actions artifacts. Automated NuGet publication requires a repository credential and an explicit repository setting.

To configure automated releases:

1. Configure a scoped `NUGET_API_KEY` secret in the GitHub `nuget` environment.
2. Configure any desired environment protection rules.
3. Set the repository Actions variable `NUGET_PUBLISH_ENABLED` to `true`.
4. Update the package version, documentation installation examples, script defaults, and package-consumer project together.
5. Commit changes, verify CI, then push the matching `v...` tag.

The release workflow builds, runs tests, checks package contents and matching symbols, and executes the exact NuGet README/guide examples before the gated publish job. The .NET 10 SDK reads the API key from the environment rather than a command-line argument. No credential is committed to source.

Source files use LF line endings on every platform so portable PDB checksums correspond to the files served by GitHub SourceLink URLs. Build official artifacts from a clean committed revision with `ContinuousIntegrationBuild=true`.

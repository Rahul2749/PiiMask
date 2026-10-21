[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$')]
    [string]$PackageVersion = '0.1.0'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$feedPath = Join-Path $repoRoot 'artifacts'
$packagePath = Join-Path $feedPath "PiiMask.$PackageVersion.nupkg"
$symbolsPath = Join-Path $feedPath "PiiMask.$PackageVersion.snupkg"
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Assert-EntryMatchesFile($archive, [string]$entryName, [string]$sourcePath) {
    $entry = $archive.GetEntry($entryName)
    if ($null -eq $entry) { throw "Missing package entry: $entryName" }
    $stream = $entry.Open()
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose(); $sha.Dispose() }
    if ($hash -ne (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash) {
        throw "Packaged file differs from the verified source/build: $entryName"
    }
}

$package = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $manifestEntry = $package.GetEntry('PiiMask.nuspec')
    if ($null -eq $manifestEntry) { throw 'Missing package manifest.' }
    $reader = [System.IO.StreamReader]::new($manifestEntry.Open())
    try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $metadata = $manifest.package.metadata
    if ($metadata.id -ne 'PiiMask' -or $metadata.version -ne $PackageVersion) { throw 'Incorrect package identity.' }
    if ($metadata.license.InnerText -ne 'MIT' -or $metadata.readme -ne 'README.md') { throw 'Missing MIT license expression or NuGet README metadata.' }
    if ([string]::IsNullOrWhiteSpace($metadata.releaseNotes)) { throw 'Missing release notes.' }
    if ($metadata.repository.url -ne 'https://github.com/Rahul2749/PiiMask' -or $metadata.repository.type -ne 'git') { throw 'Incorrect source repository metadata.' }
    $commit = [string]$metadata.repository.commit
    if ($commit -notmatch '^[a-fA-F0-9]{40}$') { throw 'Package must identify its committed source revision.' }
    if (@($manifest.SelectNodes('//*[local-name()="dependency"]')).Count -ne 0) { throw 'Unexpected runtime package dependencies.' }
    foreach ($file in @('README.md', 'LICENSE', 'docs/usage-guide.md', 'docs/release-notes.md')) {
        Assert-EntryMatchesFile $package $file (Join-Path $repoRoot $file)
    }
    foreach ($framework in @('net8.0', 'net9.0', 'net10.0')) {
        $sourceLinkPath = Join-Path $repoRoot "src/PiiMask/obj/Release/$framework/PiiMask.sourcelink.json"
        $sourceLink = Get-Content -LiteralPath $sourceLinkPath -Raw | ConvertFrom-Json
        $maps = @($sourceLink.documents.PSObject.Properties.Value)
        if (-not ($maps | Where-Object { $_ -eq "https://raw.githubusercontent.com/Rahul2749/PiiMask/$commit/*" })) { throw "SourceLink does not map $framework to the package's source commit." }
        foreach ($extension in @('dll', 'xml')) {
            $name = "PiiMask.$extension"
            Assert-EntryMatchesFile $package "lib/$framework/$name" (Join-Path $repoRoot "src/PiiMask/bin/Release/$framework/$name")
        }
    }
    $readme = Get-Content -LiteralPath (Join-Path $repoRoot 'README.md') -Raw
    if (-not $readme.Contains("dotnet add package PiiMask --version $PackageVersion")) { throw 'README installation version differs from package version.' }
    if ($PackageVersion -notlike '*-*' -and $readme -match 'enable \*\*Include prerelease\*\*') { throw 'Stable release README incorrectly requires prerelease selection.' }
}
finally { $package.Dispose() }

$symbols = [System.IO.Compression.ZipFile]::OpenRead($symbolsPath)
try {
    foreach ($framework in @('net8.0', 'net9.0', 'net10.0')) {
        Assert-EntryMatchesFile $symbols "lib/$framework/PiiMask.pdb" (Join-Path $repoRoot "src/PiiMask/bin/Release/$framework/PiiMask.pdb")
    }
}
finally { $symbols.Dispose() }

[pscustomobject]@{
    Package = $packagePath
    PackageSha256 = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
    Symbols = $symbolsPath
    SymbolsSha256 = (Get-FileHash -LiteralPath $symbolsPath -Algorithm SHA256).Hash
    Frameworks = @('net8.0', 'net9.0', 'net10.0')
    Repository = 'https://github.com/Rahul2749/PiiMask'
    Commit = $commit
    RuntimePackageDependencies = 0
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $feedPath "PiiMask.$PackageVersion.verification.json") -Encoding utf8
Write-Output "PiiMask $PackageVersion package, documentation, three target assemblies, API XML, and matching symbols verified."

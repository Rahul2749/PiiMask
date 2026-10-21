[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$')]
    [string]$PackageVersion = '0.1.0',
    [string]$Source = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$guidePath = Join-Path $repoRoot 'docs/usage-guide.md'
$feedPath = Join-Path $repoRoot 'artifacts'
$packagePath = Join-Path $feedPath "PiiMask.$PackageVersion.nupkg"
if ([string]::IsNullOrWhiteSpace($Source)) { $Source = $feedPath }
if ($Source -eq $feedPath -and -not (Test-Path -LiteralPath $packagePath)) {
    throw 'Build and pack PiiMask first: dotnet pack src/PiiMask/PiiMask.csproj -c Release -o artifacts'
}

$guide = Get-Content -LiteralPath $guidePath -Raw
$blocks = [regex]::Matches($guide, '(?ms)^```csharp\r?\n(.*?)^```\s*$')
if ($blocks.Count -ne 1) {
    throw 'Expected exactly one complete C# example in the usage guide.'
}

# A fresh directory and package cache ensure this run consumes the current nupkg.
$runPath = Join-Path $feedPath ('usage-guide-check/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runPath -Force | Out-Null
$projectPath = Join-Path $runPath 'UsageGuide.csproj'
$programPath = Join-Path $runPath 'Program.cs'
$cachePath = Join-Path $runPath 'packages'
Set-Content -LiteralPath $programPath -Value $blocks[0].Groups[1].Value -Encoding utf8
$projectSource = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="PiiMask" Version="__PACKAGE_VERSION__" />
  </ItemGroup>
</Project>
'@
Set-Content -LiteralPath $projectPath -Value $projectSource.Replace('__PACKAGE_VERSION__', $PackageVersion) -Encoding utf8

& dotnet restore $projectPath --source $Source --packages $cachePath -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'Usage guide package restore failed.' }
& dotnet build $projectPath -c Release --no-restore -m:1 -p:UseSharedCompilation=false --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Usage guide example compilation failed.' }
foreach ($framework in @('net8.0', 'net9.0', 'net10.0')) {
    & dotnet run --project $projectPath -c Release -f $framework --no-build --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Usage guide examples failed on $framework." }
}
Write-Output "Verified the usage guide against PiiMask $PackageVersion from $Source."

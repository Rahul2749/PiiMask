[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$')]
    [string]$PackageVersion = '0.1.0'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$feedPath = Join-Path $repoRoot 'artifacts'
$readme = Get-Content -LiteralPath (Join-Path $repoRoot 'README.md') -Raw
$examples = [regex]::Matches($readme, '(?ms)^```csharp\r?\n(.*?)^```\r?\n\s*Output:\s*\r?\n```text\r?\n(.*?)^```\s*$')
if ($examples.Count -ne 4) { throw 'Expected four runnable NuGet README examples with expected output.' }

$runPath = Join-Path $feedPath ('nuget-readme-check/' + [guid]::NewGuid().ToString('N'))
$cachePath = Join-Path $runPath 'packages'
$number = 0
foreach ($example in $examples) {
    $number++
    $examplePath = Join-Path $runPath "example-$number"
    New-Item -ItemType Directory -Path $examplePath -Force | Out-Null
    $projectPath = Join-Path $examplePath 'ReadmeExample.csproj'
    Set-Content -LiteralPath (Join-Path $examplePath 'Program.cs') -Value $example.Groups[1].Value -Encoding utf8
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
    & dotnet restore $projectPath --source $feedPath --packages $cachePath -p:NuGetAudit=false --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "README example $number restore failed." }
    & dotnet build $projectPath -c Release --no-restore -m:1 -p:UseSharedCompilation=false --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "README example $number compilation failed." }
    $expected = $example.Groups[2].Value.Replace("`r`n", "`n").TrimEnd()
    foreach ($framework in @('net8.0', 'net9.0', 'net10.0')) {
        $actualLines = & dotnet run --project $projectPath -c Release -f $framework --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "README example $number failed on $framework." }
        $actual = ($actualLines -join "`n").TrimEnd()
        if ($actual -cne $expected) { throw "README example $number output differs from the documentation on $framework." }
        Write-Output "NuGet README example $number passed on $framework."
    }
}
Write-Output 'All 12 NuGet README example executions matched their documented output.'

param([switch]$MusicOnly)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$buildFolder = Join-Path $projectRoot 'Temp/AudioSynthesis'
New-Item -ItemType Directory -Path $buildFolder -Force | Out-Null
$projectFile = Join-Path $buildFolder 'AudioGenerator.csproj'
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="../../Tools/Audio/GenerateGameAudio.cs" />
  </ItemGroup>
</Project>
'@ | Set-Content -LiteralPath $projectFile -Encoding UTF8
Push-Location -LiteralPath $projectRoot
try {
    $generatorArgs = @()
    if ($MusicOnly) { $generatorArgs += '--music-only' }
    dotnet run --project $projectFile -c Release -- @generatorArgs
    if ($LASTEXITCODE -ne 0) { throw 'Audio generation failed.' }
}
finally { Pop-Location }

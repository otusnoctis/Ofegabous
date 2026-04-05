param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "App\App.csproj"
$publishDir = Join-Path $repoRoot "artifacts\App\publish"
$releaseDir = Join-Path $repoRoot "artifacts\App\releases"
$propsPath = Join-Path $repoRoot "Directory.Build.props"

[xml]$props = Get-Content $propsPath
[xml]$project = Get-Content $projectPath

$packageId = $props.Project.PropertyGroup.TemplatePackageId
$appTitle = $props.Project.PropertyGroup.TemplateAppDisplayName
$authors = $props.Project.PropertyGroup.TemplateAuthors
$velopackVersion = $props.Project.PropertyGroup.VelopackVersion
$assemblyName = $project.Project.PropertyGroup.AssemblyName

if ([string]::IsNullOrWhiteSpace($assemblyName)) {
    $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
}

$mainExe = "$assemblyName.exe"

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

Write-Host "Publishing $appTitle $Version..."
dotnet publish $projectPath `
    -c Release `
    -f net10.0-windows10.0.19041.0 `
    -r win-x64 `
    --self-contained true `
    -p:VelopackPackOnPublish=false `
    -o $publishDir

$env:DOTNET_ROLL_FORWARD = "Major"

Write-Host "Packing Velopack release..."
dnx --yes vpk --version $velopackVersion pack `
    --packId $packageId `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe $mainExe `
    --packTitle $appTitle `
    --packAuthors $authors `
    --runtime "win-x64" `
    --noPortable `
    --outputDir $releaseDir

Write-Host "Velopack packages created in $releaseDir"

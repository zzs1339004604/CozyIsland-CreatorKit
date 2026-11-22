<#
.SYNOPSIS
Builds an external Cozy Island C# mod using an installed .NET SDK and local game references.
.EXAMPLE
.\Build-CodeMod.ps1 -ProjectPath '..\Examples\Example\Source\Example.csproj'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ProjectPath,
    [string]$GameManagedPath,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PlayerLayout.ps1')
$projectFile = (Resolve-Path -LiteralPath $ProjectPath).ProviderPath
if ([IO.Path]::GetExtension($projectFile) -ne '.csproj') { throw 'ProjectPath must point to a C# .csproj file.' }
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnetCommand) { throw 'Install the .NET SDK (not only the runtime) from https://dotnet.microsoft.com/download, then reopen PowerShell.' }
$sdks = & $dotnetCommand.Source --list-sdks
if ($LASTEXITCODE -ne 0 -or @($sdks).Count -eq 0) { throw 'A .NET SDK is required to compile C# mods.' }
if ([string]::IsNullOrWhiteSpace($GameManagedPath)) { $GameManagedPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'References/Managed' }
$managedRoot = Resolve-CozyFilePath $GameManagedPath
if (-not (Test-Path -LiteralPath (Join-Path $managedRoot 'Assembly-CSharp.dll'))) { throw 'Import the installed game references with Import-GameReferences.ps1 first.' }
if (-not (Test-Path -LiteralPath (Join-Path $managedRoot '0Harmony.dll'))) { throw 'The supplied game references predate the code-Mod API. Update and import the game references again.' }
$referenceManifest = Join-Path $managedRoot 'reference-manifest.json'
if (Test-Path -LiteralPath $referenceManifest -PathType Leaf) {
    $reference = Get-Content -LiteralPath $referenceManifest -Raw | ConvertFrom-Json
    if ($reference.compatibility.capability -ne 'unique-id-manifest-v1') {
        throw 'Reimport the references with this CreatorKit to verify the Player supports UniqueID.'
    }
    foreach ($actual in Get-CozyPlayerFingerprint ([pscustomobject]@{ managed = $managedRoot })) {
        $expected = @($reference.compatibility.files | Where-Object { $_.file -eq $actual.file })
        if ($expected.Count -ne 1 -or $expected[0].sha256 -ne $actual.sha256) {
            throw "Reference $($actual.file) differs from the verified Player. Run Import-GameReferences.ps1 again."
        }
    }
} else {
    Write-Warning 'These references lack a compatibility report. Build success alone does not verify the Player supports UniqueID. Run Import-GameReferences.ps1 for verified references.'
}
$arguments = @('build', $projectFile, '--configuration', $Configuration, '--nologo', "-p:GameManagedPath=$managedRoot")
& $dotnetCommand.Source @arguments
if ($LASTEXITCODE -ne 0) { throw "Code mod build failed with exit code $LASTEXITCODE." }
Write-Host 'Build complete. The sample projects write their own DLL to ../Code/; game and Harmony DLLs are references only.'

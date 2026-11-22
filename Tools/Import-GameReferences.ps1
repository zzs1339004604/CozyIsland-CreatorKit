<#
.SYNOPSIS
Copies compile-time references from your own complete Cozy Island Mono Player.
.EXAMPLE
.\Import-GameReferences.ps1 -GameDirectory 'D:\SteamLibrary\steamapps\common\Cozy Island'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameDirectory,
    [string]$Destination,
    [switch]$Rosetta
)
$ErrorActionPreference = 'Stop'
$kitRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'PlayerLayout.ps1')
$player = Get-CozyPlayer $GameDirectory
$managedRoot = $player.managed
if ([string]::IsNullOrWhiteSpace($Destination)) { $Destination = Join-Path $kitRoot 'References/Managed' }
$destinationRoot = Resolve-CozyFilePath $Destination
if ($destinationRoot.TrimEnd('/', '\') -eq $managedRoot.TrimEnd('/', '\')) { throw 'Destination must differ from the Player Managed directory.' }
if (Test-Path -LiteralPath $destinationRoot) {
    $item = Get-Item -LiteralPath $destinationRoot
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Reference destination must not be a symbolic link or junction.' }
}
$compatibility = & (Join-Path $PSScriptRoot 'Test-GameCompatibility.ps1') -GameDirectory $GameDirectory -Rosetta:$Rosetta
[IO.Directory]::CreateDirectory($destinationRoot) | Out-Null
$manifestPath = Join-Path $destinationRoot 'reference-manifest.json'
$previousRecords = @()
if (Test-Path -LiteralPath $manifestPath) {
    $previousManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $previousRecords = @($previousManifest.files)
}
$sourceFiles = @(Get-ChildItem -LiteralPath $managedRoot -File -Filter '*.dll')
$sourceNames = @{}
$records = foreach ($source in $sourceFiles) {
    $sourceNames[$source.Name] = $true
    Copy-Item -LiteralPath $source.FullName -Destination (Join-Path $destinationRoot $source.Name) -Force
    [ordered]@{ file = $source.Name; sha256 = (Get-FileHash -LiteralPath $source.FullName -Algorithm SHA256).Hash.ToLowerInvariant(); bytes = $source.Length }
}
# Remove only unchanged files owned by a previous import, never unrelated DLLs in a custom destination.
foreach ($previous in $previousRecords) {
    $name = [string]$previous.file
    if ($name -ne [IO.Path]::GetFileName($name) -or [IO.Path]::GetExtension($name) -ne '.dll') { continue }
    if ($sourceNames.ContainsKey($name)) { continue }
    $oldPath = Join-Path $destinationRoot $name
    if ((Test-Path -LiteralPath $oldPath -PathType Leaf) -and
        (Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash -eq $previous.sha256) {
        Remove-Item -LiteralPath $oldPath -Force
    }
}
$report = [ordered]@{
    generatedUtc = [DateTime]::UtcNow.ToString('o')
    sourceGameDirectory = $player.path
    compatibility = $compatibility
    sourceManagedDirectory = $managedRoot
    usage = 'Local compile-time references only. Do not publish these game DLLs with mods or the creator kit.'
    files = @($records)
}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Host "Imported $($sourceFiles.Count) game references into $destinationRoot"
Write-Host 'After any game update, import again and rebuild your code mods.'

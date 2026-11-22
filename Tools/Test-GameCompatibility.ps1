<#
.SYNOPSIS
Checks a complete Player for UniqueID support and compares its assemblies to the kit's paired build.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameDirectory,
    [string]$ResultPath,
    [switch]$Rosetta
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PlayerLayout.ps1')
$player = Get-CozyPlayer $GameDirectory
$kitRoot = Split-Path -Parent $PSScriptRoot
$identityPath = Join-Path $kitRoot 'PlayerIdentity.json'
$identity = if (Test-Path -LiteralPath $identityPath -PathType Leaf) {
    Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
} else { $null }
$fingerprint = @(Get-CozyPlayerFingerprint $player)
$matchingProfile = Find-CozyPlayerPairing $player $identity $fingerprint
$pairing = if ($null -ne $matchingProfile) { 'exact' }
    elseif (@($identity.players | Where-Object { $_.platform -eq $player.platform }).Count -gt 0) { 'unmatched' }
    else { 'unpinned' }

# The temporary DLL exercises the Player's shared DLL inventory. Validation reads its metadata.
$probeId = [guid]::NewGuid()
$probeRoot = Join-Path ([IO.Path]::GetTempPath()) ('CozyUniqueIdProbe-' + $probeId.ToString('N'))
$probeAssembly = Join-Path $probeRoot 'Probe.dll'
try {
    [IO.Directory]::CreateDirectory($probeRoot) | Out-Null
    Add-Type -TypeDefinition "internal sealed class Probe$($probeId.ToString('N')) {}" -OutputAssembly $probeAssembly
    [ordered]@{
        schemaVersion = 1
        UniqueID = $probeId.ToString('D').ToUpperInvariant()
        title = 'CreatorKit compatibility probe'
        author = 'Cozy Island'
        version = '1.0.0'
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $probeRoot 'mod.json') -Encoding UTF8

    try {
        $report = & (Join-Path $PSScriptRoot 'Validate-Mod.ps1') -GameExe $GameDirectory -ModDirectory $probeRoot -ResultPath $ResultPath -Rosetta:$Rosetta
    } catch {
        throw "The supplied Player did not pass the UniqueID compatibility probe. Use the complete paired Workshop-enabled build. $($_.Exception.Message)"
    }
    if ($report.UniqueID -cne $probeId.ToString('D') -or $report.id -cne $probeId.ToString('D') -or $report.files -ne 2) {
        throw 'The supplied Player did not return the normalized UniqueID and expected file count. Use the paired Workshop-enabled build.'
    }
} finally {
    if (Test-Path -LiteralPath $probeRoot) { Remove-Item -LiteralPath $probeRoot -Recurse -Force }
}
if ($pairing -eq 'exact') {
    Write-Host "Player assembly fingerprints match the pinned $($player.platform) profile."
} else {
    Write-Warning "UniqueID validation passed, but the $($player.platform) Player is $pairing for this kit. This does not establish compatibility with the pinned examples, game APIs or assets."
}
[pscustomobject]@{
    platform = $player.platform
    sourceRevision = $identity.sourceRevision
    pairing = $pairing
    capability = 'unique-id-manifest-v1'
    gameVersion = $report.gameVersion
    files = $fingerprint
    profile = $matchingProfile
}

<#
.SYNOPSIS
Runs the supplied Windows executable or macOS .app validator and returns its JSON result.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameExe,
    [Parameter(Mandatory = $true)][string]$ModDirectory,
    [string]$ResultPath,
    [ValidateRange(30, 3600)][int]$TimeoutSeconds = 180,
    [switch]$Rosetta
)
& (Join-Path $PSScriptRoot 'Invoke-ModOperation.ps1') -Operation validate -GameExe $GameExe -ModDirectory $ModDirectory -ResultPath $ResultPath -TimeoutSeconds $TimeoutSeconds -Rosetta:$Rosetta

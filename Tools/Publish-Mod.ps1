<#
.SYNOPSIS
Publishes through the supplied Player and the signed-in Steam client. The game chooses visibility if omitted.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameExe,
    [Parameter(Mandatory = $true)][string]$ModDirectory,
    [ValidateSet('private', 'public')][string]$Visibility,
    [string]$ResultPath,
    [ValidateRange(30, 3600)][int]$TimeoutSeconds = 750,
    [switch]$Rosetta
)
& (Join-Path $PSScriptRoot 'Invoke-ModOperation.ps1') -Operation upload @PSBoundParameters

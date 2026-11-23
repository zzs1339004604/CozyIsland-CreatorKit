<#
.SYNOPSIS
Creates a new code Mod with a UUID generated once and stored as uuid.
.EXAMPLE
./Tools/New-CodeMod.ps1 -ProjectName garden -DisplayName 'Garden Tools' -Author 'Alice'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateLength(3, 48)]
    [ValidatePattern('(?-i)^[a-z][a-z0-9_-]*$')]
    [string]$ProjectName,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$DisplayName,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$Author
)
$ErrorActionPreference = 'Stop'
if ($ProjectName -match '^(con|prn|aux|nul|com[1-9]|lpt[1-9])$') {
    throw 'ProjectName must use a valid Windows directory name.'
}
if ([string]::IsNullOrWhiteSpace($DisplayName) -or $DisplayName.Length -gt 100 -or
    [string]::IsNullOrWhiteSpace($Author) -or $Author.Length -gt 100) {
    throw 'DisplayName and Author must be nonempty and at most 100 characters each.'
}

$kitRoot = Split-Path -Parent $PSScriptRoot
$workspace = Join-Path $kitRoot 'MyMods'
$destination = Join-Path $workspace $ProjectName
if (Test-Path -LiteralPath $destination) { throw "A project already exists at '$destination'. Choose a different project directory for a new work." }
$uuid = [Guid]::NewGuid().ToString('D')
$assemblySuffix = $uuid.Replace('-', '')
$assemblyName = "CozyMods.$assemblySuffix"
$namespace = "CozyMods.Mod_$assemblySuffix"
$entryType = "$namespace.ModEntry"
$temporary = Join-Path $workspace ('.new-' + [Guid]::NewGuid().ToString('N'))
$utf8 = New-Object System.Text.UTF8Encoding($false)

try {
    [IO.Directory]::CreateDirectory((Join-Path $temporary 'Source')) | Out-Null
    $manifest = [ordered]@{
        schemaVersion = 1
        displayName = $DisplayName.Trim()
        name = $ProjectName
        uuid = $uuid
        author = $Author.Trim()
        version = '1.0.0'
        description = ''
        dependencies = @()
        code = [ordered]@{ assembly = "Code/$assemblyName.dll"; type = $entryType }
    }
    [IO.File]::WriteAllText((Join-Path $temporary 'mod.json'), ($manifest | ConvertTo-Json -Depth 6), $utf8)
    $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="../../../Tools/CozyMod.props" />
  <PropertyGroup>
    <AssemblyName>$assemblyName</AssemblyName>
    <RootNamespace>$namespace</RootNamespace>
  </PropertyGroup>
</Project>
"@
    [IO.File]::WriteAllText((Join-Path $temporary 'Source/Mod.csproj'), $project, $utf8)
    $code = @"
using CozyIsland.Modding;

namespace $namespace {
    public sealed class ModEntry : ICozyMod {
        public void OnLoad(CozyModContext context) {
            context.Log("Loaded " + context.Version);
        }

        public void OnUnload() { }
    }
}
"@
    [IO.File]::WriteAllText((Join-Path $temporary 'Source/ModEntry.cs'), $code, $utf8)
    [IO.Directory]::Move($temporary, $destination)
} finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}

Write-Host "Created $ProjectName in $destination"
Write-Host "UUID $uuid"
Write-Host 'Keep this UUID and assembly name when updating or publishing the same work.'
Write-Host "Build with Tools/Build-CodeMod.ps1 -ProjectPath '$destination/Source/Mod.csproj'."

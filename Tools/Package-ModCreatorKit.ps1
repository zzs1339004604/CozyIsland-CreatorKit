<#
.SYNOPSIS
Creates a shareable creator kit ZIP without locally extracted game references or build caches.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameDirectory,
    [string]$SteamBuildId,
    [string]$OutputPath,
    [switch]$Rosetta
)
$ErrorActionPreference = 'Stop'
$kitRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'PlayerLayout.ps1')
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path (Split-Path -Parent $kitRoot) 'Artifacts/CozyIsland-ModCreatorKit-Foundation.zip'
}
$archivePath = Resolve-CozyFilePath $OutputPath
if ([IO.Path]::GetExtension($archivePath) -ne '.zip') { throw 'OutputPath must end with .zip.' }
if ($archivePath.StartsWith($kitRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Create the ZIP outside the creator kit directory so it cannot include itself.'
}
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($archivePath)) | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$excludedSegments = @('References', 'Results', 'ResearchArchive', 'MyMods', 'bin', 'obj', '.git', '.vs', '.idea', '__pycache__', 'EvaluationSDK')
$unityProjectRoots = @{}
function Test-UnityProjectRoot([string]$ProjectRoot) {
    if (-not $unityProjectRoots.ContainsKey($ProjectRoot)) {
        $unityProjectRoots[$ProjectRoot] = (Test-Path -LiteralPath (Join-Path $ProjectRoot 'Assets') -PathType Container) -and
            (Test-Path -LiteralPath (Join-Path $ProjectRoot 'ProjectSettings') -PathType Container)
    }
    return $unityProjectRoots[$ProjectRoot]
}
function Test-UnityGeneratedFile([string[]]$Segments) {
    for ($i = 0; $i -lt $Segments.Length - 1; $i++) {
        if ($Segments[$i] -notin @('Library', 'Temp', 'Logs', 'UserSettings')) { continue }
        $projectRoot = if ($i -eq 0) { $kitRoot } else { Join-Path $kitRoot ($Segments[0..($i - 1)] -join [IO.Path]::DirectorySeparatorChar) }
        if (Test-UnityProjectRoot $projectRoot) { return $true }
    }
    if ([IO.Path]::GetExtension($Segments[-1]) -in @('.csproj', '.sln', '.slnx', '.vsconfig')) {
        $parent = if ($Segments.Length -eq 1) { $kitRoot } else {
            Join-Path $kitRoot ($Segments[0..($Segments.Length - 2)] -join [IO.Path]::DirectorySeparatorChar)
        }
        return (Test-UnityProjectRoot $parent)
    }
    return $false
}
$files = @(Get-ChildItem -LiteralPath $kitRoot -File -Recurse -Force | Where-Object {
    $relative = $_.FullName.Substring($kitRoot.Length + 1)
    $segments = $relative -split '[\\/]'
    $excluded = @($segments | Where-Object { $_ -in $excludedSegments }).Count -gt 0
    $builtExample = $segments.Length -gt 3 -and $segments[0] -eq 'Examples' -and $segments[2] -eq 'Code'
    -not $excluded -and -not $builtExample -and -not (Test-UnityGeneratedFile $segments) -and $relative -ne 'PlayerIdentity.json' -and $_.Extension -notin @('.user', '.suo', '.zip', '.pdb', '.log', '.tmp', '.bak', '.cache', '.pyc') -and -not $_.Name.EndsWith('~')
} | Sort-Object FullName)
# Check against the complete Player's metadata so renaming a shipped DLL cannot bypass the inventory.
$player = Get-CozyPlayer $GameDirectory
$sharedAssemblies = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($file in Get-ChildItem -LiteralPath $player.managed -File -Filter '*.dll') {
    $sharedAssemblies.Add([Reflection.AssemblyName]::GetAssemblyName($file.FullName).Name) | Out-Null
}
$sharedAssemblyPattern = '^(0Harmony|MonoMod.*|Mono\.Cecil.*|Assembly-CSharp.*|UnityEngine.*|UnityEditor.*|Unity\..*|mscorlib|netstandard|System(?:\..*)?|Mirror(?:\..*)?|Microsoft\.CSharp|Mono\.Security|Newtonsoft\.Json)$'
foreach ($file in $files) {
    if (($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Remove the linked file before packaging: $($file.FullName)"
    }
    if ($file.Extension -ne '.dll') { continue }
    if ($file.BaseName -match $sharedAssemblyPattern -or $sharedAssemblies.Contains($file.BaseName)) {
        throw "Remove the bundled game/framework DLL before packaging: $($file.FullName)"
    }
    try { $identity = [Reflection.AssemblyName]::GetAssemblyName($file.FullName) }
    catch { throw "The creator kit contains an unreadable managed DLL: $($file.FullName). $($_.Exception.Message)" }
    if ($identity.Name -match $sharedAssemblyPattern -or $sharedAssemblies.Contains($identity.Name)) {
        throw "Remove the bundled game/framework assembly '$($identity.Name)' before packaging: $($file.FullName)"
    }
}
$compatibility = & (Join-Path $PSScriptRoot 'Test-GameCompatibility.ps1') -GameDirectory $GameDirectory -Rosetta:$Rosetta
if ($compatibility.pairing -ne 'exact') {
    throw 'Use a pinned Player from PlayerIdentity.json to package Foundation. A capability pass alone cannot identify a release build.'
}
if ($compatibility.profile.purpose -notin @('release', 'release-candidate')) {
    throw 'Use a release or release-candidate Player profile to package Foundation.'
}
if (-not [string]::IsNullOrWhiteSpace($SteamBuildId) -and $SteamBuildId -ne $compatibility.profile.steamBuildId) {
    throw 'SteamBuildId does not match the pinned profile in PlayerIdentity.json.'
}
$identityPath = Join-Path $kitRoot 'PlayerIdentity.json'
$temporary = $archivePath + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
try {
    $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew)
    try {
        $archive = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $identityPath, 'CozyIsland-ModCreatorKit/PlayerIdentity.json') | Out-Null
            foreach ($file in $files) {
                $relative = $file.FullName.Substring($kitRoot.Length + 1).Replace('\', '/')
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, 'CozyIsland-ModCreatorKit/' + $relative, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
            }
        } finally { $archive.Dispose() }
    } finally { $stream.Dispose() }
    if (Test-Path -LiteralPath $archivePath -PathType Leaf) { [IO.File]::Replace($temporary, $archivePath, [NullString]::Value) }
    else { [IO.File]::Move($temporary, $archivePath) }
} finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
}
Write-Host "Packaged Foundation profile, $($files.Count + 1) files: $archivePath"
Get-FileHash -LiteralPath $archivePath -Algorithm SHA256

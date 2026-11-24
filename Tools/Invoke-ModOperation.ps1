# Internal shared implementation for Validate-Mod.ps1 and Publish-Mod.ps1.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameExe,
    [Parameter(Mandatory = $true)][string]$ModDirectory,
    [Parameter(Mandatory = $true)][ValidateSet('validate', 'upload')][string]$Operation,
    [ValidateSet('private', 'public')][string]$Visibility,
    [string]$ResultPath,
    [ValidateRange(30, 3600)][int]$TimeoutSeconds = 750,
    [switch]$Rosetta
)
$ErrorActionPreference = 'Stop'

function Get-CozyPlayer([string]$GamePath) {
    $item = Get-Item -LiteralPath $GamePath
    if ($item.PSIsContainer -and $item.Extension -eq '.app') {
        if (-not $IsMacOS) { throw 'A macOS .app must be used on macOS.' }
        $platform = 'macOS'
        $contents = Join-Path $item.FullName 'Contents'
        $plist = Join-Path $contents 'Info.plist'
        $exeName = & /usr/bin/plutil -extract CFBundleExecutable raw -o - $plist
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($exeName) -or $exeName -ne [IO.Path]::GetFileName($exeName)) {
            throw 'Could not determine the executable from the macOS Player Info.plist.'
        }
        $exe = Join-Path $contents "MacOS/$exeName"
        $managed = Join-Path $contents 'Resources/Data/Managed'
        $workingDirectory = $item.Parent.FullName
    } else {
        if (-not $IsWindows) { throw 'Run the Windows Player on Windows.' }
        $platform = 'Windows'
        if ($item.PSIsContainer) {
            $candidates = @(Get-ChildItem -LiteralPath $item.FullName -File -Filter '*.exe' | Where-Object {
                Test-Path -LiteralPath (Join-Path $item.FullName "$($_.BaseName)_Data/Managed/Assembly-CSharp.dll") -PathType Leaf
            })
            if ($candidates.Count -ne 1) { throw 'Choose a Windows Player folder with one executable and matching *_Data/Managed directory.' }
            $item = $candidates[0]
        } elseif ($item.Extension -ne '.exe') {
            throw 'Specify a complete Windows Player folder or executable, or a macOS .app.'
        }
        $exe = $item.FullName
        $workingDirectory = $item.DirectoryName
        $managed = Join-Path $workingDirectory "$($item.BaseName)_Data/Managed"
        if (-not (Test-Path -LiteralPath (Join-Path $workingDirectory 'UnityPlayer.dll') -PathType Leaf)) {
            throw 'The Windows Player is incomplete: missing UnityPlayer.dll.'
        }
    }
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'The Player executable is missing.' }
    foreach ($name in @('Assembly-CSharp.dll', 'Mirror.dll', '0Harmony.dll', 'UnityEngine.CoreModule.dll', 'mscorlib.dll', 'netstandard.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $managed $name) -PathType Leaf)) {
            throw "The complete Workshop-enabled Mono Player is required. Missing assembly: $name."
        }
    }
    [pscustomobject]@{
        platform = $platform
        executable = $exe
        workingDirectory = $workingDirectory
    }
}

$player = Get-CozyPlayer $GameExe
if ($Rosetta -and $player.platform -ne 'macOS') { throw '-Rosetta applies only to a macOS Player.' }
$modRoot = (Resolve-Path -LiteralPath $ModDirectory).ProviderPath
if (-not (Test-Path -LiteralPath (Join-Path $modRoot 'mod.json') -PathType Leaf)) { throw 'ModDirectory must contain mod.json.' }
$processName = [IO.Path]::GetFileNameWithoutExtension($player.executable)
$alreadyRunning = @(Get-Process -Name $processName -ErrorAction SilentlyContinue | Where-Object {
    try { $_.Path -eq $player.executable } catch { $false }
})
if ($alreadyRunning.Count -gt 0) { throw 'Close this game instance before starting an author command; keep the Steam client running.' }
if ([string]::IsNullOrWhiteSpace($ResultPath)) {
    $resultName = $Operation + '-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8) + '.json'
    $ResultPath = Join-Path (Split-Path -Parent $PSScriptRoot) "Results/$resultName"
}
$resultFile = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ResultPath)
if (Test-Path -LiteralPath $resultFile) { throw 'ResultPath already exists. Choose a new result file so an old result cannot be mistaken for this run.' }
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resultFile)) | Out-Null
$logFile = [IO.Path]::ChangeExtension($resultFile, '.log')

$gameArguments = @('--disable-mods', ('--mod-' + $Operation), $modRoot, '--mod-result', $resultFile, '--mod-exit', '-logFile', $logFile)
if ($Operation -eq 'upload' -and $PSBoundParameters.ContainsKey('Visibility')) {
    $gameArguments += @('--mod-visibility', $Visibility)
}

$start = [Diagnostics.ProcessStartInfo]::new()
$start.FileName = $player.executable
$start.WorkingDirectory = $player.workingDirectory
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
# Keep Steam's app context local to the child and launch this exact Player for the author's command.
$start.EnvironmentVariables['SteamAppId'] = '2918500'
$start.EnvironmentVariables['SteamGameId'] = '2918500'
if ($player.platform -eq 'macOS') {
    $gameArguments += @('-screen-fullscreen', '0', '-screen-width', '960', '-screen-height', '540')
    $start.FileName = '/usr/bin/arch'
    # Prefer the native arm64 slice of a Universal Player even if PowerShell runs under Rosetta.
    $architectures = if ($Rosetta) { @('-x86_64') } else { @('-arm64', '-x86_64') }
    $gameArguments = @($architectures) + @($player.executable) + $gameArguments
}
foreach ($argument in $gameArguments) { $start.ArgumentList.Add($argument) }

Write-Host "Starting $Operation with $($player.executable); result: $resultFile"
$process = [Diagnostics.Process]::new()
$process.StartInfo = $start
try {
    if (-not $process.Start()) { throw 'Could not start the supplied Player.' }
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        throw "Timed out waiting for game process $($process.Id). The process remains running; inspect $logFile and $resultFile before retrying."
    }
    if (-not (Test-Path -LiteralPath $resultFile)) {
        throw "The supplied Player exited ($($process.ExitCode)) without a result. Check $logFile, the Steam client/login and the Player architecture. This command requires a Workshop-enabled game build."
    }
    $report = Get-Content -LiteralPath $resultFile -Raw | ConvertFrom-Json
    if (-not $report.success) {
        $reason = if ($report.error) { $report.error } elseif ($report.result) { $report.result } else { 'Unknown game-side error' }
        throw "$Operation failed: $reason. Full result: $resultFile"
    }
    if ($report.operation -ne $Operation -or $process.ExitCode -ne 0) { throw "Unexpected result from the supplied Player: $resultFile" }
    if ($Operation -eq 'upload') {
        if ($report.result -ne 'OK') { throw "Steam did not report a successful upload: $resultFile" }
        Write-Host "Uploaded Workshop item $($report.workshopId): https://steamcommunity.com/sharedfiles/filedetails/?id=$($report.workshopId)"
        if ($report.needsWorkshopAgreement) { Write-Warning 'Steam requires the creator to accept the Workshop agreement on the item page.' }
    } else { Write-Host "Validated $($report.name) ($($report.uuid)), $($report.files) packaged files." }
    Write-Output $report
} finally { $process.Dispose() }

# Internal shared implementation for Validate-Mod.ps1 and Publish-Mod.ps1.
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
. (Join-Path $PSScriptRoot 'PlayerLayout.ps1')
$player = Get-CozyPlayer $GameExe
if (($player.platform -eq 'macOS' -and -not $IsMacOS) -or
    ($player.platform -eq 'Windows' -and [Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT)) {
    throw "Run the supplied $($player.platform) Player on its own platform."
}
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
$resultFile = Resolve-CozyFilePath $ResultPath
if (Test-Path -LiteralPath $resultFile) { throw 'ResultPath already exists. Choose a new result file so an old result cannot be mistaken for this run.' }
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resultFile)) | Out-Null
$logFile = [IO.Path]::ChangeExtension($resultFile, '.log')

function ConvertTo-WindowsArgument([string]$Value) {
    # Quote using CommandLineToArgvW/CRT escaping on Windows PowerShell 5.1.
    $escaped = [Regex]::Replace($Value, '(\\*)"', '$1$1\"')
    $escaped = [Regex]::Replace($escaped, '(\\+)$', '$1$1')
    '"' + $escaped + '"'
}
$gameArguments = @('--disable-mods', ('--mod-' + $Operation), $modRoot, '--mod-result', $resultFile, '--mod-exit', '-logFile', $logFile)
if ($Operation -eq 'upload' -and $PSBoundParameters.ContainsKey('Visibility')) {
    $gameArguments += @('--mod-visibility', $Visibility)
}

$start = New-Object Diagnostics.ProcessStartInfo
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
if ($null -ne $start.PSObject.Properties['ArgumentList']) {
    foreach ($argument in $gameArguments) { $start.ArgumentList.Add($argument) }
} else {
    $start.Arguments = ($gameArguments | ForEach-Object { ConvertTo-WindowsArgument $_ }) -join ' '
}

Write-Host "Starting $Operation with $($player.executable); result: $resultFile"
$process = New-Object Diagnostics.Process
$process.StartInfo = $start
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
try {
    if (-not $process.Start()) { throw 'Could not start the supplied Player.' }
    while (-not $process.HasExited) {
        if ([DateTime]::UtcNow -ge $deadline) {
            throw "Timed out waiting for game process $($process.Id). The process remains running; inspect $logFile and $resultFile before retrying."
        }
        Start-Sleep -Milliseconds 250
        $process.Refresh()
    }
    $process.WaitForExit()
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

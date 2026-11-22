# Shared path and fingerprint handling for the creator tools.
function Resolve-CozyFilePath([string]$Path) {
    $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

function Get-CozyPlayer([string]$GamePath) {
    $resolved = (Resolve-Path -LiteralPath $GamePath).ProviderPath
    $item = Get-Item -LiteralPath $resolved
    if ($item.PSIsContainer -and $item.Extension -eq '.app') {
        if (-not $IsMacOS) { throw 'A macOS .app must be used on macOS.' }
        $platform = 'macOS'
        $contents = Join-Path $resolved 'Contents'
        $plist = Join-Path $contents 'Info.plist'
        $exeName = & /usr/bin/plutil -extract CFBundleExecutable raw -o - $plist
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($exeName) -or $exeName -ne [IO.Path]::GetFileName($exeName)) {
            throw 'Could not determine the executable from the macOS Player Info.plist.'
        }
        $exe = Join-Path $contents "MacOS/$exeName"
        $managed = Join-Path $contents 'Resources/Data/Managed'
        $workingDirectory = [IO.Path]::GetDirectoryName($resolved)
    } else {
        $platform = 'Windows'
        if ($item.PSIsContainer) {
            $workingDirectory = $resolved
            $candidates = @(Get-ChildItem -LiteralPath $resolved -File -Filter '*.exe' | Where-Object {
                Test-Path -LiteralPath (Join-Path $resolved "$($_.BaseName)_Data/Managed/Assembly-CSharp.dll") -PathType Leaf
            })
            if ($candidates.Count -ne 1) { throw 'Choose a Windows Player folder with one executable and matching *_Data/Managed directory.' }
            $exe = $candidates[0].FullName
        } elseif ($item.Extension -eq '.exe') {
            $exe = $resolved
            $workingDirectory = $item.DirectoryName
        } else {
            throw 'Specify a complete Windows Player folder or executable, or a macOS .app.'
        }
        $exeName = [IO.Path]::GetFileNameWithoutExtension($exe)
        $managed = Join-Path $workingDirectory "${exeName}_Data/Managed"
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
        path = $resolved
        executable = $exe
        managed = $managed
        workingDirectory = $workingDirectory
    }
}

function Get-CozyPlayerFingerprint($Player) {
    $names = @('Assembly-CSharp.dll', 'Mirror.dll', '0Harmony.dll', 'UnityEngine.CoreModule.dll')
    $runtimeDependencies = @(
        'Mono.Cecil.dll', 'Mono.Cecil.Mdb.dll', 'Mono.Cecil.Pdb.dll', 'Mono.Cecil.Rocks.dll',
        'MonoMod.Backports.dll', 'MonoMod.Core.dll', 'MonoMod.Iced.dll',
        'MonoMod.ILHelpers.dll', 'MonoMod.RuntimeDetour.dll', 'MonoMod.Utils.dll', 'System.ValueTuple.dll'
    )
    foreach ($name in $runtimeDependencies) {
        if (Test-Path -LiteralPath (Join-Path $Player.managed $name) -PathType Leaf) { $names += $name }
    }
    foreach ($name in $names) {
        [pscustomobject]@{
            file = $name
            sha256 = (Get-FileHash -LiteralPath (Join-Path $Player.managed $name) -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
}

function Find-CozyPlayerPairing($Player, $Identity, $Fingerprint) {
    $candidates = @($Identity.players | Where-Object { $_.platform -eq $Player.platform })
    foreach ($candidate in $candidates) {
        if (@($candidate.files).Count -ne $Fingerprint.Count) { continue }
        $allMatch = $true
        foreach ($actual in $Fingerprint) {
            $expected = @($candidate.files | Where-Object { $_.file -eq $actual.file })
            if ($expected.Count -ne 1 -or $expected[0].sha256 -ne $actual.sha256) {
                $allMatch = $false
                break
            }
        }
        if ($allMatch -and $candidate.executableSha256) {
            $allMatch = (Get-FileHash -LiteralPath $Player.executable -Algorithm SHA256).Hash -eq $candidate.executableSha256
        }
        if ($allMatch) { return $candidate }
    }
    return $null
}

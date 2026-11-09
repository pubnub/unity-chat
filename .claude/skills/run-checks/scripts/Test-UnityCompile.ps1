<#
.SYNOPSIS
    Opens the Unity Chat project in batch mode, lets Unity compile its scripts and lists compiler errors.

.DESCRIPTION
    The .NET build covers only the C# Chat library. These compile only inside Unity:
      - the copy of the library in Assets/PubnubChat/Runtime/PubnubChatApi, built against the com.pubnub.sdk package
      - the Unity-only files in Assets/PubnubChat/Runtime (UnityChat, UnityListenerFactory, UnityChatPNSDKSource)
      - the docs snippets in Assets/Snippets

    Samples~ folders are never compiled by the project, so this doesn't cover them.

    The editor version comes from ProjectSettings/ProjectVersion.txt. Unity refuses to open a project that is already
    open in another editor, so close it first. The first run after a package or editor change re-imports the
    project and can take several minutes. Exits with 1 on compiler errors or when Unity fails to start.

.PARAMETER UnityPath
    Path to the Unity executable. Defaults to the Unity Hub install location of the project's editor version.

.PARAMETER LogFile
    Where Unity writes its log. Defaults to unity-chat-compile.log in the temp folder.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .claude/skills/run-checks/scripts/Test-UnityCompile.ps1
    pwsh -File .claude/skills/run-checks/scripts/Test-UnityCompile.ps1 -UnityPath /Applications/Unity/Hub/Editor/2022.3.57f1/Unity.app/Contents/MacOS/Unity
#>
[CmdletBinding()]
param(
    [string]$UnityPath,
    [string]$LogFile = (Join-Path ([System.IO.Path]::GetTempPath()) 'unity-chat-compile.log')
)

$ErrorActionPreference = 'Stop'
$repoRoot = (git rev-parse --show-toplevel).Trim()
$projectPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'unity-chat/PubnubChatUnity'))

$versionLine = Select-String -LiteralPath (Join-Path $projectPath 'ProjectSettings/ProjectVersion.txt') -Pattern '^m_EditorVersion:\s*(\S+)'
$editorVersion = $versionLine.Matches[0].Groups[1].Value

if (-not $UnityPath) {
    $onWindows = ($PSVersionTable.PSEdition -eq 'Desktop') -or $IsWindows
    $UnityPath = if ($onWindows) {
        Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
    } elseif ($IsMacOS) {
        "/Applications/Unity/Hub/Editor/$editorVersion/Unity.app/Contents/MacOS/Unity"
    } else {
        Join-Path $HOME "Unity/Hub/Editor/$editorVersion/Editor/Unity"
    }
}
if (-not (Test-Path -LiteralPath $UnityPath)) {
    Write-Host "FAIL: Unity $editorVersion not found at $UnityPath; pass -UnityPath" -ForegroundColor Red
    exit 1
}

if (Test-Path -LiteralPath $LogFile) { Remove-Item -LiteralPath $LogFile -Force }
Write-Host "Compiling $projectPath with Unity $editorVersion (log: $LogFile)"
$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $UnityPath
$startInfo.Arguments = "-batchmode -nographics -quit -projectPath `"$projectPath`" -logFile `"$LogFile`""
$startInfo.UseShellExecute = $false
# Start-Process -Wait would also wait for the compiler server Unity leaves running for about 10 minutes.
$process = [System.Diagnostics.Process]::Start($startInfo)
$process.WaitForExit()
$log = if (Test-Path -LiteralPath $LogFile) { @(Get-Content -LiteralPath $LogFile) } else { @() }

$errors = @($log | Where-Object { $_ -match 'error CS\d{4}' } | ForEach-Object { $_.Trim() } | Sort-Object -Unique)
if ($errors.Count -gt 0) {
    Write-Host "FAIL: $($errors.Count) compiler error(s):" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}
if ($log -match 'another Unity instance is running') {
    Write-Host "FAIL: the project is open in another Unity editor; close it and run again" -ForegroundColor Red
    exit 1
}
if ($process.ExitCode -ne 0) {
    Write-Host "FAIL: Unity exited with code $($process.ExitCode) without compiler errors in the log; its last lines:" -ForegroundColor Red
    $log | Select-Object -Last 25 | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Write-Host "OK: the Unity project compiles"
exit 0

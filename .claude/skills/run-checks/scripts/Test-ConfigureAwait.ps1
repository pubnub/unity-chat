<#
.SYNOPSIS
    Lists `await` statements in the C# Chat library that do not use .ConfigureAwait(false).

.DESCRIPTION
    Every await in c-sharp-chat/PubnubChatApi/PubnubChatApi should end with .ConfigureAwait(false). The library runs
    inside Unity, whose main thread has a synchronization context: resuming on it slows the game loop, and
    deadlocks callers that block on Chat tasks.

    The library still has older awaits without it, so by default only awaits on lines added or changed against
    the merge base with origin/master (plus untracked files) are reported. Pass -All to list every violation in
    the library. Exits with 1 when a reported violation is found.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .claude/skills/run-checks/scripts/Test-ConfigureAwait.ps1
    pwsh -File .claude/skills/run-checks/scripts/Test-ConfigureAwait.ps1 -All
#>
[CmdletBinding()]
param(
    [switch]$All,
    [string]$Base = 'origin/master'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

$library = 'c-sharp-chat/PubnubChatApi/PubnubChatApi'

# Maps each file to scan to the set of line numbers to check ($null means every line).
$scope = @{}
if ($All) {
    foreach ($file in @(git ls-files --cached --others --exclude-standard -- "$library/*.cs")) { $scope[$file] = $null }
} else {
    $mergeBase = (git merge-base $Base HEAD 2>$null)
    if (-not $mergeBase) { $mergeBase = 'HEAD' }
    foreach ($file in @(git ls-files --others --exclude-standard -- "$library/*.cs")) { $scope[$file] = $null }
    foreach ($file in @(git diff --name-only --diff-filter=d $mergeBase -- "$library/*.cs")) {
        $lines = New-Object 'System.Collections.Generic.HashSet[int]'
        foreach ($hunk in @(git diff -U0 $mergeBase -- $file | Select-String '^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@')) {
            $start = [int]$hunk.Matches[0].Groups[1].Value
            $count = if ($hunk.Matches[0].Groups[2].Success) { [int]$hunk.Matches[0].Groups[2].Value } else { 1 }
            for ($n = $start; $n -lt $start + $count; $n++) { [void]$lines.Add($n) }
        }
        if ($lines.Count -gt 0) { $scope[$file] = $lines }
    }
}

$violations = @()
$fileCount = 0
foreach ($file in @($scope.Keys | Where-Object { $_ -notmatch '/(bin|obj)/' -and (Test-Path -LiteralPath $_) } | Sort-Object)) {
    $fileCount++
    $changedLines = $scope[$file]
    $lines = @(Get-Content -LiteralPath $file)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch '\bawait\s' -or $lines[$i] -match '^\s*(//|\*|/\*)') { continue }
        # An await statement may span several lines; read up to its terminating semicolon.
        $statement = ''
        $touched = $null -eq $changedLines
        for ($j = $i; $j -lt [Math]::Min($i + 12, $lines.Count); $j++) {
            $statement += $lines[$j]
            if (-not $touched -and $changedLines.Contains($j + 1)) { $touched = $true }
            if ($lines[$j] -match ';\s*(//.*)?$') { break }
        }
        if ($touched -and $statement -notmatch 'ConfigureAwait\(false\)') {
            $violations += "${file}:$($i + 1): $($lines[$i].Trim())"
        }
    }
}

$description = if ($All) { 'the whole library' } else { "lines changed against $Base" }
if ($violations.Count -eq 0) {
    Write-Host "OK: every await uses .ConfigureAwait(false) ($fileCount file(s) scanned: $description)"
    exit 0
}

Write-Host "FAIL: $($violations.Count) await(s) without .ConfigureAwait(false) ($fileCount file(s) scanned: $description):" -ForegroundColor Red
$violations | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
exit 1

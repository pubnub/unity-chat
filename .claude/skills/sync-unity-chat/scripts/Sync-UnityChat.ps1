<#
.SYNOPSIS
    Copies the C# Chat library sources into the Unity Chat package and maintains their .meta files.

.DESCRIPTION
    c-sharp-chat/PubnubChatApi/PubnubChatApi is the source of truth for the Chat logic. Unity compiles a copy of
    its .cs files from unity-chat/PubnubChatUnity/Assets/PubnubChat/Runtime/PubnubChatApi, because the package must
    build against the com.pubnub.sdk package rather than a prebuilt Chat DLL.

    The script mirrors every .cs file of the library (bin/ and obj/ excluded):
      - copies new and changed files byte for byte
      - deletes copies whose source no longer exists, together with their .meta files and emptied folders
      - creates a .meta file with a new GUID for every new file and folder; existing .meta files are never rewritten
      - deletes .meta files whose asset no longer exists

    Unity ignores assets without a .meta file when the package is installed from git, so a missing .meta breaks
    customers even though the project opens fine locally. Contents are compared with line endings normalized,
    so checkouts with different core.autocrlf settings don't show as drift.

    -Check only reports. It exits with 1 when the copy differs from the library or a .meta file is missing or
    orphaned.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .claude/skills/sync-unity-chat/scripts/Sync-UnityChat.ps1
    pwsh -File .claude/skills/sync-unity-chat/scripts/Sync-UnityChat.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

$sourceRel = 'c-sharp-chat/PubnubChatApi/PubnubChatApi'
$targetRel = 'unity-chat/PubnubChatUnity/Assets/PubnubChat/Runtime/PubnubChatApi'
$sourceRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $sourceRel))
$targetRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $targetRel))

$scriptMetaTemplate = "fileFormatVersion: 2`nguid: {0}`nMonoImporter:`n  externalObjects: {{}}`n  serializedVersion: 2`n  defaultReferences: []`n  executionOrder: 0`n  icon: {{instanceID: 0}}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$folderMetaTemplate = "fileFormatVersion: 2`nguid: {0}`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {{}}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"

function Get-RelativeFiles([string]$Root, [string]$Filter) {
    if (-not (Test-Path -LiteralPath $Root)) { return @() }
    Get-ChildItem -LiteralPath $Root -Recurse -File -Filter $Filter |
        ForEach-Object { $_.FullName.Substring($Root.Length + 1).Replace('\', '/') } |
        Where-Object { $_ -notmatch '(^|/)(bin|obj)/' } |
        Sort-Object
}

function Get-RelativeFolders([string]$Root) {
    if (-not (Test-Path -LiteralPath $Root)) { return @() }
    Get-ChildItem -LiteralPath $Root -Recurse -Directory |
        ForEach-Object { $_.FullName.Substring($Root.Length + 1).Replace('\', '/') } |
        Where-Object { $_ -notmatch '(^|/)(bin|obj)(/|$)' } |
        Sort-Object
}

function Read-Normalized([string]$Path) {
    [System.IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
}

function New-MetaFile([string]$Path, [string]$Template) {
    $guid = [guid]::NewGuid().ToString('N')
    [System.IO.File]::WriteAllText($Path, ($Template -f $guid))
}

function Get-TargetPath([string]$Relative) {
    Join-Path $targetRoot $Relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

$sources = @(Get-RelativeFiles $sourceRoot '*.cs')
if ($sources.Count -eq 0) { throw "No .cs files found under $sourceRel" }
$targets = @(Get-RelativeFiles $targetRoot '*.cs')

$added = @($sources | Where-Object { $targets -notcontains $_ })
$removed = @($targets | Where-Object { $sources -notcontains $_ })
$changed = @($sources | Where-Object { $targets -contains $_ } | Where-Object {
    (Read-Normalized (Join-Path $sourceRoot $_)) -ne (Read-Normalized (Get-TargetPath $_))
})

$removedFolders = @()
if (-not $Check) {
    foreach ($file in @($added + $changed)) {
        $destination = Get-TargetPath $file
        $folder = Split-Path -Parent $destination
        if (-not (Test-Path -LiteralPath $folder)) { New-Item -ItemType Directory -Path $folder -Force | Out-Null }
        Copy-Item -LiteralPath (Join-Path $sourceRoot $file) -Destination $destination -Force
    }
    foreach ($file in $removed) {
        $path = Get-TargetPath $file
        Remove-Item -LiteralPath $path -Force
        if (Test-Path -LiteralPath "$path.meta") { Remove-Item -LiteralPath "$path.meta" -Force }
    }
    # Deepest folders first, so that a parent emptied by the removal of its children goes too.
    foreach ($folder in @(Get-RelativeFolders $targetRoot | Sort-Object { $_.Length } -Descending)) {
        $path = Get-TargetPath $folder
        if (@(Get-ChildItem -LiteralPath $path -Force).Count -eq 0) {
            Remove-Item -LiteralPath $path -Force
            if (Test-Path -LiteralPath "$path.meta") { Remove-Item -LiteralPath "$path.meta" -Force }
            $removedFolders += "$folder/"
        }
    }
}

# .meta files: every file and folder under the target, including the target folder itself, needs one.
$missingMeta = @()
$createdMeta = @()
$assets = @()
if (Test-Path -LiteralPath $targetRoot) {
    $assets += [pscustomobject]@{ Path = $targetRoot; Display = $targetRel; Template = $folderMetaTemplate }
    foreach ($folder in @(Get-RelativeFolders $targetRoot)) {
        $assets += [pscustomobject]@{ Path = (Get-TargetPath $folder); Display = "$targetRel/$folder"; Template = $folderMetaTemplate }
    }
    foreach ($file in @(Get-RelativeFiles $targetRoot '*' | Where-Object { $_ -notmatch '\.meta$' })) {
        $assets += [pscustomobject]@{ Path = (Get-TargetPath $file); Display = "$targetRel/$file"; Template = $scriptMetaTemplate }
    }
}
foreach ($asset in $assets) {
    if (Test-Path -LiteralPath "$($asset.Path).meta") { continue }
    if ($Check) {
        $missingMeta += $asset.Display
    } else {
        New-MetaFile "$($asset.Path).meta" $asset.Template
        $createdMeta += "$($asset.Display).meta"
    }
}

$orphanMeta = @()
foreach ($meta in @(Get-RelativeFiles $targetRoot '*.meta')) {
    $assetPath = Get-TargetPath $meta.Substring(0, $meta.Length - 5)
    if (Test-Path -LiteralPath $assetPath) { continue }
    $orphanMeta += "$targetRel/$meta"
    if (-not $Check) { Remove-Item -LiteralPath (Get-TargetPath $meta) -Force }
}

function Write-List([string]$Label, [string[]]$Items, [string]$Color) {
    foreach ($item in $Items) { Write-Host ("  {0,-9} {1}" -f $Label, $item) -ForegroundColor $Color }
}

Write-Host "$sourceRel -> $targetRel ($($sources.Count) source files)"
if ($Check) {
    Write-List 'MISSING' $added Red
    Write-List 'DIFFERS' $changed Red
    Write-List 'EXTRA' $removed Red
    Write-List 'NO META' $missingMeta Red
    Write-List 'ORPHAN' $orphanMeta Red
    $drift = $added.Count + $changed.Count + $removed.Count + $missingMeta.Count + $orphanMeta.Count
    if ($drift -gt 0) {
        Write-Host "FAIL: the Unity copy is out of sync; run the script without -Check" -ForegroundColor Red
        exit 1
    }
    Write-Host "OK: the Unity copy matches the C# Chat library and every asset has a .meta file"
    exit 0
}

Write-List 'added' $added Green
Write-List 'updated' $changed Green
Write-List 'deleted' @($removed + $removedFolders) Yellow
Write-List 'new meta' $createdMeta Green
Write-List 'del meta' $orphanMeta Yellow
if (($added.Count + $changed.Count + $removed.Count + $createdMeta.Count + $orphanMeta.Count) -eq 0) {
    Write-Host "OK: already in sync"
} else {
    Write-Host "Synced. Commit the copies and .meta files together with the C# Chat change."
}
exit 0

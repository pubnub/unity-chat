<#
.SYNOPSIS
    Compares the C# Chat PubnubChatConfig constructor with the Unity PubnubChatConfigAsset ScriptableObject.

.DESCRIPTION
    PubnubChatConfigAsset (unity-chat/.../Samples~/PubnubChatConfigAsset) lets Unity users edit a PubnubChatConfig
    in the inspector and converts to it with an implicit operator. Samples~ is never compiled by the Unity project
    and the .NET build can't see it, so a config change that isn't mirrored there only fails in customer projects.

    Asset members are the public members marked [SerializeField] (the file uses [field: SerializeField]
    auto-properties). Parses both files and reports:
      FAIL  constructor parameter with no asset member of the same name in PascalCase
      FAIL  asset member that the implicit operator never reads
      FAIL  named argument in the operator that the constructor doesn't have (a compile error in Unity)
      WARN  asset default that differs from the constructor default (parameters defaulting to null are skipped)

    Exits with 1 when any FAIL is reported.
#>
[CmdletBinding()]
param(
    [string]$ConfigFile = 'c-sharp-chat/PubnubChatApi/PubnubChatApi/Entities/Data/PubnubChatConfig.cs',
    [string]$AssetFile = 'unity-chat/PubnubChatUnity/Assets/PubnubChat/Samples~/PubnubChatConfigAsset/PubnubChatConfigAsset.cs'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

# Constructor parameters deliberately not exposed by the asset.
$notExposedOnPurpose = @()

function Split-TopLevel([string]$Text) {
    $parts = @()
    $depth = 0
    $current = ''
    foreach ($char in $Text.ToCharArray()) {
        if ('<([{'.Contains($char)) { $depth++ }
        elseif ('>)]}'.Contains($char)) { $depth-- }
        if ($char -eq ',' -and $depth -eq 0) { $parts += $current; $current = ''; continue }
        $current += $char
    }
    if ($current.Trim()) { $parts += $current }
    $parts | ForEach-Object { $_.Trim() }
}

function ConvertTo-Pascal([string]$Name) { $Name.Substring(0, 1).ToUpperInvariant() + $Name.Substring(1) }

# --- C# side -------------------------------------------------------------------------------------
$configSource = Get-Content -Raw $ConfigFile
$ctor = [regex]::Match($configSource, 'public\s+PubnubChatConfig\s*\((?<params>[^)]*)\)')
if (-not $ctor.Success) { throw "Couldn't find the PubnubChatConfig constructor in $ConfigFile" }
$parameters = @()
foreach ($part in (Split-TopLevel $ctor.Groups['params'].Value)) {
    $match = [regex]::Match($part, '^(?<type>.+?)\s+(?<name>\w+)\s*(=\s*(?<default>.+))?$', 'Singleline')
    $parameters += [pscustomobject]@{
        Name    = $match.Groups['name'].Value
        Type    = $match.Groups['type'].Value.Trim()
        Default = if ($match.Groups['default'].Success) { $match.Groups['default'].Value.Trim() } else { $null }
    }
}

# --- Unity side ----------------------------------------------------------------------------------
$assetSource = Get-Content -Raw $AssetFile
$members = @{}
$memberPattern = '(?m)^\s*(?<attributes>(\[[^\]]*\]\s*)+)public\s+(?<type>[\w\.<>,\s]+?)\s+(?<name>\w+)(\s*\{[^}]*\})?(\s*=\s*(?<init>[^;]+))?;?'
foreach ($match in [regex]::Matches($assetSource, $memberPattern)) {
    if ($match.Groups['attributes'].Value -notmatch 'SerializeField') { continue }
    $members[$match.Groups['name'].Value] = [pscustomobject]@{
        Type        = $match.Groups['type'].Value.Trim()
        Initializer = if ($match.Groups['init'].Success) { ($match.Groups['init'].Value -replace '\s+', ' ').Trim() } else { $null }
    }
}
$operator = [regex]::Match($assetSource, 'implicit\s+operator\s+PubnubChatConfig\s*\([^)]*\)\s*\{(?<body>.*?)\n\s*\}', 'Singleline')
if (-not $operator.Success) { throw "Couldn't find the implicit operator in $AssetFile" }
$body = $operator.Groups['body'].Value
$readMembers = @([regex]::Matches($body, 'asset\.(\w+)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
$namedArguments = @([regex]::Matches($body, '(\w+)\s*:\s*asset\.') | ForEach-Object { $_.Groups[1].Value })

function Get-TypeDefault([string]$Type) {
    switch ($Type) { 'int' { '0' } 'bool' { 'false' } 'float' { '0' } 'string' { 'null' } default { $null } }
}

# --- Report --------------------------------------------------------------------------------------
$failures = 0
Write-Host "PubnubChatConfig: $($parameters.Count) constructor parameters; PubnubChatConfigAsset: $($members.Count) serialized members, $($readMembers.Count) read by the operator"

foreach ($parameter in $parameters) {
    if ($notExposedOnPurpose -contains $parameter.Name) { continue }
    $memberName = ConvertTo-Pascal $parameter.Name
    if (-not $members.ContainsKey($memberName)) {
        Write-Host "  FAIL  parameter '$($parameter.Name)' has no asset member '$memberName'" -ForegroundColor Red; $failures++
        continue
    }
    if ($null -eq $parameter.Default -or $parameter.Default -eq 'null') { continue }
    $assetDefault = $members[$memberName].Initializer
    if ($null -eq $assetDefault) { $assetDefault = Get-TypeDefault $members[$memberName].Type }
    if ($assetDefault -ne $parameter.Default) {
        Write-Host "  WARN  default of '$memberName' differs: asset '$assetDefault', C# '$($parameter.Default)'" -ForegroundColor Yellow
    }
}
foreach ($name in ($members.Keys | Sort-Object)) {
    if ($readMembers -notcontains $name) {
        Write-Host "  FAIL  asset member '$name' isn't passed to PubnubChatConfig by the implicit operator" -ForegroundColor Red; $failures++
    }
}
$parameterNames = @($parameters | ForEach-Object { $_.Name })
foreach ($argument in $namedArguments) {
    if ($parameterNames -notcontains $argument) {
        Write-Host "  FAIL  the operator passes '${argument}:', which the PubnubChatConfig constructor doesn't have" -ForegroundColor Red; $failures++
    }
}

if ($failures -gt 0) { exit 1 }
Write-Host "OK: PubnubChatConfigAsset exposes and maps every PubnubChatConfig constructor parameter"
exit 0

param(
    [Parameter(Mandatory = $true)]
    [string]$Base,

    [Parameter(Mandatory = $true)]
    [string]$Local,

    [Parameter(Mandatory = $true)]
    [string]$Remote,

    [Parameter()]
    [string]$MarkerSize,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$PathParts
)

$ErrorActionPreference = 'Stop'

$Path = ($PathParts -join ' ')

function Resolve-UnityYAMLMerge {
    $projectRoot = Split-Path -Parent $PSScriptRoot
    $projectVersionPath = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
    $editorVersion = $null

    if (Test-Path -LiteralPath $projectVersionPath) {
        $versionLine = Get-Content -LiteralPath $projectVersionPath |
            Where-Object { $_ -like 'm_EditorVersion:*' } |
            Select-Object -First 1
        if ($versionLine) {
            $editorVersion = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()
        }
    }

    $candidates = @()
    if ($env:UNITY_YAML_MERGE) {
        $candidates += $env:UNITY_YAML_MERGE
    }
    $candidates += 'F:\Unity\Editor\Data\Tools\UnityYAMLMerge.exe'

    if ($editorVersion) {
        $candidates += Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Data\Tools\UnityYAMLMerge.exe"

        $programFilesX86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
        if ($programFilesX86) {
            $candidates += Join-Path $programFilesX86 "Unity\Hub\Editor\$editorVersion\Editor\Data\Tools\UnityYAMLMerge.exe"
        }

        $candidates += "D:\Program Files\Unity\Hub\Editor\$editorVersion\Editor\Data\Tools\UnityYAMLMerge.exe"
        $candidates += "F:\Unity\Hub\Editor\$editorVersion\Editor\Data\Tools\UnityYAMLMerge.exe"
    }

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw 'UnityYAMLMerge.exe was not found. Set UNITY_YAML_MERGE to the full UnityYAMLMerge.exe path.'
}

$unityMerge = Resolve-UnityYAMLMerge
$ext = [System.IO.Path]::GetExtension($Path)
if ([string]::IsNullOrWhiteSpace($ext)) {
    if ($Path -like '*.prefab') {
        $ext = '.prefab'
    } else {
        $ext = '.unity'
    }
}

$mergeExt = $ext
$unityYamlExtensions = @(
    '.asset',
    '.mat',
    '.anim',
    '.controller',
    '.overrideController',
    '.playable',
    '.mask',
    '.physicMaterial',
    '.physicsMaterial2D'
)
if ($unityYamlExtensions -contains $ext) {
    $mergeExt = '.unity'
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("unityyamlmerge-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null

function Copy-WithExtension {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $destination = Join-Path $tempRoot ($Name + $mergeExt)
    Copy-Item -LiteralPath $Source -Destination $destination -Force
    return $destination
}

$baseCopy = Copy-WithExtension -Source $Base -Name 'base'
$otherCopy = Copy-WithExtension -Source $Remote -Name 'other'
$localCopy = Copy-WithExtension -Source $Local -Name 'local'
$resultCopy = Join-Path $tempRoot ('result' + $ext)

Push-Location (Split-Path -Parent $unityMerge)
try {
    & $unityMerge merge -p $baseCopy $otherCopy $localCopy $resultCopy
    $exitCode = $LASTEXITCODE
    if ($exitCode -eq 0 -and (Test-Path -LiteralPath $resultCopy)) {
        Copy-Item -LiteralPath $resultCopy -Destination $Local -Force
    }
    exit $exitCode
}
finally {
    Pop-Location
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

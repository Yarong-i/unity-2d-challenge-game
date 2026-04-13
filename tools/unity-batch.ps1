param(
    [switch]$ApplySceneRequest,
    [string]$UnityPath = "",
    [string]$ProjectPath = "",
    [string]$ExecuteMethod = "CodexUnityBridge.ApplySceneRequestFromBatch",
    [string]$LogFile = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $LogFile = Join-Path $ProjectPath "Logs\codex-unity-batch.log"
}

function Find-UnityEditor {
    param([string]$RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        if (Test-Path $RequestedPath) {
            return (Resolve-Path $RequestedPath).Path
        }

        throw "Unity executable was not found at: $RequestedPath"
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EDITOR)) {
        if (Test-Path $env:UNITY_EDITOR) {
            return (Resolve-Path $env:UNITY_EDITOR).Path
        }
    }

    $hubRoot = "C:\Program Files\Unity\Hub\Editor"
    if (Test-Path $hubRoot) {
        $candidate = Get-ChildItem $hubRoot -Directory |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "Editor\Unity.exe" } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1

        if ($candidate) {
            return $candidate
        }
    }

    $defaultPath = "C:\Program Files\Unity\Editor\Unity.exe"
    if (Test-Path $defaultPath) {
        return $defaultPath
    }

    throw "Unity executable was not found. Pass -UnityPath or set UNITY_EDITOR."
}

$unityExe = Find-UnityEditor -RequestedPath $UnityPath
$logDir = Split-Path $LogFile -Parent
if (-not [string]::IsNullOrWhiteSpace($logDir)) {
    New-Item -ItemType Directory -Force $logDir | Out-Null
}

$arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath,
    "-logFile", $LogFile
)

if ($ApplySceneRequest) {
    $arguments += @("-executeMethod", $ExecuteMethod)
}

Write-Host "Unity: $unityExe"
Write-Host "Project: $ProjectPath"
Write-Host "Log: $LogFile"
if ($ApplySceneRequest) {
    Write-Host "ExecuteMethod: $ExecuteMethod"
}

& $unityExe @arguments
exit $LASTEXITCODE

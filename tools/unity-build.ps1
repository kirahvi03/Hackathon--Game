<#
.SYNOPSIS
    用 Unity 命令行批量构建工程（无需打开编辑器 GUI）。

.DESCRIPTION
    工程必须已经存在于磁盘上。构建产物默认落在仓库的 Builds/ 下（已被 .gitignore 排除，
    分发请走 GitHub Releases）。

.PARAMETER ProjectPath
    Unity 工程根目录（含 Assets/ 的那一层）。默认自动找仓库内第一个这样的目录。

.PARAMETER Target
    构建目标：Windows64（默认）或 WebGL。

.PARAMETER OutputPath
    产物路径。Windows64 是 .exe 文件路径；WebGL 是输出目录。

.PARAMETER EditorPath
    编辑器根目录。不指定则自动搜索。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-build.ps1
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-build.ps1 -Target WebGL
#>

[CmdletBinding()]
param(
    [string]$ProjectPath,
    [ValidateSet('Windows64', 'WebGL')]
    [string]$Target = 'Windows64',
    [string]$OutputPath,
    [string]$EditorPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

function Find-UnityEditor {
    param([string]$Explicit)
    if ($Explicit) {
        $candidate = Join-Path $Explicit 'Unity.exe'
        if (Test-Path $candidate) { return $candidate }
        throw "在 '$Explicit' 下找不到 Unity.exe"
    }
    $roots = @(
        'C:\Program Files\Unity\Hub\Editor',
        'C:\Program Files\Unity',
        "$env:LOCALAPPDATA\Programs\Unity\Hub\Editor",
        'D:\Unity\Hub\Editor'
    ) | Where-Object { Test-Path $_ }
    $found = foreach ($root in $roots) {
        Get-ChildItem $root -Recurse -Filter 'Unity.exe' -ErrorAction SilentlyContinue -Depth 3
    }
    $best = $found | Sort-Object FullName -Descending | Select-Object -First 1
    if (-not $best) { return $null }
    return $best.FullName
}

if (-not $ProjectPath) {
    $candidate = Get-ChildItem $repoRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { Test-Path (Join-Path $_.FullName 'Assets') } |
        Select-Object -First 1
    if (-not $candidate) {
        Write-Host '  [FAIL] 仓库里找不到 Unity 工程（含 Assets/ 的目录）。' -ForegroundColor Red
        Write-Host '         先跑 .\tools\create-unity-project.ps1' -ForegroundColor DarkYellow
        exit 1
    }
    $ProjectPath = $candidate.FullName
}
$ProjectPath = [IO.Path]::GetFullPath($ProjectPath)

if (-not (Test-Path (Join-Path $ProjectPath 'Assets'))) {
    Write-Host ("  [FAIL] '{0}' 里没有 Assets/，不是 Unity 工程根目录。" -f $ProjectPath) -ForegroundColor Red
    exit 1
}

$buildsDir = Join-Path $repoRoot 'Builds'
New-Item -ItemType Directory -Force -Path $buildsDir | Out-Null
if (-not $OutputPath) {
    $OutputPath = if ($Target -eq 'Windows64') {
        Join-Path $buildsDir 'HackathonGame.exe'
    } else {
        Join-Path $buildsDir 'WebGL'
    }
}

$unityExe = Find-UnityEditor -Explicit $EditorPath
if (-not $unityExe) {
    Write-Host '  [FAIL] 没找到已安装的 Unity 编辑器。' -ForegroundColor Red
    exit 1
}

$method = if ($Target -eq 'Windows64') { 'BuildWindows64Player' } else { 'BuildWebGLPlayer' }
$logFile = Join-Path $buildsDir ("build-{0}-{1:yyyyMMdd-HHmmss}.log" -f $Target, (Get-Date))

Write-Host 'Unity 批量构建' -ForegroundColor White
Write-Host ("  工程   : {0}" -f $ProjectPath)
Write-Host ("  编辑器 : {0}" -f $unityExe)
Write-Host ("  目标   : {0}" -f $Target)
Write-Host ("  产物   : {0}" -f $OutputPath)
Write-Host ("  日志   : {0}" -f $logFile)
Write-Host ''

$unityArgs = @(
    '-batchmode', '-quit', '-nographics',
    '-projectPath', $ProjectPath,
    "-$method", $OutputPath,
    '-logFile', $logFile
)
if ($Target -eq 'WebGL') {
    # WebGL 需要显式指定，否则部分版本不生成
    $unityArgs += @('-buildTarget', 'WebGL')
}

& $unityExe @unityArgs
$code = $LASTEXITCODE
Write-Host ''
Write-Host ("Unity exit code = {0}" -f $code)

if ($Target -eq 'Windows64') {
    if (Test-Path $OutputPath) {
        $size = (Get-Item $OutputPath).Length / 1MB
        Write-Host ("构建成功: {0} ({1:N1} MB)" -f $OutputPath, $size) -ForegroundColor Green
    } else {
        Write-Host ('构建失败：产物不存在。检查日志末尾的编译错误：') -ForegroundColor Red
        if (Test-Path $logFile) { Get-Content $logFile -Tail 40 }
        exit 1
    }
} else {
    if (Test-Path (Join-Path $OutputPath 'index.html')) {
        Write-Host ("构建成功: {0}" -f $OutputPath) -ForegroundColor Green
    } else {
        Write-Host '构建失败：WebGL 输出缺少 index.html。' -ForegroundColor Red
        if (Test-Path $logFile) { Get-Content $logFile -Tail 40 }
        exit 1
    }
}

Write-Host ''
Write-Host '分发建议：把 Builds/ 里的产物打成 zip 传到 GitHub Releases，不要提交进仓库。' -ForegroundColor Cyan
exit 0

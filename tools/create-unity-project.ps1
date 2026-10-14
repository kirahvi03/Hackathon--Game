<#
.SYNOPSIS
    从已安装的 Unity 编辑器自带的官方工程模板创建 Unity 工程。

.DESCRIPTION
    不去猜 manifest.json 的包版本——Unity 编辑器随包提供官方 ProjectTemplates
    （.tgz），里面就是与该编辑器版本**精确匹配**的工程骨架（Assets / Packages /
    ProjectSettings）。本脚本解出模板并铺到目标目录，因此不会出现包版本不匹配。

    如果找不到模板，退化为调用 Unity 自身的 -createProject。

.PARAMETER TargetPath
    工程根目录。默认为仓库下的 MyGame。

.PARAMETER Template
    模板关键字，默认 2d。可选 3d、3d-urp、2d-urp 等（按实际存在的模板匹配）。

.PARAMETER EditorPath
    编辑器根目录（含 Unity.exe 的那一层）。不指定则自动搜索已安装版本。

.PARAMETER Force
    目标目录已存在且非空时，仍然继续（会覆盖同名文件）。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\create-unity-project.ps1
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\create-unity-project.ps1 -Template 3d -TargetPath .\MyGame3D
#>

[CmdletBinding()]
param(
    [string]$TargetPath,
    [string]$Template = '2d',
    [string]$EditorPath,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $TargetPath) { $TargetPath = Join-Path $repoRoot 'MyGame' }
$TargetPath = [IO.Path]::GetFullPath($TargetPath)

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

Write-Host '从官方模板创建 Unity 工程' -ForegroundColor White
Write-Host ("仓库根 : {0}" -f $repoRoot)
Write-Host ("目标   : {0}" -f $TargetPath)
Write-Host ("模板   : {0}" -f $Template)

$unityExe = Find-UnityEditor -Explicit $EditorPath
if (-not $unityExe) {
    Write-Host '  [FAIL] 没找到已安装的 Unity 编辑器。' -ForegroundColor Red
    Write-Host '         先安装编辑器（Unity Hub → Installs，或用已下载的离线安装包），' -ForegroundColor DarkYellow
    Write-Host '         或用 -EditorPath 指定含 Unity.exe 的目录。' -ForegroundColor DarkYellow
    exit 1
}
$editorDir = Split-Path -Parent $unityExe
Write-Host ("  [ OK ] 编辑器: {0}" -f $unityExe) -ForegroundColor Green

if ((Test-Path $TargetPath) -and (Get-ChildItem $TargetPath -Force -ErrorAction SilentlyContinue)) {
    if (-not $Force) {
        Write-Host ("  [FAIL] 目标目录已存在且非空: {0}" -f $TargetPath) -ForegroundColor Red
        Write-Host '         换一个 -TargetPath，或加 -Force 覆盖同名文件。' -ForegroundColor DarkYellow
        exit 1
    }
    Write-Host '  [WARN] 目标目录非空，-Force 已指定，继续。' -ForegroundColor DarkYellow
}

# ---- 找官方工程模板 ----
$tplDir = Join-Path $editorDir 'Data\Resources\PackageManager\ProjectTemplates'
$tpl = $null
if (Test-Path $tplDir) {
    $tpl = Get-ChildItem $tplDir -Filter '*.tgz' -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "*template.$Template-*" } |
        Sort-Object Name -Descending | Select-Object -First 1
    if (-not $tpl) {
        Write-Host ("  [WARN] 没有匹配 '{0}' 的模板，可用模板：" -f $Template) -ForegroundColor DarkYellow
        Get-ChildItem $tplDir -Filter '*.tgz' | ForEach-Object { Write-Host ("         " + $_.Name) -ForegroundColor DarkGray }
    }
}

New-Item -ItemType Directory -Force -Path $TargetPath | Out-Null

if ($tpl) {
    Write-Host ("  [ OK ] 官方模板: {0}" -f $tpl.Name) -ForegroundColor Green
    $staging = Join-Path $env:TEMP ('unity-tpl-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
    New-Item -ItemType Directory -Force -Path $staging | Out-Null

    Write-Host '  解压模板...' -ForegroundColor DarkGray
    & tar.exe -xzf $tpl.FullName -C $staging
    if ($LASTEXITCODE -ne 0) { throw "tar 解压失败（exit $LASTEXITCODE）" }

    # 模板 tgz 的根一般是 package/，其中才是真正的工程骨架
    $src = Join-Path $staging 'package'
    if (-not (Test-Path $src)) {
        $src = (Get-ChildItem $staging -Directory | Select-Object -First 1).FullName
    }
    if (-not $src -or -not (Test-Path $src)) { throw '解压后找不到工程骨架目录' }

    Write-Host '  铺到目标目录...' -ForegroundColor DarkGray
    Copy-Item -Path (Join-Path $src '*') -Destination $TargetPath -Recurse -Force

    # 模板描述文件不属于工程本身
    $descriptor = Join-Path $TargetPath 'package.json'
    if (Test-Path $descriptor) { Remove-Item $descriptor -Force }

    Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
} else {
    Write-Host '  退化为 Unity -createProject（默认设置）' -ForegroundColor DarkYellow
    & $unityExe -batchmode -quit -nographics -createProject $TargetPath -logFile -
    Write-Host ("  Unity exit={0}" -f $LASTEXITCODE) -ForegroundColor DarkGray
}

# ---- 校验 ----
Write-Host ''
Write-Host '== 校验生成结果 ==' -ForegroundColor Cyan
$checks = @{
    'Assets'                            = Join-Path $TargetPath 'Assets'
    'Packages/manifest.json'            = Join-Path $TargetPath 'Packages\manifest.json'
    'ProjectSettings/ProjectVersion.txt'= Join-Path $TargetPath 'ProjectSettings\ProjectVersion.txt'
}
$failed = 0
foreach ($k in $checks.Keys) {
    if (Test-Path $checks[$k]) {
        Write-Host ("  [ OK ] {0}" -f $k) -ForegroundColor Green
    } else {
        $failed++
        Write-Host ("  [FAIL] {0} 缺失" -f $k) -ForegroundColor Red
    }
}

$pv = Join-Path $TargetPath 'ProjectSettings\ProjectVersion.txt'
if (Test-Path $pv) {
    Write-Host ''
    Write-Host '  ProjectVersion.txt:' -ForegroundColor DarkGray
    Get-Content $pv | ForEach-Object { Write-Host ("    " + $_) -ForegroundColor DarkGray }
}

if ($failed -gt 0) {
    Write-Host ''
    Write-Host '生成不完整，请检查上面的失败项。' -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host '完成。下一步：' -ForegroundColor Cyan
Write-Host '  1) 用 Unity Hub 打开这个目录（Projects → Add → 选择该目录）' -ForegroundColor Gray
Write-Host '  2) 配置 Unity SmartMerge: .\scripts\configure-unity-git.ps1' -ForegroundColor Gray
Write-Host ('  3) git add -A; git commit -m "feat: add Unity project"; git push') -ForegroundColor Gray
exit 0

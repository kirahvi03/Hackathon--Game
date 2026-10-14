<#
.SYNOPSIS
    为当前仓库注册 Unity SmartMerge（UnityYAMLMerge）合并驱动。

.DESCRIPTION
    在 .gitattributes 中声明了 merge=unityyamlmerge 的 Unity YAML 资产
    （.unity / .prefab / .asset / .mat 等）会调用本脚本注册的合并驱动。
    不注册也能用，但场景冲突会退化为文本合并，容易产生损坏的 YAML。

    用法：
        powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\configure-unity-git.ps1
        powershell ... -File .\scripts\configure-unity-git.ps1 -EditorPath "D:\Unity\2022.3.62f3c1\Editor"

.NOTES
    只写入**当前仓库**的 git config（--local），不污染全局配置。
#>

[CmdletBinding()]
param(
    # 显式指定编辑器根目录（即包含 Editor\Data\Tools\UnityYAMLMerge.exe 的那一层）。
    [string]$EditorPath
)

$ErrorActionPreference = 'Stop'

function Find-UnityYamlMerge {
    if ($EditorPath) {
        $candidate = Join-Path $EditorPath 'Data\Tools\UnityYAMLMerge.exe'
        if (Test-Path $candidate) { return $candidate }
        throw "在 '$EditorPath' 下找不到 Data\Tools\UnityYAMLMerge.exe"
    }

    $searchRoots = @(
        'C:\Program Files\Unity\Hub\Editor',
        'C:\Program Files\Unity\Editor',
        'C:\Program Files\Unity',
        "$env:LOCALAPPDATA\Programs\Unity\Hub\Editor",
        'D:\Unity\Hub\Editor'
    ) | Where-Object { Test-Path $_ }

    foreach ($root in $searchRoots) {
        # 优先取版本号最大的编辑器
        $found = Get-ChildItem $root -Recurse -Filter 'UnityYAMLMerge.exe' -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if ($found) { return $found.FullName }
    }
    return $null
}

Write-Host 'Unity SmartMerge 合并驱动配置' -ForegroundColor White

$mergeExe = Find-UnityYamlMerge
if (-not $mergeExe) {
    Write-Host '  [FAIL] 找不到 UnityYAMLMerge.exe。' -ForegroundColor Red
    Write-Host '         请确认 Unity 编辑器已安装，或用 -EditorPath 手动指定编辑器根目录，例如：' -ForegroundColor DarkYellow
    Write-Host '         -EditorPath "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor"' -ForegroundColor DarkGray
    exit 1
}

Write-Host ("  [ OK ] 找到: {0}" -f $mergeExe) -ForegroundColor Green

# 确认当前在 git 仓库内
$repoRoot = git rev-parse --show-toplevel 2>$null
if (-not $repoRoot) {
    Write-Host '  [FAIL] 当前目录不是 git 仓库。' -ForegroundColor Red
    exit 1
}
Write-Host ("  [ OK ] 仓库: {0}" -f $repoRoot) -ForegroundColor Green

# 路径用正斜杠写入，避免 git config 里的反斜杠转义问题
$mergeExeGit = $mergeExe -replace '\\', '/'
git config --local merge.unityyamlmerge.name 'Unity SmartMerge'
git config --local merge.unityyamlmerge.driver "`"$mergeExeGit`" merge -p %O %B %A %A"
git config --local merge.unityyamlmerge.recursive binary

Write-Host '  [ OK ] 已写入本仓库 git config:' -ForegroundColor Green
Write-Host ("         merge.unityyamlmerge.driver = `"$mergeExeGit`" merge -p %O %B %A %A") -ForegroundColor DarkGray
Write-Host ''
Write-Host '完成。之后 .unity / .prefab /.asset 等文件冲突会走 SmartMerge 语义合并。' -ForegroundColor Cyan
exit 0

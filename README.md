# Hackathon -- Game

黑客松的**独立游戏项目**仓库，引擎为 **Unity**。

> 插件方向的代码在另一个仓库：[Hackathon--Plug-In](https://github.com/kirahvi03/Hackathon--Plug-In)。

---

## 1. 当前状态

| 项 | 状态 |
| --- | --- |
| Unity Hub | ✅ 已装（3.3.6，中国版） |
| 许可证 | ✅ 已激活 Personal（`C:\ProgramData\Unity\Unity_lic.ulf`） |
| Unity 编辑器 | ⏳ 安装包下载中 —— **2022.3.62f3c1 LTS**（3.76GB） |
| Git LFS | ✅ 已启用并配好跟踪规则 |
| 工程骨架 | ⏳ 待编辑器装好后用 `tools/create-unity-project.ps1` 生成 |
| 本机硬件 | 16 逻辑核 / 15.5GB 内存 / C 盘剩余约 790GB |

编辑器安装包：`C:\Users\19371\Downloads\UnitySetup64-2022.3.62f3c1.exe`
校验基准：大小 `3,759,910,736` bytes，MD5 `546ec5901f3ffc7c5ef5835e4096e381`。

> ⚠️ 这次下载**被中断过两次**（CDN 连接被重置）。脚本用 `curl -C -` 断点续传，
> 任何一次中断后重跑同一条命令即可接着下，不会从头开始。

---

## 2. 编辑器装好后：创建工程

**不要**在 Hub 里手点新建，用脚本从编辑器自带的**官方模板**生成——
这样 `Packages/manifest.json` 的包版本与编辑器版本精确匹配，避免包解析错误：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\create-unity-project.ps1
# 3D：加 -Template 3d
```

它会：找到已安装编辑器 → 取 `Data\Resources\PackageManager\ProjectTemplates`
里的官方模板 → 铺到 `MyGame/` → 校验 `Assets/`、`Packages/manifest.json`、
`ProjectSettings/ProjectVersion.txt` 三件套。

然后注册 Unity 的场景语义合并驱动：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\configure-unity-git.ps1
```

---

## 3. 文档与脚本

| 文件 | 用途 |
| --- | --- |
| [`docs/game-design.md`](docs/game-design.md) | **先填这个**：一句话概念 / 核心循环 / 技术边界 |
| [`docs/unity-workflow.md`](docs/unity-workflow.md) | 协作约定：`.meta` 必提交、场景冲突、LFS 配额、排错 |
| `tools/create-unity-project.ps1` | 从官方模板创建工程（版本精确匹配） |
| `tools/unity-build.ps1` | 命令行批量构建 Windows64 / WebGL |
| `scripts/configure-unity-git.ps1` | 注册 SmartMerge 合并驱动（每个克隆跑一次） |

---

## 4. 目录结构

```
hackathon-game/
├─ README.md
├─ .gitignore                    Unity 忽略规则（含子目录工程）
├─ .gitattributes                LFS + Unity SmartMerge 规则
├─ docs/
│  ├─ game-design.md             游戏设计文档（模板，先填）
│  └─ unity-workflow.md          Unity 协作工作流
├─ scripts/
│  └─ configure-unity-git.ps1    注册 SmartMerge
├─ tools/
│  ├─ create-unity-project.ps1   从官方模板创建工程
│  └─ unity-build.ps1            批量构建
├─ MyGame/                       ← Unity 工程根（待生成）
│  ├─ Assets/
│  ├─ Packages/manifest.json
│  └─ ProjectSettings/
└─ Builds/                       构建产物（已忽略，分发走 Releases）
```

---

## 5. 已知环境问题

| 问题 | 影响 | 处理 |
| --- | --- | --- |
| Unity China CDN 不稳（连接会被重置） | 大文件下载中断 | `curl -C -` 续传，重跑即可 |
| npm 公网 TLS 中间人（证书 altname `*.bistu.edu.cn`） | 装 Unity 包/素材工具可能失败 | 用 Unity 自带包管理器，或走镜像 |
| Unity Hub 中国版无 headless CLI | 装不了纯自动化 | 编辑器安装走 GUI 或直接跑离线安装包 |
| `pwsh` 未安装 | 脚本需 Windows PowerShell 5.1 兼容 | 现有脚本均已在 5.1 下验证 |

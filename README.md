# Hackathon -- Game

黑客松的**独立游戏项目**仓库，引擎为 **Unity**（中国版 Hub 3.3.6 已装）。

> 插件方向的代码在另一个仓库：[Hackathon--Plug-In](https://github.com/kirahvi03/Hackathon--Plug-In)。

---

## 1. 当前状态

| 项 | 状态 |
| --- | --- |
| Unity Hub | ✅ 已装（3.3.6，中国版） |
| Unity 编辑器 | ⏳ 下载中 —— **2022.3.62f3c1 LTS**（3.76GB，离线安装包） |
| 许可证 | ❌ 未激活，需在 Unity Hub 登录账号 |
| Git LFS | ✅ 已在本仓库启用并配好跟踪规则 |
| 本机硬件 | 16 逻辑核 / 15.5GB 内存 / C 盘剩余 791GB |

编辑器安装包位置：`C:\Users\19371\Downloads\UnitySetup64-2022.3.62f3c1.exe`
（校验：大小 3,759,910,736 bytes，MD5 `546ec5901f3ffc7c5ef5835e4096e381`）

---

## 2. 编辑器装好后：创建工程

务必**在本仓库根目录内**创建 Unity 工程，否则 Git 与 LFS 跟踪会错位。

1. 打开 **Unity Hub** → 右上角登录 Unity 账号 → 激活 Personal 许可证
2. `Installs` → `Add` → 选择已下载的安装包，或 `Locate` 指向已安装的编辑器目录
3. `Projects` → `New project` → 模板建议 **2D (Built-in Render Pipeline)**
   （黑客松时间紧，2D 出成果最快；3D/URP 也可以，但素材与调优成本更高）
4. 工程路径选 `C:\Users\19371\Documents\deepseek-harness\hackathon-game`
   —— 注意 Unity 要求工程目录为**空目录或它自己创建**，所以建议先建子目录：

   ```
   hackathon-game/
   └─ MyGame/          ← Unity 工程根（Assets/ Packages/ ProjectSettings/ 在这里）
   ```

   这样仓库根仍可放 README、脚本与文档。

---

## 3. Unity 专用 Git 配置（一次性）

`.gitattributes` 已声明 Unity 的 YAML 资产（场景、预制体、材质等）用 **SmartMerge** 做语义合并，
避免多人同时改一个场景就冲突到无法合并。需要注册合并驱动：

```powershell
# 编辑器安装后运行，自动定位 UnityYAMLMerge.exe 并写入本仓库 git config
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\configure-unity-git.ps1
```

未配置时 Git 会退化为普通文本合并（不报错，但合并 YAML 场景容易产生坏文件）。

---

## 4. GitHub LFS 配额提醒

GitHub 免费账号的 LFS 额度是 **1GB 存储 + 1GB/月流量**。美术资源（贴图、音频、模型）
很容易超。建议：

- 贴图注意压缩与分辨率，音频用 `.ogg` 而非 `.wav`
- 大体积构建产物**不要入库**（`.gitignore` 已排除 `Library/`、`Builds/`、`*.apk` 等），
  需要分发时走 **GitHub Releases**
- 用 `git lfs ls-files` 查看当前被 LFS 管理的文件，用 `git lfs status` 看占用

---

## 5. 目录规划（建议）

```
hackathon-game/
├─ README.md
├─ .gitignore            Unity 专用忽略规则
├─ .gitattributes        LFS + Unity SmartMerge 规则
├─ scripts/
│  └─ configure-unity-git.ps1
└─ MyGame/               Unity 工程（由 Unity Hub 创建）
   ├─ Assets/
   ├─ Packages/
   └─ ProjectSettings/
```

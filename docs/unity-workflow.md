# Unity 工程工作流

面向本仓库的约定。目标是**多人协作不互相踩**，以及黑客松期间少花时间在环境上。

---

## 1. 目录约定

```
hackathon-game/
├─ MyGame/                    ← Unity 工程根（由 tools/create-unity-project.ps1 生成）
│  ├─ Assets/
│  │  ├─ _Project/            ← 我们自己的东西全放这里，与导入的素材包隔开
│  │  │  ├─ Scenes/           场景
│  │  │  ├─ Scripts/          C# 脚本（按子系统再分子目录）
│  │  │  ├─ Prefabs/          预制体
│  │  │  ├─ Art/              自绘素材
│  │  │  ├─ Audio/            音频
│  │  │  ├─ Settings/         ScriptableObject 配置资产
│  │  │  └─ UI/
│  │  └─ ThirdParty/          ← 素材站导入的包，原样保留，不要手动改
│  ├─ Packages/manifest.json
│  └─ ProjectSettings/
├─ Builds/                    ← 构建产物，已忽略，分发走 Releases
├─ docs/
├─ scripts/                   Git 相关脚本
└─ tools/                     工程创建 / 构建脚本
```

> **不要提交 `Library/`、`Temp/`、`Builds/`、`Logs/`**——`.gitignore` 已覆盖（含子目录情形）。

---

## 2. 首次创建工程

编辑器装好后，**不要**在 Hub 里手点新建，而是用脚本从编辑器自带的官方模板生成，
这样 `Packages/manifest.json` 的包版本与你的编辑器版本精确匹配，不会出现包解析错误：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\create-unity-project.ps1
# 3D 工程：
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\create-unity-project.ps1 -Template 3d -TargetPath .\MyGame3D
```

生成后：

```powershell
.\scripts\configure-unity-git.ps1     # 注册 SmartMerge 合并驱动
git add -A
git commit -m "feat: add Unity project skeleton"
git push
```

---

## 3. 日常协作规则

### 3.1 `.meta` 必须提交

Unity 为每个资产生成 `.meta`（保存 GUID）。**丢了 meta = 场景里所有引用断掉**。
`.gitignore` 已确保 Assets 下的 `.meta` 不被忽略。提交时注意 `git status` 里
新增资产应同时出现「资产 + 同名.meta」两个文件。

### 3.2 场景冲突

`.unity` / `.prefab` 是 YAML 文本，能 diff 但**直接文本合并极易产生坏文件**。
所以 `.gitattributes` 给它们指定了 `merge=unityyamlmerge`（SmartMerge 语义合并）：

- 每个克隆仓库后跑一次 `.\scripts\configure-unity-git.ps1`
- 仍未配置时，遇到场景冲突**不要**手改，用 `git checkout --theirs/--ours` 二选一更安全

### 3.3 减少场景冲突的实践

| 做法 | 原因 |
| --- | --- |
| 一个功能一个场景，别多人共用一个 Main 场景 | 场景是最容易冲突的资产 |
| 能做成 Prefab 就做成 Prefab | Prefab 比场景小，冲突面小 |
| 用 ScriptableObject 存配置，别把数值写死在场景里 | 数值调整不碰场景文件 |
| 提交前 `git pull --rebase` | 减少无意义 merge 节点 |
| 大改前先分工：谁改场景、谁改脚本 | 时间只有几十小时，冲突一次很贵 |

### 3.4 二进制资源与 LFS 配额

贴图/音频/模型走 Git LFS（`.gitattributes` 已配好）。GitHub 免费额度是
**1GB 存储 + 1GB/月流量**，容易超：

```powershell
git lfs ls-files     # 当前被 LFS 管理的文件
git lfs status       # 待提交的 LFS 对象
```

省额度：音频用 `.ogg` 而不是 `.wav`；贴图控制分辨率；**不要把构建产物入库**。

---

## 4. 构建

```powershell
# Windows 64 位（产物落在 Builds/）
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-build.ps1

# WebGL（评委可直接点开玩，但构建慢、体积大）
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\unity-build.ps1 -Target WebGL
```

构建失败时脚本会打印日志末尾 40 行，直接看编译错误。
完整日志在 `Builds/build-*.log`。

分发：把 `Builds/` 里的产物压成 zip 传到 **GitHub Releases**，不要提交进仓库。

---

## 5. 排错

| 现象 | 处理 |
| --- | --- |
| 打开工程报包解析错误 | `Packages/manifest.json` 的包版本与编辑器不匹配。删掉该包或改用模板重新生成 |
| 资产引用全变 Missing | `.meta` 没提交或被误删。从 git 历史恢复对应 `.meta` |
| 场景合并后打开是空白/报错 | 用了文本合并。回滚该文件，改用 SmartMerge 重新合并 |
| Hub 里看不到编辑器 | `tools/create-unity-project.ps1` 与 `unity-build.ps1` 都能自动搜索；Hub 里用 `Locate` 指向编辑器目录 |
| 构建卡在 "Licensing" | 许可证未激活，打开 Hub 登录并激活 Personal |
| **Unity 启动即崩溃，事件日志里是 `HandleProjectAlreadyOpenInAnotherInstance`** | **同一个工程被两个 Unity 实例同时打开**（最常见是重复双击、或一个后台实例没退干净）。Unity 对这种冲突是**直接致命退出并生成 dump**，不是弹窗提示 |
| 上面那种情况之后再也打不开工程 | 崩溃会残留 `MyGame/Temp/UnityLockfile`。确认没有 Unity 进程后删掉它即可 |

> ⚠️ **务必确认只有一个 Unity 实例**。命令行/脚本启动 Unity 时特别注意：
> `Unity.exe` 是 GUI 子系统程序，PowerShell 用 `& Unity.exe ...` **不会等待**，
> 看起来"秒退"其实进程还在后台跑，随后再启动第二个实例就会撞车崩溃。
> 脚本里要用 `Start-Process -Wait -PassThru`，并且启动前先确认没有别的实例。

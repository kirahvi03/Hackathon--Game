# 交互与对话（Interaction & Dialogue）

> 里程碑对应：**50% 可玩闭环** 里的"叙事入口"。
> 目标：让玩家靠近角色按 `E` 就能"说话"，并让不同立场的人对章鱼有不同反应。

---

## 1. 玩家看到的效果

1. 走进角色的交互范围（默认半径 1.6），屏幕下方出现提示：`按 E 与「研究员」交谈`
2. 按 `E` → 屏幕下方弹出对话框：**左侧人物立绘 + 右侧名字与台词**
3. 再按 `E` 逐句推进，右下角显示 `2 / 4` 的进度；`Esc` 直接关闭
4. 最后一句按 `E` 结束后：
   - **研究员**：只留下信息，无任何负面影响
   - **武装人员**：直接开枪 —— 枪口闪光 + 弹道，主角 **HP −8、pH −0.35**
5. 对话期间玩家被冻结（不能走动、不能扑击），避免"边聊边跑"

---

## 2. 两类角色的立场

| 角色 | 立绘 | 行为 | 台词基调 |
| --- | --- | --- | --- |
| 研究员 `Researcher` | `Resources/Portraits/researcher.png` | 尝试与章鱼交流，试探它是否有意识 | 观察记录 → 试探 → 察觉被模仿 → 要求证明 |
| 武装人员 `Guard` | `Resources/Portraits/guard.png` | 只说三句，然后开枪 | 沉默 → 命令 → 拒绝与"标本"谈判 |

立绘是用户提供的原图，只做了 **webp → PNG** 的容器转换（Unity 2022 不认 webp），分辨率保持 2048×2048 未缩放。

---

## 3. 代码结构

| 文件 | 职责 |
| --- | --- |
| `Interactable.cs` | 数据：`kind` / `displayName` / `lines[]` / `portrait` / 结束效果（`healthDamageOnEnd`、`phDeltaOnEnd`、`shootsOnEnd`）/ `singleUse` |
| `PlayerInteractor.cs` | 每帧 `OverlapCircleAll` 找最近的可交互角色；按 E 且对话未打开时调用 `DialogueSystem.TryOpen` |
| `DialogueSystem.cs` | 单例；维护当前角色与行号；`OnGUI` 绘制对话框；结束时结算效果 |
| `PlayerVitals.cs` | `health` / `ph`，`ApplyDamage(healthDelta, phDelta)` |
| `HudOverlay.cs` | 左上角 HP / pH 状态条、交互提示、操作说明 |
| `MuzzleFlash.cs` | 开枪表现：一条按方向旋转、缩放的亮线 + 枪口亮点，0.09s 后销毁 |
| `UiFont.cs` | 从系统字体创建动态中文字体（内置 Arial 没有中文字形） |

---

## 4. 想加一个新角色 / 改台词

全部数据都在 `WhiteboxBootstrap.SpawnCharacter` 里，不用碰 prefab：

```csharp
var interactable = go.AddComponent<Interactable>();
interactable.kind = CharacterKind.Researcher;
interactable.displayName = "研究员";
interactable.lines = LinesFor(kind);      // ← 改这里，或直接给 string[]
interactable.portrait = LoadPortrait("researcher"); // Resources/Portraits/<名字>.png
interactable.singleUse = true;
interactable.healthDamageOnEnd = 8f;      // 结束时扣血
interactable.phDeltaOnEnd = -0.35f;       // 结束时 pH 变化
interactable.shootsOnEnd = true;          // 结束时开枪表现
```

立绘放在 `Assets/_Project/Resources/Portraits/` 下即可用 `Resources.Load<Sprite>` 按名字取；
`Editor/PortraitImportSettings.cs` 会自动把该目录的图片设为 **Sprite + Point 过滤 + 不压缩**（像素风必须这样，否则边缘发糊）。

---

## 5. 已知限制（下一步要做的）

- 对话框是 **IMGUI** 画的，样式还比较素；美术阶段应换成 uGUI + TMP 的正式对话面板
- 没有打字机效果，也没有分歧选项（对话是线性的）
- 只有一个 `Interactable` 状态，没有"记忆"（比如被枪击后研究员态度变化）
- 武装人员开枪后不会真的变成追击 AI —— 只结算了数值与表现

---

## 6. 怎么验证这套功能

编辑器里：菜单 **Hackathon → 白盒冒烟测试**，会在不进入 Play 的情况下构造一次场景并校验：

- 两张立绘能否通过 Resources.Load<Sprite> 取到
- 是否生成了 1 个研究员 + 2 个武装人员，且都有台词 / 立绘 / 显示名
- 武装人员是否配置了「开枪 + 扣血 + 降 pH」
- PlayerVitals.ApplyDamage 的数值是否正确

无头模式（CI / 脚本）也能跑：

```
Unity.exe -batchmode -nographics -quit -projectPath MyGame ^
  -executeMethod Hackathon.EditorTools.WhiteboxSmokeTest.RunHeadless
```

退出码 0 = 全部通过。**注意**：冒烟测试不验证 IMGUI 的实际绘制效果（无头模式没有渲染），
对话框长相仍需在编辑器里按 Play 亲眼确认。

using System.Collections.Generic;
using UnityEditor;
using Hackathon.Gameplay;
using UnityEngine;

namespace Hackathon.EditorTools
{
    /// <summary>
    /// 白盒冒烟测试：在编辑器里手动构造一次 WhiteboxBootstrap 的产物，
    /// 检查角色、立绘、台词是否都到位。可在无头模式跑（CI 用）。
    ///
    /// 用法：
    ///   菜单 Hackathon → 白盒冒烟测试
    ///   或 Unity.exe -batchmode -quit -executeMethod Hackathon.EditorTools.WhiteboxSmokeTest.RunHeadless
    /// </summary>
    public static class WhiteboxSmokeTest
    {
        static int _failures;

        [MenuItem("Hackathon/白盒冒烟测试（构造场景 + 校验角色）")]
        public static void RunMenu()
        {
            int code = Run();
            EditorUtility.DisplayDialog("白盒冒烟测试", code == 0 ? "全部通过" : (_failures + " 项失败，见 Console"), "OK");
        }

        /// <summary>无头入口，退出码 0 = 通过。</summary>
        public static void RunHeadless()
        {
            int code = Run();
            EditorApplication.Exit(code);
        }

        static int Run()
        {
            _failures = 0;

            // 记录测试前的根物体，结束后清理干净，避免污染场景
            var before = new HashSet<GameObject>(Object.FindObjectsOfType<GameObject>());

            try
            {
                // 立绘能被 Resources 加载
                Check(Resources.Load<Sprite>("Portraits/researcher") != null, "立绘 researcher 可加载");
                Check(Resources.Load<Sprite>("Portraits/guard") != null, "立绘 guard 可加载");

                // 俯视贴图能被 Resources 加载
                Check(ActorSprites.Load(ActorSprites.OctopusId) != null, "俯视贴图 octopus 可加载");
                Check(ActorSprites.Load(ActorSprites.ResearcherId) != null, "俯视贴图 researcher 可加载");
                Check(ActorSprites.Load(ActorSprites.GuardId) != null, "俯视贴图 guard 可加载");

                // 攻击差分图、场景背景、杂物贴图
                Check(ActorSprites.Load(ActorSprites.OctopusAttackId) != null, "攻击差分图 octopus_attack 可加载");
                Check(ActorSprites.Load(ActorSprites.FloorId) != null, "场景背景 floor_lab 可加载");
                foreach (string propId in PropSprites.AllIds)
                {
                    Check(PropSprites.Load(propId) != null, "杂物贴图可加载: " + propId);
                }

                var boot = new GameObject("Boot__SmokeTest");
                var bootstrap = boot.AddComponent<WhiteboxBootstrap>();
                boot.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);

                var interactables = Object.FindObjectsOfType<Interactable>();
                Debug.Log("[Smoke] 生成的可交互角色数 = " + interactables.Length);
                Check(interactables.Length >= 3, "至少生成 3 个可交互角色（1 研究员 + 2 武装人员）");

                int researchers = 0, guards = 0;
                foreach (var it in interactables)
                {
                    Check(it.lines != null && it.lines.Length > 0, it.displayName + " 有台词");
                    Check(it.portrait != null, it.displayName + " 绑定了立绘");

                    var actorRenderer = it.GetComponentInChildren<SpriteRenderer>();
                    string expectedSprite = it.kind == CharacterKind.Guard ? "guard" : "researcher";
                    Check(actorRenderer != null && actorRenderer.sprite != null && actorRenderer.sprite.name == expectedSprite,
                        it.displayName + " 挂上了 " + expectedSprite + " 俯视贴图");

                    Check(actorRenderer != null && Mathf.Abs(actorRenderer.bounds.size.y - 1.52f) < 0.05f,
                        it.displayName + " 贴图高度 ≈ 1.52（1.9 的 80%）");

                    var actorBody = it.GetComponent<Rigidbody2D>();
                    Check(actorBody != null && actorBody.freezeRotation, it.displayName + " 被推开时不转圈");
                    Check(!string.IsNullOrEmpty(it.displayName), "角色有显示名");

                    if (it.kind == CharacterKind.Researcher) researchers++;
                    else if (it.kind == CharacterKind.Guard)
                    {
                        guards++;
                        Check(it.shootsOnEnd, "武装人员谈完会开枪");
                        Check(it.healthDamageOnEnd > 0f, "武装人员会扣血");
                        Check(it.phDeltaOnEnd < 0f, "武装人员会让 pH 向酸性偏移");
                    }
                }

                Debug.Log("[Smoke] 研究员 = " + researchers + "，武装人员 = " + guards);
                Check(researchers >= 1, "存在研究员");
                Check(guards >= 1, "存在武装人员");

                var player = GameObject.Find("Player");
                Check(player != null, "生成了 Player");
                if (player != null)
                {
                    var playerRenderer = player.GetComponentInChildren<SpriteRenderer>();
                    Check(playerRenderer != null && playerRenderer.sprite != null && playerRenderer.sprite.name == "octopus",
                        "主角挂上了章鱼贴图");
                    Check(player.GetComponent<SpriteFacing>() != null, "主角有左右翻转组件");
                    Check(playerRenderer != null && Mathf.Abs(playerRenderer.bounds.size.y - 1.6f) < 0.05f,
                        "主角贴图高度 ≈ 1.6");

                    var playerBody = player.GetComponent<Rigidbody2D>();
                    Check(playerBody != null && playerBody.freezeRotation, "主角刚体锁定旋转（只能翻转，不能转圈）");
                }

                // 背景：一整张场景贴图，而不是 256 块菱形瓦片
                var floorRoot = GameObject.Find("Floor");
                Check(floorRoot != null, "生成了 Floor");
                if (floorRoot != null)
                {
                    var floorRenderers = floorRoot.GetComponentsInChildren<SpriteRenderer>();
                    Check(floorRenderers.Length == 1,
                        "背景是单张贴图（渲染器数 = " + floorRenderers.Length + "，回退瓦片时会是 256）");
                    if (floorRenderers.Length == 1)
                    {
                        Check(floorRenderers[0].sprite != null && floorRenderers[0].sprite.name == "floor_lab",
                            "背景挂的是 floor_lab");
                        Check(floorRenderers[0].sortingOrder <= -1000, "背景在最底层");
                    }
                }

                // 相机底色要和背景贴图的暗部一致，否则背景板两侧会露出明显色带
                var cam = Object.FindObjectOfType<Camera>();
                Check(cam != null, "场景里有相机");
                if (cam != null)
                {
                    Color camBg = cam.backgroundColor;
                    Check(Mathf.Abs(camBg.r - 0.059f) < 0.02f
                          && Mathf.Abs(camBg.g - 0.059f) < 0.02f
                          && Mathf.Abs(camBg.b - 0.055f) < 0.02f,
                        "相机底色与背景暗部一致 #0F0F0E，实际 #" + ColorUtility.ToHtmlStringRGB(camBg));
                }

                // 杂物：每件都换成了贴图池里的一张
                int propChecked = 0;
                foreach (var destructible in Object.FindObjectsOfType<Destructible>())
                {
                    if (destructible.bodyClass == BodyClass.Creature) continue;

                    propChecked++;
                    var propRenderer = destructible.GetComponentInChildren<SpriteRenderer>();
                    string propName = propRenderer != null && propRenderer.sprite != null ? propRenderer.sprite.name : "(null)";
                    Check(System.Array.IndexOf(PropSprites.AllIds, propName) >= 0,
                        "杂物 " + destructible.name + " 挂上了贴图: " + propName);
                }
                Check(propChecked >= 13, "至少生成了 13 件杂物（10 小件 + 3 大件），实际 " + propChecked);

                var vitals = Object.FindObjectOfType<PlayerVitals>();
                Check(vitals != null, "主角有 PlayerVitals");
                if (vitals != null)
                {
                    float hp0 = vitals.health, ph0 = vitals.ph;
                    vitals.ApplyDamage(8f, -0.35f);
                    Check(Mathf.Abs(vitals.health - (hp0 - 8f)) < 0.01f, "ApplyDamage 扣血正确");
                    Check(Mathf.Abs(vitals.ph - (ph0 - 0.35f)) < 0.01f, "ApplyDamage 改变 pH 正确");
                }

                Check(Object.FindObjectOfType<DialogueSystem>() != null, "场景里有 DialogueSystem");
                Check(Object.FindObjectOfType<PlayerInteractor>() != null, "玩家有 PlayerInteractor");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Smoke] 抛异常: " + e);
                _failures++;
            }
            finally
            {
                foreach (var go in Object.FindObjectsOfType<GameObject>())
                {
                    if (!before.Contains(go)) Object.DestroyImmediate(go);
                }
            }

            Debug.Log(_failures == 0 ? "[Smoke] 全部通过" : "[Smoke] 失败项 = " + _failures);
            return _failures == 0 ? 0 : 1;
        }

        static void Check(bool condition, string description)
        {
            if (condition) Debug.Log("  ✔ " + description);
            else
            {
                Debug.LogError("  ✘ " + description);
                _failures++;
            }
        }
    }
}

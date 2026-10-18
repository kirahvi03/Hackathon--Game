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
                }

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

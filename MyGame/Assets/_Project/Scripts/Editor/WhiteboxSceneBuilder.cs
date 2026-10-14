using Hackathon.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hackathon.EditorTools
{
    /// <summary>
    /// 生成白盒场景，省掉手工建场景 + 拖组件的步骤。
    ///
    /// 两种用法：
    ///   菜单：Hackathon → 生成白盒场景
    ///   命令行：Unity.exe -batchmode -quit -projectPath MyGame \
    ///            -executeMethod Hackathon.EditorTools.WhiteboxSceneBuilder.Build
    /// </summary>
    public static class WhiteboxSceneBuilder
    {
        const string ScenesFolder = "Assets/_Project/Scenes";
        const string ScenePath = ScenesFolder + "/Whitebox.unity";

        [MenuItem("Hackathon/生成白盒场景")]
        public static void Build()
        {
            // 确保目录存在（AssetDatabase 建目录才会生成正确的 .meta）
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                {
                    AssetDatabase.CreateFolder("Assets", "_Project");
                }
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 场景里只需要一个 Boot 对象，其余全部由 WhiteboxBootstrap 在运行时生成
            var boot = new GameObject("Boot");
            boot.AddComponent<WhiteboxBootstrap>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 把白盒场景设为构建列表里的第一个场景，便于直接出包
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            if (saved)
            {
                Debug.Log("[WhiteboxSceneBuilder] 已生成并保存: " + ScenePath);
            }
            else
            {
                Debug.LogError("[WhiteboxSceneBuilder] 场景保存失败: " + ScenePath);
            }
        }
    }
}

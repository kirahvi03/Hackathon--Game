using System.Collections.Generic;
using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 场景杂物的贴图池。图片放在 Resources/Sprites/Props/ 下，文件名就是 id：
    /// rig（大机台）、console（控制台）、rack（机架）、cabinet（储物柜）、cart（推车）。
    ///
    /// 分两个池子：大件（BodyClass.Large）和小件（BodyClass.Small）各挑各的，
    /// 这样机器不会长成储物柜的样子。加载失败会自动回退到白盒方块。
    /// </summary>
    public static class PropSprites
    {
        /// <summary>大件（机器类）优先用的贴图。</summary>
        public static readonly string[] LargeIds = { "rig", "rack", "cart" };

        /// <summary>小件（箱子类）优先用的贴图。</summary>
        public static readonly string[] SmallIds = { "console", "cabinet", "cart" };

        /// <summary>所有杂物 id，冒烟测试和预加载用。</summary>
        public static readonly string[] AllIds = { "rig", "console", "rack", "cabinet", "cart" };

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>按 id 加载杂物贴图（带缓存，失败返回 null）。</summary>
        public static Sprite Load(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            Sprite cached;
            if (Cache.TryGetValue(id, out cached)) return cached;

            Sprite sprite = Resources.Load<Sprite>("Sprites/Props/" + id);
            if (sprite == null)
            {
                Debug.LogWarning("[Whitebox] 杂物贴图未找到: Resources/Sprites/Props/" + id);
            }
            else if (sprite.texture != null)
            {
                // 像素画不要被双线性插值糊掉
                sprite.texture.filterMode = FilterMode.Point;
                sprite.texture.wrapMode = TextureWrapMode.Clamp;
            }

            Cache[id] = sprite;
            return sprite;
        }

        /// <summary>按种类 + 一个整数（用坐标哈希即可）挑一张贴图。</summary>
        public static Sprite Pick(BodyClass bodyClass, int hash)
        {
            string[] pool = bodyClass == BodyClass.Large ? LargeIds : SmallIds;
            return Load(pool[Mathf.Abs(hash) % pool.Length]);
        }

        /// <summary>把贴图贴到 renderer 上，并按目标宽度（世界单位）等比缩放。</summary>
        public static bool Fit(SpriteRenderer renderer, Sprite sprite, float targetWidth)
        {
            if (renderer == null || sprite == null) return false;

            float width = sprite.bounds.size.x;
            if (width <= 0.0001f) return false;

            float scale = targetWidth / width;
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            return true;
        }

        /// <summary>清缓存（编辑器里热重载资源时用）。</summary>
        public static void ClearCache()
        {
            Cache.Clear();
        }
    }
}

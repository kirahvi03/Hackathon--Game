using System.Collections.Generic;
using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 角色贴图的统一入口。贴图放在 Resources/Sprites 下，文件名就是 id。
    ///
    /// 两张立绘和俯视贴图都是 1-bit 风格，运行时把采样强制成 Point + Clamp，
    /// 保证像素边缘锐利、透明边缘不会采样到画布另一侧。
    /// </summary>
    public static class ActorSprites
    {
        public const string OctopusId = "octopus";
        public const string OctopusAttackId = "octopus_attack";   // 按 F 时临时换上的攻击动作
        public const string ResearcherId = "researcher";
        public const string GuardId = "guard";
        public const string FloorId = "floor_lab";                // 场景背景（整张地面）

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Load(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            Sprite cached;
            if (Cache.TryGetValue(id, out cached) && cached != null) return cached;

            Sprite sprite = Resources.Load<Sprite>("Sprites/" + id);
            if (sprite == null)
            {
                Debug.LogWarning("[Whitebox] 角色贴图未找到: Resources/Sprites/" + id);
                return null;
            }

            Texture2D texture = sprite.texture;
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
            }

            Cache[id] = sprite;
            return sprite;
        }

        /// <summary>
        /// 把精灵缩放到指定世界高度（宽度按原比例跟随）。
        /// 不用 PPU 硬编码，改用 bounds，换分辨率或换图都不用改代码。
        /// </summary>
        public static bool Fit(SpriteRenderer renderer, Sprite sprite, float targetHeight)
        {
            if (renderer == null || sprite == null) return false;

            float spriteHeight = sprite.bounds.size.y;
            if (spriteHeight <= 0.0001f) return false;

            renderer.sprite = sprite;
            float scale = targetHeight / spriteHeight;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            return true;
        }
    }
}
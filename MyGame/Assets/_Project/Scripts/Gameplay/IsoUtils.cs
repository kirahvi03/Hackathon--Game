using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 等轴测（2:1）投影的方向与坐标换算。
    ///
    /// 约定：世界是普通的 XY 平面，等轴测的"斜 45°"观感来自美术与地面菱形排布，
    /// 而不是靠旋转相机。屏幕坐标与世界坐标的关系为：
    ///     screenX = worldX - worldY
    ///     screenY = (worldX + worldY) * VerticalScale
    /// 其中 VerticalScale = 0.5 就是 2:1 等轴测。
    /// </summary>
    public static class IsoUtils
    {
        /// <summary>2:1 等轴测的纵向压缩比例。</summary>
        public const float VerticalScale = 0.5f;

        /// <summary>
        /// 把输入空间的二维方向（x 右、y 上）转成等轴测世界方向。
        /// 这样玩家按 W 就是"屏幕上往上"，按 D 就是"屏幕上往右"，符合直觉。
        /// </summary>
        public static Vector2 InputToWorld(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return Vector2.zero;

            input = input.normalized;
            // 由 screenX = x - y、screenY = (x + y) * VerticalScale 反解：
            float x = (input.x + input.y / VerticalScale) * 0.5f;
            float y = (input.y / VerticalScale - input.x) * 0.5f;
            return new Vector2(x, y).normalized;
        }

        /// <summary>世界坐标转屏幕方向（调试与排序用）。</summary>
        public static Vector2 WorldToScreen(Vector2 world)
        {
            return new Vector2(world.x - world.y, (world.x + world.y) * VerticalScale);
        }

        /// <summary>
        /// 按 Y 计算排序层：Y 越小越靠近镜头，排序层越高。
        /// 等轴测下的前后遮挡全靠这个，否则会出现"人站在箱子后面却画在箱子上面"。
        /// </summary>
        public static int SortingOrderFor(Vector2 worldPosition, int baseOrder = 0)
        {
            return baseOrder - Mathf.RoundToInt(worldPosition.y * 100f);
        }
    }
}

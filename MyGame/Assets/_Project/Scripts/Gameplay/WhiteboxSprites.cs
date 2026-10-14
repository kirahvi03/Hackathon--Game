using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 运行时生成白盒用的精灵，因此这个阶段不需要任何美术资产。
    /// 全部使用 Point 过滤，配合 2D Pixel Perfect 保持像素风。
    /// </summary>
    public static class WhiteboxSprites
    {
        static Sprite _square;
        static Sprite _diamond;

        /// <summary>1x1 世界单位的白色方块；用 transform.localScale 控制大小。</summary>
        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.filterMode = FilterMode.Point;
                    tex.Apply();
                    _square = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                }
                return _square;
            }
        }

        /// <summary>
        /// 2:1 等轴测菱形（宽 1、高 0.5 世界单位），用于铺地面。
        /// 地面菱形按 0.5 的世界间距排布就能无缝拼满。
        /// </summary>
        public static Sprite Diamond(int width = 64, int height = 32, float outline = 0.14f)
        {
            if (_diamond != null) return _diamond;

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            var body = Color.white;
            var edge = new Color(0.72f, 0.72f, 0.72f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 归一化到以中心为原点的菱形距离：|nx| + |ny| <= 1 即菱形内部
                    float nx = Mathf.Abs((x + 0.5f) / width - 0.5f) * 2f;
                    float ny = Mathf.Abs((y + 0.5f) / height - 0.5f) * 2f;
                    float d = nx + ny;

                    Color c;
                    if (d > 1f) c = new Color(0f, 0f, 0f, 0f);
                    else if (d > 1f - outline) c = edge;
                    else c = body;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();

            // pixelsPerUnit = width，使精灵正好是 1 宽、0.5 高（2:1）
            _diamond = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
            return _diamond;
        }
    }
}

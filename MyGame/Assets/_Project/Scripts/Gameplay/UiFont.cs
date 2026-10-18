using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// IMGUI 用的中文字体。
    /// Unity 内置的 Arial 不含中日韩字形，直接用会显示成方框，
    /// 所以从系统字体里挑一个中文字体做动态字体。
    /// </summary>
    public static class UiFont
    {
        static Font _font;
        static bool _resolved;

        public static Font Get()
        {
            if (_resolved) return _font;
            _resolved = true;

            string[] candidates =
            {
                "Microsoft YaHei", "微软雅黑",
                "SimHei", "黑体",
                "SimSun", "宋体",
                "Noto Sans CJK SC"
            };

            try
            {
                _font = Font.CreateDynamicFontFromOSFont(candidates, 24);
            }
            catch (System.Exception)
            {
                _font = null;
            }

            return _font;
        }
    }
}

using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 对话系统：负责播放 <see cref="Interactable"/> 的行、绘制对话框与立绘，
    /// 并在对话结束时结算效果（扣血、pH 变化）。
    ///
    /// 为什么用 IMGUI（OnGUI）而不是 uGUI：白盒阶段不依赖 prefab / Canvas 连线，
    /// 整个界面由代码绘制，改一行字就能看到效果。等美术阶段再换成 uGUI/TMP。
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        /// <summary>全局单例，供玩家控制器与交互器查询状态。</summary>
        public static DialogueSystem Instance { get; private set; }

        [Header("外观")]
        public Color panelColor = new Color(0.06f, 0.07f, 0.09f, 0.96f);
        public Color frameColor = new Color(0.78f, 0.74f, 0.62f, 0.9f);
        public Color nameColor = new Color(0.96f, 0.91f, 0.74f);
        public Color textColor = new Color(0.92f, 0.92f, 0.88f);
        public Color hintColor = new Color(0.62f, 0.62f, 0.58f);

        [Header("尺寸")]
        public float panelHeight = 300f;
        public float maxPanelWidth = 1100f;
        public float sideMargin = 40f;
        public float bottomMargin = 40f;

        Interactable _current;
        int _line;
        int _openedFrame = -1;
        PlayerVitals _vitals;

        /// <summary>当前是否正在对话。</summary>
        public bool IsOpen { get { return _current != null; } }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>绑定主角状态，用于对话结束后的效果结算。</summary>
        public void Bind(PlayerVitals vitals)
        {
            _vitals = vitals;
        }

        /// <summary>尝试开始一段对话。返回是否成功。</summary>
        public bool TryOpen(Interactable target)
        {
            if (target == null || IsOpen) return false;
            if (target.lines == null || target.lines.Length == 0) return false;
            if (target.singleUse && target.consumed) return false;

            _current = target;
            _line = 0;
            // 同一帧内 E 既用于"触发"又用于"继续"，会瞬间跳过第一句，这里记帧号规避
            _openedFrame = Time.frameCount;
            return true;
        }

        void Update()
        {
            if (!IsOpen) return;

            // 开对话的那一帧不处理"继续"
            if (Time.frameCount == _openedFrame) return;

            if (Input.GetKeyDown(KeyCode.E)) Advance();
            else if (Input.GetKeyDown(KeyCode.Escape)) Close(false);
        }

        void Advance()
        {
            _line++;
            if (_current != null && _line >= _current.lines.Length)
            {
                Close(true);
            }
        }

        void Close(bool finished)
        {
            Interactable finishedTarget = finished ? _current : null;
            _current = null;
            _line = 0;

            if (finishedTarget == null) return;

            if (finishedTarget.singleUse) finishedTarget.consumed = true;

            if (finishedTarget.shootsOnEnd)
            {
                // 从开枪者打向主角：有 PlayerVitals 就能拿到主角位置
                Vector2 from = finishedTarget.transform.position;
                Vector2 to = _vitals != null ? (Vector2)_vitals.transform.position : from + Vector2.down;
                MuzzleFlash.Spawn(from, to);
            }

            if (_vitals != null &&
                (finishedTarget.healthDamageOnEnd != 0f || finishedTarget.phDeltaOnEnd != 0f))
            {
                _vitals.ApplyDamage(finishedTarget.healthDamageOnEnd, finishedTarget.phDeltaOnEnd);
            }
        }

        // ---------------------------------------------------------------- 绘制

        void OnGUI()
        {
            var uiFont = UiFont.Get();
            if (uiFont != null) GUI.skin.font = uiFont;
            if (!IsOpen) return;

            float w = Mathf.Min(Screen.width - sideMargin * 2f, maxPanelWidth);
            float h = panelHeight;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - h - bottomMargin;

            var panel = new Rect(x, y, w, h);
            DrawRect(panel, panelColor);
            DrawBorder(panel, frameColor, 2f);

            // 立绘：正方形，贴左，保持比例
            float portraitSize = h - 32f;
            var portraitRect = new Rect(x + 16f, y + 16f, portraitSize, portraitSize);
            DrawRect(portraitRect, new Color(0.12f, 0.12f, 0.13f, 1f));
            if (_current.portrait != null)
            {
                GUI.DrawTexture(portraitRect, _current.portrait.texture, ScaleMode.ScaleToFit, true);
            }
            DrawBorder(portraitRect, new Color(0.45f, 0.45f, 0.45f, 0.9f), 1f);

            // 文本区
            float textLeft = portraitRect.xMax + 24f;
            float textWidth = (x + w - 24f) - textLeft;

            var nameStyle = new GUIStyle(GUI.skin.label);
            nameStyle.fontSize = 26;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.normal.textColor = nameColor;
            GUI.Label(new Rect(textLeft, y + 16f, textWidth, 34f), _current.displayName, nameStyle);

            var textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 22;
            textStyle.wordWrap = true;
            textStyle.normal.textColor = textColor;
            string line = _current.lines[Mathf.Clamp(_line, 0, _current.lines.Length - 1)];
            GUI.Label(new Rect(textLeft, y + 58f, textWidth, h - 120f), line, textStyle);

            // 进度与提示
            var progressStyle = new GUIStyle(GUI.skin.label);
            progressStyle.fontSize = 15;
            progressStyle.alignment = TextAnchor.UpperRight;
            progressStyle.normal.textColor = hintColor;
            GUI.Label(new Rect(textLeft, y + h - 34f, textWidth, 22f),
                string.Format("{0} / {1}", _line + 1, _current.lines.Length), progressStyle);

            var hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 16;
            hintStyle.alignment = TextAnchor.LowerRight;
            hintStyle.normal.textColor = hintColor;
            GUI.Label(new Rect(x, y + h - 32f, w - 16f, 24f), "按 E 继续　·　Esc 关闭", hintStyle);
        }

        static void DrawRect(Rect r, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        static void DrawBorder(Rect r, Color color, float thickness)
        {
            DrawRect(new Rect(r.x, r.y, r.width, thickness), color);
            DrawRect(new Rect(r.x, r.yMax - thickness, r.width, thickness), color);
            DrawRect(new Rect(r.x, r.y, thickness, r.height), color);
            DrawRect(new Rect(r.xMax - thickness, r.y, thickness, r.height), color);
        }
    }
}

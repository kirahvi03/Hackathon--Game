using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 左上角状态条（生命 / pH）与交互提示。全部用 IMGUI 画，白盒阶段零依赖。
    /// </summary>
    public class HudOverlay : MonoBehaviour
    {
        [Header("外观")]
        public Color barBack = new Color(0.10f, 0.11f, 0.13f, 0.85f);
        public Color healthColor = new Color(0.85f, 0.30f, 0.35f);
        public Color textColor = new Color(0.92f, 0.92f, 0.88f);
        public Color hintColor = new Color(0.95f, 0.90f, 0.60f);

        PlayerVitals _vitals;
        PlayerInteractor _interactor;
        float _flashUntil;

        void Update()
        {
            if (_vitals == null) _vitals = FindObjectOfType<PlayerVitals>();
            if (_interactor == null) _interactor = FindObjectOfType<PlayerInteractor>();

            // 受伤后短暂泛红
            if (_vitals != null && Time.time - _vitals.LastDamageTime < 0.35f) _flashUntil = Time.time + 0.2f;
        }

        void OnGUI()
        {
            var uiFont = UiFont.Get();
            if (uiFont != null) GUI.skin.font = uiFont;
            DrawVitals();
            DrawInteractionHint();
            DrawControls();
        }

        void DrawVitals()
        {
            if (_vitals == null) return;

            const float x = 20f, y = 18f, w = 240f, h = 16f;

            DrawRect(new Rect(x - 8f, y - 8f, w + 120f, 76f), new Color(0.05f, 0.06f, 0.07f, 0.75f));

            // 生命
            DrawRect(new Rect(x, y, w, h), barBack);
            float hp = Mathf.Clamp01(_vitals.health / Mathf.Max(1f, _vitals.maxHealth));
            DrawRect(new Rect(x, y, w * hp, h), healthColor);
            GUI.Label(new Rect(x, y - 1f, w, h), string.Format("  HP  {0:0}/{1:0}", _vitals.health, _vitals.maxHealth), LabelStyle(15f, TextAnchor.MiddleLeft));

            // pH
            float py = y + 34f;
            DrawRect(new Rect(x, py, w, h), barBack);
            float phT = Mathf.InverseLerp(_vitals.minPh, _vitals.maxPh, _vitals.ph);
            DrawRect(new Rect(x, py, w * phT, h), _vitals.PhColor());
            GUI.Label(new Rect(x, py - 1f, w, h), string.Format("  pH  {0:0.00}", _vitals.ph), LabelStyle(15f, TextAnchor.MiddleLeft));

            if (Time.time < _flashUntil)
            {
                DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.8f, 0.1f, 0.1f, 0.10f));
            }
        }

        void DrawInteractionHint()
        {
            if (_interactor == null) return;

            Interactable target = _interactor.Nearest;
            DialogueSystem dialogue = DialogueSystem.Instance;
            if (target == null || (dialogue != null && dialogue.IsOpen)) return;

            if (_vitals != null)
            {
                float d = Vector2.Distance(target.transform.position, _vitals.transform.position);
                if (d > _interactor.radius) return;
            }

            var style = LabelStyle(20f, TextAnchor.MiddleCenter);
            style.normal.textColor = hintColor;
            var r = new Rect(Screen.width * 0.5f - 160f, Screen.height - 120f, 320f, 30f);
            GUI.Label(r, string.Format("按 E 与「{0}」交谈", target.displayName), style);
        }

        void DrawControls()
        {
            // 对话打开时隐藏操作提示（对话框会盖住这个位置）
            DialogueSystem dialogue = DialogueSystem.Instance;
            if (dialogue != null && dialogue.IsOpen) return;

            var style = LabelStyle(14f, TextAnchor.LowerLeft);
            style.normal.textColor = new Color(0.7f, 0.7f, 0.66f, 0.9f);
            GUI.Label(new Rect(20f, Screen.height - 30f, 700f, 22f),
                "WASD 移动　F 触手扑击　E 交互/对话　Esc 关闭对话", style);
        }

        static GUIStyle LabelStyle(float size, TextAnchor anchor)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = (int)size;
            s.alignment = anchor;
            s.normal.textColor = Color.white;
            return s;
        }

        static void DrawRect(Rect r, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}

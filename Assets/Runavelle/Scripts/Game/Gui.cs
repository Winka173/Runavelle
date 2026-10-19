using System.Collections.Generic;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>
    /// IMGUI theme + helpers (same UI technology as Arcane-Brigade). Everything is laid out in a virtual
    /// 1920x1080-height space scaled to the screen; Safe area aware; records UI rects so taps on UI never
    /// reach the battlefield.
    /// </summary>
    public static class Gui
    {
        public static float Scale = 1f;
        public static float UiScaleSetting = 1f;
        public static Rect Safe;          // safe area in virtual coordinates
        public static float VW, VH;       // virtual size
        public static readonly List<Rect> Blockers = new List<Rect>();
        private static readonly List<Rect> _blockersBuilding = new List<Rect>();

        public static Font Body, Heading, Numbers, Display;
        public static GUIStyle Label, LabelSmall, LabelCenter, Title, H1, H2, Button, ButtonBig, ButtonSmall, Panel, PanelDark, Tooltip, Number, NumberBig, Hint;
        private static bool _init;
        public static readonly Color Ink = new Color32(43, 32, 46, 255);
        public static readonly Color Parchment = new Color32(243, 231, 204, 255);
        public static readonly Color Gold = new Color32(232, 194, 90, 255);
        public static readonly Color ManaBlue = new Color32(95, 176, 255, 255);
        public static readonly Color Good = new Color32(110, 190, 90, 255);
        public static readonly Color Bad = new Color32(226, 92, 80, 255);
        public static readonly Color Light = new Color32(250, 244, 230, 255);
        public static readonly Color Muted = new Color32(150, 140, 130, 255);

        public static void Init()
        {
            if (_init) return;
            _init = true;
            Body = Resources.Load<Font>("Fonts/BeVietnamPro-SemiBold");
            Heading = Resources.Load<Font>("Fonts/BeVietnamPro-Black") ?? Body;
            Numbers = Resources.Load<Font>("Fonts/NotoSansMono-Bold") ?? Body;
            Display = Resources.Load<Font>("Fonts/YoungSerif-Regular") ?? Heading;

            Label = new GUIStyle { font = Body, fontSize = 26, wordWrap = true, richText = true, normal = { textColor = Ink } };
            LabelSmall = new GUIStyle(Label) { fontSize = 21 };
            LabelCenter = new GUIStyle(Label) { alignment = TextAnchor.MiddleCenter };
            Hint = new GUIStyle(Label) { fontSize = 22, normal = { textColor = Light }, alignment = TextAnchor.MiddleCenter };
            Title = new GUIStyle(Label) { font = Heading, fontSize = 64, alignment = TextAnchor.MiddleCenter, normal = { textColor = Light } };
            H1 = new GUIStyle(Label) { font = Heading, fontSize = 40 };
            H2 = new GUIStyle(Label) { font = Heading, fontSize = 30 };
            Number = new GUIStyle(Label) { font = Numbers, fontSize = 28, wordWrap = false, alignment = TextAnchor.MiddleLeft, normal = { textColor = Light } };
            NumberBig = new GUIStyle(Number) { fontSize = 40 };
            Panel = new GUIStyle { normal = { background = ArtLibrary.Tex("ui/panel") }, border = new RectOffset(28, 28, 28, 30), padding = new RectOffset(24, 24, 22, 24) };
            PanelDark = new GUIStyle(Panel) { normal = { background = ArtLibrary.Tex("ui/panel_dark") } };
            Tooltip = new GUIStyle(Panel) { normal = { background = ArtLibrary.Tex("ui/tooltip") }, border = new RectOffset(18, 18, 18, 18), padding = new RectOffset(16, 16, 12, 14) };
            Button = MakeButton("ui/button", 26);
            ButtonBig = MakeButton("ui/button", 34);
            ButtonSmall = MakeButton("ui/button", 21);
        }

        private static GUIStyle MakeButton(string key, int size)
        {
            return new GUIStyle
            {
                font = Heading, fontSize = size, alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true,
                border = new RectOffset(22, 22, 20, 24), padding = new RectOffset(16, 16, 8, 12),
                normal = { background = ArtLibrary.Tex(key), textColor = Ink },
                hover = { background = ArtLibrary.Tex("ui/button_hover"), textColor = Ink },
                active = { background = ArtLibrary.Tex("ui/button_down"), textColor = Ink },
            };
        }

        /// <summary>Call at the start of OnGUI.</summary>
        public static void Begin()
        {
            Init();
            Scale = Screen.height / 1080f * UiScaleSetting;
            // phones in landscape: keep text readable on small screens
            if (Screen.dpi > 200 && Screen.height < 900) Scale *= 1.1f;
            VW = Screen.width / Scale;
            VH = Screen.height / Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Scale, Scale, 1));
            var sa = Screen.safeArea;
            Safe = new Rect(sa.x / Scale, (Screen.height - sa.yMax) / Scale, sa.width / Scale, sa.height / Scale);
            if (Event.current.type == EventType.Layout) { }
            if (Event.current.type == EventType.Repaint)
            {
                Blockers.Clear();
                Blockers.AddRange(_blockersBuilding);
                _blockersBuilding.Clear();
            }
        }

        public static void Block(Rect r)
        {
            if (Event.current.type == EventType.Repaint) _blockersBuilding.Add(r);
        }

        /// <summary>True if a screen-space point (pixels, bottom-left origin) is over UI drawn last frame.</summary>
        public static bool OverUI(Vector2 screenPos)
        {
            var p = new Vector2(screenPos.x / Scale, (Screen.height - screenPos.y) / Scale);
            foreach (var r in Blockers) if (r.Contains(p)) return true;
            return false;
        }

        public static void DrawPanel(Rect r, bool dark = false)
        {
            Block(r);
            GUI.Box(r, GUIContent.none, dark ? PanelDark : Panel);
        }

        public static bool Btn(Rect r, string text, bool enabled = true, GUIStyle style = null, string variant = null)
        {
            Block(r);
            style = style ?? Button;
            var prev = GUI.enabled;
            GUI.enabled = enabled;
            GUIStyle s = style;
            if (!enabled)
            {
                s = new GUIStyle(style) { normal = { background = ArtLibrary.Tex("ui/button_off"), textColor = new Color(0.25f, 0.22f, 0.25f) } };
                s.hover = s.normal; s.active = s.normal;
            }
            else if (variant != null)
            {
                s = new GUIStyle(style) { normal = { background = ArtLibrary.Tex("ui/button_" + variant), textColor = Light } };
                s.hover = new GUIStyleState { background = ArtLibrary.Tex("ui/button_" + variant), textColor = Gold };
                s.active = s.hover;
            }
            bool clicked = GUI.Button(r, text, s);
            GUI.enabled = prev;
            if (clicked) AudioService.Ui();
            return clicked && enabled;
        }

        public static bool IconBtn(Rect r, string icon, string tooltip = null, bool enabled = true, bool selected = false)
        {
            Block(r);
            var bg = selected ? "ui/button_hover" : enabled ? "ui/button" : "ui/button_off";
            GUI.DrawTexture(r, ArtLibrary.Tex(bg), ScaleMode.StretchToFill);
            var tex = ArtLibrary.Tex(icon);
            float pad = r.height * 0.16f;
            if (tex != null) GUI.DrawTexture(new Rect(r.x + pad, r.y + pad * 0.7f, r.width - pad * 2, r.height - pad * 2), tex, ScaleMode.ScaleToFit);
            bool clicked = enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (clicked) AudioService.Ui();
            return clicked;
        }

        public static void Icon(Rect r, string key, Color? tint = null)
        {
            var tex = ArtLibrary.Tex(key);
            if (tex == null) return;
            var c = GUI.color;
            if (tint.HasValue) GUI.color = tint.Value;
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
            GUI.color = c;
        }

        public static void Text(Rect r, string s, GUIStyle style, Color? col = null, bool shadow = false)
        {
            if (shadow)
            {
                var sh = new GUIStyle(style) { normal = { textColor = new Color(0, 0, 0, 0.6f) } };
                GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, sh);
            }
            if (col.HasValue)
            {
                var st = new GUIStyle(style) { normal = { textColor = col.Value } };
                GUI.Label(r, s, st);
            }
            else GUI.Label(r, s, style);
        }

        public static void Bar(Rect r, float t, Color fill, string label = null)
        {
            GUI.DrawTexture(r, ArtLibrary.Tex("ui/bar_bg"), ScaleMode.StretchToFill);
            t = Mathf.Clamp01(t);
            if (t > 0.001f)
            {
                var c = GUI.color;
                GUI.color = fill;
                GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, (r.width - 6) * t, r.height - 6), ArtLibrary.Tex("ui/bar_fill"), ScaleMode.StretchToFill);
                GUI.color = c;
            }
            if (label != null)
            {
                var st = new GUIStyle(Number) { fontSize = Mathf.RoundToInt(r.height * 0.62f), alignment = TextAnchor.MiddleCenter };
                Text(r, label, st, Light, true);
            }
        }

        public static void Rect(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        public static string Money(int gold, int mana)
        {
            string s = "";
            if (gold != 0 || mana == 0) s += "<color=#f6c64a>" + gold + "</color> Vàng";
            if (mana != 0) s += (s.Length > 0 ? "  " : "") + "<color=#5fb0ff>" + mana + "</color> Mana";
            return s;
        }
    }
}

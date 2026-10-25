using System;
using UnityEngine;
using UnityEngine.UI;

namespace Oddyseus.UI
{
    // Helper bersama untuk Hub dan layar turunannya.
    public static class HubUI
    {
        public static Image Flat(Transform parent, string name, Color color, float x0, float y0, float x1, float y1, bool block = false)
        {
            var rt = UIFactory.NewRect(parent, name);
            UIFactory.Stretch(rt, x0, y0, x1, y1);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = block;   // hanya latar yang memblokir klik ke layar di belakang
            return img;
        }

        public static Image Card(Transform parent, string name, Color fill, float x0, float y0, float x1, float y1, Color frame)
        {
            var img = UIFactory.Panel(parent, name, fill, x0, y0, x1, y1, false);
            HubTheme.Frame(img, frame);
            return img;
        }

        public static void Disc(RectTransform parent, string name, Vector2 center, float diameter, Color color)
        {
            UIFactory.Circle(parent, name, center, diameter, color).raycastTarget = false;
        }

        public static void Tint(Button b, Color fill)
        {
            var cb = b.colors;
            cb.normalColor = fill;
            cb.selectedColor = fill;
            cb.highlightedColor = Color.Lerp(fill, HubTheme.Gold, 0.30f);
            cb.pressedColor = new Color(fill.r * 0.75f, fill.g * 0.75f, fill.b * 0.75f, 1f);
            b.colors = cb;
        }

        public static string Initials(string s)
        {
            if (string.IsNullOrEmpty(s)) return "?";
            var parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
            return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpper();
        }

        // Tombol bergaya Hub. sub == null: judul di tengah. sub != null: judul + keterangan + garis emas di kiri.
        public static Button Btn(Transform parent, string name, string title, string sub, Color fill, Color textColor,
                                 float x0, float y0, float x1, float y1, Action onClick, int fontSize)
        {
            var img = UIFactory.Panel(parent, name, Color.white, x0, y0, x1, y1, false);
            HubTheme.Frame(img, HubTheme.Bronze);

            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            Tint(b, fill);
            var cb = b.colors;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            img.gameObject.AddComponent<ButtonFx>();
            if (onClick != null) b.onClick.AddListener(() => onClick());

            if (sub == null)
            {
                UIFactory.Label(img.transform, title, fontSize, textColor, TextAnchor.MiddleCenter, 0.06f, 0.10f, 0.94f, 0.90f);
            }
            else
            {
                Flat(img.transform, "Accent", HubTheme.Gold, 0f, 0.12f, 0.025f, 0.88f);
                UIFactory.Label(img.transform, title, fontSize, textColor, TextAnchor.MiddleLeft, 0.08f, 0.40f, 0.96f, 0.92f);
                UIFactory.Label(img.transform, sub, 14, HubTheme.Muted, TextAnchor.MiddleLeft, 0.08f, 0.08f, 0.96f, 0.42f);
            }
            return b;
        }
    }
}
using UnityEngine;
using UnityEngine.EventSystems;

namespace Oddyseus.UI
{
    // Efek hover/tekan pada tombol.
    public class ButtonFx : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        float target = 1f;
        bool hover;

        public void OnPointerEnter(PointerEventData e) { hover = true; target = 1.04f; }
        public void OnPointerExit(PointerEventData e) { hover = false; target = 1f; }
        public void OnPointerDown(PointerEventData e) { target = 0.96f; }
        public void OnPointerUp(PointerEventData e) { target = hover ? 1.04f : 1f; }

        void OnDisable()
        {
            hover = false;
            target = 1f;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float cur = transform.localScale.x;
            if (Mathf.Abs(cur - target) < 0.001f)
            {
                if (cur != target) transform.localScale = new Vector3(target, target, 1f);
                return;
            }
            float s = Mathf.Lerp(cur, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    // Fade + zoom kecil saat panel muncul. Menghapus dirinya sendiri setelah selesai.
    public class PopInFx : MonoBehaviour
    {
        const float Duration = 0.18f;
        const float StartScale = 0.94f;
        CanvasGroup cg;
        float t;

        void Awake()
        {
            cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            transform.localScale = Vector3.one * StartScale;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Duration);
            k = 1f - (1f - k) * (1f - k);
            cg.alpha = k;
            transform.localScale = Vector3.one * Mathf.Lerp(StartScale, 1f, k);
            if (t >= Duration) Destroy(this);
        }
    }

    public static class UIEffects
    {
        public static void PopIn(Component c)
        {
            if (c != null && c.GetComponent<PopInFx>() == null) c.gameObject.AddComponent<PopInFx>();
        }
    }
}
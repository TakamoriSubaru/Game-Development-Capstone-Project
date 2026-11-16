using System;
using UnityEngine;
using UnityEngine.UI;

namespace Oddyseus.UI
{
    public enum PageId { Hub, Legends, Compendium, Armory, Achievements }

    // Pasang di satu GameObject kosong di scene DungeonHub (dibuat menu Oddyseus > Build Hub Scene).
    // Seluruh UI dibuat lewat kode, sama seperti DungeonController.
    public class HubController : MonoBehaviour
    {
        RectTransform canvas, modalLayer;
        Page[] pages;

        void Awake()
        {
            UIFactory.EnsureEventSystem();
            canvas = UIFactory.CreateCanvas("HubCanvas");
            pages = new Page[5];
            pages[(int)PageId.Hub] = Make(new HubPage(), "Hub", true, true, true, false);
            pages[(int)PageId.Legends] = Make(new LegendsPage(), "Legends", true, false, true, false);
            pages[(int)PageId.Compendium] = Make(new CompendiumPage(), "Compendium", true, true, false, false);
            pages[(int)PageId.Armory] = Make(new ArmoryPage(), "Armory", true, false, true, true);
            pages[(int)PageId.Achievements] = Make(new AchievementsPage(), "Achievements", true, true, false, false);

            modalLayer = UIFactory.NewRect(canvas, "ModalLayer");
            UIFactory.Stretch(modalLayer, 0f, 0f, 1f, 1f);
            modalLayer.gameObject.SetActive(false);

            Go(PageId.Hub);
        }

        Page Make(Page p, string name, bool tr, bool bl, bool br, bool left)
        {
            p.Init(canvas, this, name, tr, bl, br, left);
            return p;
        }

        public void Go(PageId id)
        {
            for (int i = 0; i < pages.Length; i++) pages[i].Show(i == (int)id);
        }

        public void RefreshCurrent()
        {
            for (int i = 0; i < pages.Length; i++) if (pages[i].Root.gameObject.activeSelf) pages[i].Refresh();
        }

        public void BackToLobby() { SceneFader.LoadScene(GameScenes.Lobby, 0.5f); }
        public void OpenDungeon() { SceneFader.LoadScene(GameScenes.Dungeon, 0.5f); }

        // ---------- dialog ----------

        public void CloseModal()
        {
            UIFactory.ClearChildren(modalLayer);
            modalLayer.gameObject.SetActive(false);
        }

        public void ShowModal(string title, string body, string[] labels, Action[] actions)
        {
            UIFactory.ClearChildren(modalLayer);
            modalLayer.gameObject.SetActive(true);
            modalLayer.SetAsLastSibling();
            var dim = UIFactory.Panel(modalLayer, "Dim", Theme.Dim, 0f, 0f, 1f, 1f, false);
            dim.raycastTarget = true;   // blokir klik ke layar di belakang dialog
            var safe = Ui.SafeArea(modalLayer);

            Ui.Panel9(safe, "Box", 0.28f, 0.27f, 0.72f, 0.73f, 0.5f).raycastTarget = true;
            var box = safe.Find("Box");
            Ui.Txt(box, title, 30, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.10f, 0.70f, 0.90f, 0.88f, true);
            Ui.Txt(box, body, 20, Theme.DarkBrown, TextAnchor.MiddleCenter, 0.12f, 0.36f, 0.88f, 0.70f);
            int n = labels.Length;
            float w = 0.76f / n;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                float x0 = 0.12f + i * w + 0.015f, x1 = 0.12f + (i + 1) * w - 0.015f;
                bool primary = i == n - 1;
                Ui.Pill(box, "Btn" + i, labels[i], primary ? "parchment" : "alt", primary ? Ui.GoldTint : Color.white,
                        x0, 0.13f, x1, 0.30f, () => { CloseModal(); if (actions[k] != null) actions[k](); }, 22);
            }
        }
    }
}

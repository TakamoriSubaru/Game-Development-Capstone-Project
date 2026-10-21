using System;
using UnityEngine;

namespace OddyseusJourneysEnd.Party
{
    /// <summary>
    /// Health system yang bisa dipakai ulang (party member, enemy, boss, dll).
    /// Class biasa (bukan MonoBehaviour): simpan sebagai field di PartyMember / komponen enemy.
    /// Ada "Block" (armor sementara ala Slay the Spire) yang menyerap damage sebelum HP.
    /// </summary>
    [Serializable]
    public class HealthPoint
    {
        [SerializeField, Min(1)] private int max = 10;
        [SerializeField, Min(0)] private int current = 10;
        [SerializeField, Min(0)] private int block = 0;

        /// <summary>Dipanggil setiap HP / Max / Block berubah (untuk refresh UI).</summary>
        public event Action<HealthPoint> Changed;
        /// <summary>Jumlah HP yang benar-benar hilang (setelah Block).</summary>
        public event Action<int> Damaged;
        /// <summary>Jumlah HP yang benar-benar pulih.</summary>
        public event Action<int> Healed;
        public event Action Died;
        public event Action Revived;

        public HealthPoint() { }

        public HealthPoint(int maxHp, int currentHp = -1)
        {
            Initialize(maxHp, currentHp);
        }

        // ---------- Info ----------
        public int Max => max;
        public int Current => current;
        public int Block => block;
        public bool IsDead => current <= 0;
        public bool IsFull => current >= max;
        public float Ratio => max <= 0 ? 0f : (float)current / max;

        public HealthState State
        {
            get
            {
                if (current <= 0) return HealthState.Down;
                float r = Ratio;
                if (r <= 0.3f) return HealthState.Critical;
                if (r <= 0.7f) return HealthState.Wounded;
                return HealthState.Healthy;
            }
        }

        // ---------- Setup ----------
        /// <summary>Set awal. currentHp &lt; 0 berarti HP penuh.</summary>
        public void Initialize(int maxHp, int currentHp = -1)
        {
            max = Mathf.Max(1, maxHp);
            current = currentHp < 0 ? max : Mathf.Clamp(currentHp, 0, max);
            block = 0;
            Changed?.Invoke(this);
        }

        /// <summary>Dipakai saat load save. Hanya memicu Changed (bukan Died/Healed/Damaged).</summary>
        public void Restore(int maxHp, int currentHp, int blockAmount = 0)
        {
            max = Mathf.Max(1, maxHp);
            current = Mathf.Clamp(currentHp, 0, max);
            block = Mathf.Max(0, blockAmount);
            Changed?.Invoke(this);
        }

        public void SetMax(int newMax, bool healByDifference = false)
        {
            newMax = Mathf.Max(1, newMax);
            int diff = newMax - max;
            max = newMax;
            if (healByDifference && diff > 0 && !IsDead) current += diff;
            current = Mathf.Min(current, max);
            Changed?.Invoke(this);
        }

        // ---------- Damage / Heal ----------
        /// <summary>Return jumlah HP yang hilang. Block menyerap dulu kecuali ignoreBlock.</summary>
        public int TakeDamage(int amount, bool ignoreBlock = false)
        {
            if (IsDead || amount <= 0) return 0;

            if (!ignoreBlock && block > 0)
            {
                int absorbed = Mathf.Min(block, amount);
                block -= absorbed;
                amount -= absorbed;
            }

            int lost = Mathf.Min(current, amount);
            current -= lost;

            if (lost > 0) Damaged?.Invoke(lost);
            Changed?.Invoke(this);
            if (lost > 0 && current <= 0) Died?.Invoke();
            return lost;
        }

        /// <summary>Return jumlah HP yang pulih. Karakter yang sudah Down tidak bisa di-Heal (pakai Revive).</summary>
        public int Heal(int amount)
        {
            if (IsDead || amount <= 0) return 0;

            int gained = Mathf.Min(max - current, amount);
            if (gained <= 0) return 0;

            current += gained;
            Healed?.Invoke(gained);
            Changed?.Invoke(this);
            return gained;
        }

        public void HealFull() => Heal(max - current);

        public void Revive(int hp = 1)
        {
            if (!IsDead) return;
            current = Mathf.Clamp(hp, 1, max);
            Revived?.Invoke();
            Changed?.Invoke(this);
        }

        // ---------- Block ----------
        public void AddBlock(int amount)
        {
            if (amount <= 0) return;
            block += amount;
            Changed?.Invoke(this);
        }

        public void ClearBlock()
        {
            if (block == 0) return;
            block = 0;
            Changed?.Invoke(this);
        }
    }
}

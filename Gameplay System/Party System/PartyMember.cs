using System;
using System.Collections.Generic;
using System.Linq;
using OddyseusJourneysEnd.Save;
using UnityEngine;

namespace OddyseusJourneysEnd.Party
{
    /// <summary>Data save untuk satu karakter (dipakai JsonUtility).</summary>
    [Serializable]
    public class PartyMemberSave
    {
        public string characterId;                    // nama asset CharacterData
        public int currentHp;
        public List<string> equippedMoveIds = new List<string>(); // 4 slot, "" = kosong
        public List<string> collectionIds = new List<string>();
    }

    /// <summary>State runtime sebuah karakter. HP dikelola oleh HealthPoint.</summary>
    public class PartyMember
    {
        public const int MaxEquippedMoves = 4;

        public CharacterData Data { get; }
        public HealthPoint Health { get; }

        private readonly List<MoveData> _collection = new List<MoveData>();
        private readonly MoveData[] _equipped = new MoveData[MaxEquippedMoves];

        /// <summary>Dipanggil tiap ada perubahan (HP, equip, unequip, learn move).</summary>
        public event Action<PartyMember> Changed;

        public PartyMember(CharacterData data)
        {
            Data = data;
            Health = new HealthPoint(data.maxHp);
            Health.Changed += _ => Changed?.Invoke(this);

            _collection.AddRange(data.moveCollection.Where(m => m != null).Distinct());
            for (int i = 0; i < Mathf.Min(MaxEquippedMoves, _collection.Count); i++)
                _equipped[i] = _collection[i];
        }

        // ---------- Info ----------
        public string Name => Data.DisplayName;
        public CharacterRole Role => Data.role;
        public int CurrentHp => Health.Current;
        public int MaxHp => Health.Max;
        public bool IsDown => Health.IsDead;
        public HealthState State => Health.State;

        // ---------- HP (diteruskan ke HealthPoint supaya kode UI lama tetap jalan) ----------
        public int TakeDamage(int amount) => Health.TakeDamage(amount);
        public int Heal(int amount) => Health.Heal(amount);

        // ---------- Moves ----------
        public IReadOnlyList<MoveData> Collection => _collection;

        /// <summary>Move yang dimiliki tapi belum dipasang.</summary>
        public IEnumerable<MoveData> UnequippedMoves => _collection.Where(m => !IsEquipped(m));

        public MoveData GetEquipped(int slot) =>
            slot >= 0 && slot < MaxEquippedMoves ? _equipped[slot] : null;

        public bool IsEquipped(MoveData move) => Array.IndexOf(_equipped, move) >= 0;

        public void LearnMove(MoveData move)
        {
            if (move == null || _collection.Contains(move)) return;
            _collection.Add(move);
            Changed?.Invoke(this);
        }

        /// <summary>slot = -1 -> slot kosong pertama. Slot terisi -> move lama diganti.</summary>
        public bool TryEquip(MoveData move, int slot = -1)
        {
            if (move == null || !_collection.Contains(move) || IsEquipped(move)) return false;

            if (slot < 0) slot = Array.IndexOf(_equipped, null);
            if (slot < 0 || slot >= MaxEquippedMoves) return false;

            _equipped[slot] = move;
            Changed?.Invoke(this);
            return true;
        }

        public bool Unequip(int slot)
        {
            if (slot < 0 || slot >= MaxEquippedMoves || _equipped[slot] == null) return false;
            _equipped[slot] = null;
            Changed?.Invoke(this);
            return true;
        }

        // ---------- Save / Load ----------
        public PartyMemberSave CaptureState()
        {
            var s = new PartyMemberSave
            {
                characterId = Data.name,
                currentHp = Health.Current,
            };
            foreach (var m in _collection) s.collectionIds.Add(m.name);
            for (int i = 0; i < MaxEquippedMoves; i++)
                s.equippedMoveIds.Add(_equipped[i] != null ? _equipped[i].name : "");
            return s;
        }

        /// <summary>Buat PartyMember dari data save. Return null jika asset karakter tidak ketemu.</summary>
        public static PartyMember FromSave(PartyMemberSave s)
        {
            var data = AssetLookup.Find<CharacterData>(s.characterId);
            if (data == null)
            {
                Debug.LogWarning($"[Save] CharacterData '{s.characterId}' tidak ditemukan. Pastikan asset ada di folder Resources.");
                return null;
            }

            var member = new PartyMember(data);
            member.ApplySave(s);
            return member;
        }

        private void ApplySave(PartyMemberSave s)
        {
            _collection.Clear();
            Array.Clear(_equipped, 0, _equipped.Length);

            foreach (var id in s.collectionIds)
            {
                var move = AssetLookup.Find<MoveData>(id);
                if (move != null && !_collection.Contains(move)) _collection.Add(move);
            }

            for (int i = 0; i < MaxEquippedMoves && i < s.equippedMoveIds.Count; i++)
            {
                var move = AssetLookup.Find<MoveData>(s.equippedMoveIds[i]);
                if (move != null && _collection.Contains(move) && !IsEquipped(move))
                    _equipped[i] = move;
            }

            Health.Restore(Data.maxHp, s.currentHp);
        }
    }
}

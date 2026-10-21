using System;
using System.Collections.Generic;
using OddyseusJourneysEnd.Save;
using UnityEngine;

namespace OddyseusJourneysEnd.Party
{
    /// <summary>
    /// Menyimpan seluruh karakter (roster) dan party aktif (maks 4).
    /// Juga ISaveable: ikut tersimpan / ter-reset oleh SavePoint.
    /// </summary>
    public class PartyManager : MonoBehaviour, ISaveable
    {
        public static PartyManager Instance { get; private set; }

        public const int MaxActive = 4;
        public const int MinActive = 1;

        [SerializeField] private List<CharacterData> startingRoster = new List<CharacterData>();
        [SerializeField] private bool persistAcrossScenes = true;

        private readonly List<PartyMember> _roster = new List<PartyMember>();
        private readonly List<PartyMember> _active = new List<PartyMember>();

        public IReadOnlyList<PartyMember> Roster => _roster;
        public IReadOnlyList<PartyMember> Active => _active;

        public event Action PartyChanged;

        [Serializable]
        public class PartySave
        {
            public List<PartyMemberSave> roster = new List<PartyMemberSave>();
            public List<int> active = new List<int>(); // index di roster
        }

        // ---------- Lifecycle ----------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

            BuildStartingParty(notify: false);
        }

        private void OnEnable()
        {
            if (Instance == this) SaveSystem.Register(this);
        }

        private void OnDisable()
        {
            if (Instance == this) SaveSystem.Unregister(this);
        }

        private void BuildStartingParty(bool notify)
        {
            ClearAll();

            foreach (var data in startingRoster)
                if (data != null) AddToRoster(data, notify: false);

            for (int i = 0; i < Mathf.Min(MaxActive, _roster.Count); i++)
                _active.Add(_roster[i]);

            if (notify) PartyChanged?.Invoke();
        }

        private void ClearAll()
        {
            foreach (var m in _roster) m.Changed -= OnMemberChanged;
            _roster.Clear();
            _active.Clear();
        }

        // ---------- Roster ----------
        public PartyMember AddToRoster(CharacterData data, bool notify = true)
        {
            var member = new PartyMember(data);
            member.Changed += OnMemberChanged;
            _roster.Add(member);
            if (notify) PartyChanged?.Invoke();
            return member;
        }

        private void OnMemberChanged(PartyMember _) => PartyChanged?.Invoke();

        // ---------- Party aktif ----------
        public bool IsActive(PartyMember member) => _active.Contains(member);

        public bool TryAddToActive(PartyMember member)
        {
            if (member == null || !_roster.Contains(member)) return false;
            if (_active.Contains(member) || _active.Count >= MaxActive) return false;
            _active.Add(member);
            PartyChanged?.Invoke();
            return true;
        }

        public bool RemoveFromActive(PartyMember member)
        {
            if (!_active.Contains(member) || _active.Count <= MinActive) return false;
            _active.Remove(member);
            PartyChanged?.Invoke();
            return true;
        }

        public bool ToggleActive(PartyMember member) =>
            IsActive(member) ? RemoveFromActive(member) : TryAddToActive(member);

        public void ReorderActive(int from, int to)
        {
            if (from < 0 || from >= _active.Count || to < 0 || to >= _active.Count || from == to) return;
            var m = _active[from];
            _active.RemoveAt(from);
            _active.Insert(to, m);
            PartyChanged?.Invoke();
        }

        // ---------- ISaveable ----------
        public string SaveKey => "party";

        public string CaptureState()
        {
            var save = new PartySave();
            foreach (var m in _roster) save.roster.Add(m.CaptureState());
            foreach (var m in _active) save.active.Add(_roster.IndexOf(m));
            return JsonUtility.ToJson(save);
        }

        public void RestoreState(string json)
        {
            var save = JsonUtility.FromJson<PartySave>(json);
            if (save == null) return;

            ClearAll();

            // Simpan pemetaan index lama -> member baru, supaya aman kalau ada asset yang hilang
            var byIndex = new Dictionary<int, PartyMember>();
            for (int i = 0; i < save.roster.Count; i++)
            {
                var member = PartyMember.FromSave(save.roster[i]);
                if (member == null) continue;
                member.Changed += OnMemberChanged;
                _roster.Add(member);
                byIndex[i] = member;
            }

            foreach (int idx in save.active)
                if (byIndex.TryGetValue(idx, out var m) && !_active.Contains(m) && _active.Count < MaxActive)
                    _active.Add(m);

            if (_active.Count == 0 && _roster.Count > 0) _active.Add(_roster[0]);

            PartyChanged?.Invoke();
        }

        /// <summary>Dipanggil saat Restart: kembali ke party awal dan HP penuh.</summary>
        public void ResetState() => BuildStartingParty(notify: true);
    }
}

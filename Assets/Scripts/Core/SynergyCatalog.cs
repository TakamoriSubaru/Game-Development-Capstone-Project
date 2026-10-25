using System.Collections.Generic;

namespace Oddyseus.Core
{
    // Members = hanya karakter yang disebut di Requires yang dapat bonus. Party = seluruh party yang turun ke battle.
    public enum SynergyScope { Members, Party }

    public class SynergyInfo
    {
        public string Id;
        public string Name;
        public string Desc;
        public string[] Requires;      // UnitData.Id. Aktif kalau SEMUA id ini ada di party yang turun ke battle.
        public SynergyScope Scope;
        public StatMod[] Mods;

        public string ModsLabel
        {
            get
            {
                var parts = new List<string>();
                for (int i = 0; i < Mods.Length; i++) parts.Add(Mods[i].Label);
                return string.Join(", ", parts.ToArray());
            }
        }
    }

    // Sinergi berbasis komposisi: aktif otomatis kalau kombinasi karakter tertentu turun bersama.
    // MENAMBAH SINERGI = tambah satu baris S(...) di All. Tidak perlu kode lain.
    //   S(id, nama, deskripsi, new[] { "unitIdA", "unitIdB" }, scope, new StatMod(StatKey.Atk, 0.15f), ...)
    // Id karakter: odysseus, polites, eurylochus, elpenor, perimedes, eurybates.
    // Semua isi = DRAF untuk disetujui Designer 1.
    public static class SynergyCatalog
    {
        static SynergyInfo S(string id, string name, string desc, string[] requires, SynergyScope scope, params StatMod[] mods)
        {
            return new SynergyInfo { Id = id, Name = name, Desc = desc, Requires = requires, Scope = scope, Mods = mods };
        }

        public static readonly SynergyInfo[] All =
        {
            S("spear_wall", "Spear Wall", "Odysseus and Eurybates hold the line together.",
              new[] { "odysseus", "eurybates" }, SynergyScope.Members, new StatMod(StatKey.Atk, 0.15f)),

            S("ithaca_bond", "Ithaca's Bond", "Odysseus and Polites steady the whole crew.",
              new[] { "odysseus", "polites" }, SynergyScope.Party, new StatMod(StatKey.Def, 0.10f)),

            S("menders_circle", "Mender's Circle", "Perimedes and Polites guard the wounded.",
              new[] { "perimedes", "polites" }, SynergyScope.Party, new StatMod(StatKey.Heal, 0.25f)),

            S("doomed_crew", "Doomed Crew", "Eurylochus and Elpenor fight with nothing to lose.",
              new[] { "eurylochus", "elpenor" }, SynergyScope.Members, new StatMod(StatKey.Atk, 0.20f), new StatMod(StatKey.Def, -0.10f)),

            S("voyage_trio", "Wind at Our Backs", "Odysseus, Perimedes and Eurylochus move as one.",
              new[] { "odysseus", "perimedes", "eurylochus" }, SynergyScope.Party, new StatMod(StatKey.Speed, 1f)),
        };

        public static List<SynergyInfo> Resolve(IList<Unit> allies)
        {
            var present = new HashSet<string>();
            for (int i = 0; i < allies.Count; i++)
                if (allies[i].Present) present.Add(allies[i].Data.Id);

            var active = new List<SynergyInfo>();
            for (int i = 0; i < All.Length; i++)
            {
                bool ok = true;
                for (int k = 0; k < All[i].Requires.Length; k++)
                    if (!present.Contains(All[i].Requires[k])) { ok = false; break; }
                if (ok) active.Add(All[i]);
            }
            return active;
        }

        public static void Apply(IList<Unit> allies, IList<SynergyInfo> active)
        {
            for (int s = 0; s < active.Count; s++)
            {
                var syn = active[s];
                for (int i = 0; i < allies.Count; i++)
                {
                    bool member = System.Array.IndexOf(syn.Requires, allies[i].Data.Id) >= 0;
                    if (syn.Scope == SynergyScope.Members && !member) continue;
                    for (int m = 0; m < syn.Mods.Length; m++) allies[i].AddMod(syn.Mods[m]);
                }
            }
        }
    }
}

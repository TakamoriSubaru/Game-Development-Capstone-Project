using System.Collections.Generic;

namespace Oddyseus.Core
{
    // Jenis aksi musuh. Kabur dan CallReinforcements adalah aksi umum (selalu ada, tapi dibatasi syarat).
    public enum EnemyActionType { Attack, Defend, Buff, Debuff, Heal, Steal, Flee, CallReinforcements }

    // Peran musuh. Debuffer dan Thief boleh kabur walau masih punya teman.
    public enum EnemyRole { Fighter, Debuffer, Thief }

    // Bobot pilihan aksi satu jenis musuh. Bobot 0 = aksi itu tidak dimiliki.
    // Attack, Buff, Debuff, Heal hanya masuk undian kalau musuh punya skill dengan jenis itu di UnitData.Skills.
    // Defend = Guard ke diri sendiri (tidak butuh skill). Steal butuh bobot > 0.
    public class EnemyBehavior
    {
        public EnemyRole Role = EnemyRole.Fighter;
        public float Attack = 4f, Defend = 1f, Buff = 0f, Debuff = 0f, Heal = 0f, Steal = 0f;
        public float Flee = 0.4f;            // dipakai hanya kalau syarat kabur terpenuhi
        public bool CanFlee = true;          // false untuk boss
        public bool CanCallReinforcements = true;
    }

    // DATA PLACEHOLDER. Ubah bobot di sini sampai terasa pas. Kunci = UnitData.Id musuh.
    public static class EnemyCatalog
    {
        static readonly EnemyBehavior Default = new EnemyBehavior();

        public static EnemyBehavior BehaviorOf(string id)
        {
            switch (id)
            {
                case "cicone_warrior": return new EnemyBehavior { Attack = 5, Defend = 2 };
                case "cicone_archer": return new EnemyBehavior { Attack = 5, Defend = 1 };
                case "cicone_chief": return new EnemyBehavior { Attack = 4, Defend = 2, Buff = 2 };
                case "sea_raider": return new EnemyBehavior { Attack = 5, Defend = 1 };
                case "polyphemus": return new EnemyBehavior { Attack = 5, Defend = 1, Buff = 1, CanFlee = false, CanCallReinforcements = false };
                case "shade": return new EnemyBehavior { Role = EnemyRole.Debuffer, Attack = 3, Defend = 1, Debuff = 2 };
                case "wild_beast": return new EnemyBehavior { Attack = 5, Defend = 1 };
                case "thief": return new EnemyBehavior { Role = EnemyRole.Thief, Attack = 2, Defend = 1, Steal = 4 };
            }
            return Default;
        }
    }
}

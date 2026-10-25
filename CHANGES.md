# Battle update (drop into Assets/Scripts, overwrite when asked)

Before copying: DELETE the folder Assets/Scripts/Battle (old test files, they cause the CS0246 errors).

New files: Core/StatMod.cs, Core/SynergyCatalog.cs, Core/RelicEffects.cs
Changed:   Core/Unit.cs, Core/UnitData.cs, Core/GameData.cs, Core/BattleSystem.cs, Core/RunState.cs,
           UI/Battleview.cs, UI/PartyView.cs, UI/LegendsView.cs

## What changed
- Speed is a range per character (SpeedMin-SpeedMax in GameData.Make). Rolled every round. Odysseus 3-7.
- Synergy: Core/SynergyCatalog.cs. One S(...) line per synergy. Active when all listed character ids are in the party.
- Relics: top bar already shows DungeonState.Equipment. Core/RelicEffects.cs maps relic name -> stat effects, applied in BattleView.Begin.
- BattleView: SPD on turn-order chips, synergy list in the bottom-right box, SPD range in the HUD.
- Battle text is English (logs, UI, skill descriptions, encounter names, run notes).
- Blessing already costs the acting hero's turn (BattleSystem.UseBlessing). Unchanged.

## Add a synergy
S("id", "Name", "Description", new[] { "odysseus", "eurybates" }, SynergyScope.Members, new StatMod(StatKey.Atk, 0.15f)),
StatKey: Atk, Def, Heal (percent, 0.15 = +15%), Speed (flat).

## Battle screen layout (Battleview.cs)
Left: turn order (vertical, with SPD) + Synergy. Center: stage + hero HUD/skills. Right: Blessings + log.
Relics stay in the top bar. Not previewed in Unity: adjust the anchor numbers in BattleView.Build() if something overlaps.

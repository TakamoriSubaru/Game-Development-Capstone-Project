# Battle System

Unity (C#) turn-based battle for Dungeon Mode. All content is data (ScriptableObjects), so new units, synergies and relics need no new code.

## Rules
- **One action per turn.** No AP. A unit uses one skill, or invokes the Blessing of the Gods (which uses up that hero's action).
- **Limbus-style speed.** Each unit has a min and max speed (Odysseus 3-7). Every round each unit rolls a number in its range. Highest goes first, ties are random.
- **Cooldown.** `cooldown = N` means the unit cannot use that skill for its next N turns.
- **Brace.** Halves damage taken until the unit's next turn.
- **Bleed.** Flat damage at the start of the bleeding unit's turn.
- **Blessing.** Heals all living allies by a percent of max HP. Uses per battle is set on the `BlessingData`.
- **Relics.** Collected in `BattleRunState.relics` for the whole run (Slay the Spire style). Show them in a row at the top of the screen. Effects are stat modifiers on the party, plus an optional heal at battle start.
- **Synergies.** Composition based. A synergy is active when every id in `requiredUnitIds` is in the party (and alive, if `requireMembersAlive`).

## Files
| Path | Role |
|---|---|
| `Data/*` | ScriptableObject definitions: Skill, Unit, Synergy, Relic, Blessing |
| `Core/BattleManager.cs` | The battle loop. UI calls `StartBattle`, `UseSkill`, `UseBlessing` and listens to events |
| `Core/BattleUnit.cs` | Runtime unit: HP, speed roll, cooldowns, stat modifiers |
| `Core/SynergyResolver.cs` | Decides which synergies are active and applies them |
| `Core/BattleRunState.cs` | Party, relics, Men / Food / Gold across a run |
| `Debug/BattleConsoleDriver.cs` | Auto-playing test harness, logs to Console |
| `Editor/OdysseySampleDataGenerator.cs` | Menu `Odyssey > Generate Sample Battle Data` |

## Try it
1. Copy `Assets/` into the Unity project and let it compile.
2. Menu `Odyssey > Generate Sample Battle Data`.
3. New scene, empty GameObject with `BattleManager` and `BattleConsoleDriver`.
4. Assign the generated assets in the driver (4 heroes, 1-4 enemies, all 3 synergies, some relics, the blessing) and press Play.

## Adding a synergy
In the generator, copy a `MakeSynergy` line, or use `Assets > Create > Odyssey > Synergy`. Fields: `requiredUnitIds`, `scope`, `modifiers`.

Modifier math: `final = (base + flat) * (1 + percent)`. `percent` 0.15 = +15%. Use `Flat` for `CritChance` (0.2 = +20 points) and `Percent` for `HealPower` (base 1.0).

Prompt template for an AI helper:
> Add a synergy to `OdysseySampleDataGenerator.cs` with `MakeSynergy`. Name: X. Requires unit ids: a, b. Scope: AllParty. Effect: +N% to STAT. Use only the `StatType` values in `BattleTypes.cs`.

## Known gaps
- No UI yet. Subscribe to the events on `BattleManager` (`RoundStarted`, `TurnStarted`, `Damaged`, `Healed`, `UnitDied`, `SynergiesChanged`, `BattleEnded`).
- Relics are stat modifiers only. Add triggered relics (on kill, on crit) by adding events and a relic effect type.
- Enemy AI is weighted random.

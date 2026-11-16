# UI pass: parchment redesign (branch `ui-parchment-redesign`)

Scope so far: theme, asset import, and the five Dungeon Mode hub screens. Battle screen and attack cinematic are NOT done yet.

## How to run
1. Open the project in Unity 6. Assets import automatically (`UIAssetImporter` sets Sprite type + 9-slice borders).
2. `HubSceneBuilder` creates `Assets/_Project/Scenes/DungeonHub.unity` and adds it to Build Settings (or run **Oddyseus > Build Hub Scene**).
3. Play from Title > Lobby > click the map (Dungeon Mode), or open DungeonHub directly.
4. In the Editor/debug builds, the hub shows **Unlock all / Reset meta** to preview every state.

## Where things are
- Theme: `Scripts/UI/UIFactory.cs` (`Theme`). Sprites/fonts: `UISprites.cs`. Widgets: `UiKit.cs`.
- Screens: `Scripts/UI/Hub/` (HubPage, LegendsPage, CompendiumPage, ArmoryPage, AchievementsPage).
- Display data: `Scripts/UI/Content/`. `RelicCatalog.cs` is generated from `docs/ui-assets/manifest.json`.
- Art: `Assets/_Project/Resources/UI/**` (loaded by path with Resources.Load). Fonts: Cinzel + EB Garamond (OFL, licences alongside).

## Known placeholders / open decisions
- Relic Stat / Effect / Source pack: no data in repo -> shown as "-". Fill `RelicInfo.Stat` etc.
- Blessings: 8 from GDD 6.2; Dionysus/Aphrodite/Hestia/Nemesis are TBD. Stamina cost = 1 is an ASSUMPTION.
- Achievements: 16 rows are placeholders (GDD has none). Only two have live progress; the rest show NOT ACTIVE YET.
- Costumes: 3 per legend is a placeholder; only the default costume counts as owned.
- Floor 1 names: code (`PackCatalog`) is the single source for hub + compendium. Asset manifest differs on 3 packs
  (Cyclops Cave / Cave of Polyphemus, Triton's Strait / Triton Strait, Aeolus's Isle / Isle of Aeolus).
- Cyclops/other pack discovery and clears are not hooked to gameplay (no gameplay code was changed).
- Fonts are variable TTFs; Unity uses the default weight, bold is synthesised.

# Game-Development-Capstone-Project

## Oddyseus: Journey's End

A turn-based roguelite retelling of Homer's *Odyssey*. The player leads Odysseus and his crew home from Troy to Ithaca.

- **Engine / language:** Unity, C#
- **Platforms:** PC, Android
- **Modes:**
  - **Dungeon Mode** (roguelite): pick a party of 4, sail a map of 9 islands in 2 phases (Battle, Event and Resource islands, 2 Rest Stops, 1 Boss), and manage Men, Food and Gold.
  - **Campaign Mode**: an episodic, scripted retelling of the Odyssey where characters are unlocked.
- **No gacha, no leveling.** Power comes from strategy and resource management.

Current priority: a playable core loop in Dungeon Mode, then Campaign.

## Repository layout

```
docs/
  GDD_Oddyseus_Journeys_End_v0.7.pdf   current Game Design Document
  moodboard/                           mood board deck for art and sound
  planning/                            Gantt chart
  archive/                             older document versions (kept for history)
Assets/, Packages/, ProjectSettings/   Unity project (added when the project is created)
```

Open the repository root as a project in Unity Hub. The `.gitignore` is the standard Unity template, so `Library/`, `Temp/` and build output are not committed.

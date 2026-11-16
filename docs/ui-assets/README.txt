ODDYSEUS: JOURNEY'S END - UI ASSETS (Warm Parchment Gouache)
Cut from the generated sheets on 2026-10-30.

FOLDERS
backgrounds/   parchment_base.png + 5 corner vignettes (transparent, layer on top of the base)
hub/           compass_medallion.png (transparent, centre disc empty - add the title as text/title art)
portraits/     9 legends: portrait_<name>.png (square) and _round.png (gold ring, transparent); portrait_locked_round.png
packs/         20 pack paintings (tall), pack_01 ... pack_20, in floor order
scrolls/       scroll_normal / hover / undiscovered / cleared (art window is transparent) + scroll_layout.json (window rectangles in pixels)
packs_scroll/  20 ready-made scrolls: pack art already inside the normal scroll (no name text - draw names in the game)
relics/        60 relic/equipment icons: relic_<nn>_<code>.png, 384x384
relic_frames/  frame_common / uncommon / rare / legendary (transparent)
blessings/     12 god emblems: blessing_<god>.png (square tile) and _round.png (transparent circle)
icons/         menu, role, stat, resource and UI icons (transparent)
frames/        panel, card, row, menu button and item-slot frames (use as 9-slice, centres are flat)
buttons/       pill buttons (3 sets x normal/hover/pressed/disabled), tab buttons
ribbons/       name ribbons (terracotta, olive, gold, blue-grey)
bars/          progress bars, scroll bar parts, small arrows
chips_badges/  role chips (Tank, DPS, Healer, DoT, Buffer) and category badges (Character, Costume, Emblem, Title)
ornaments/     Greek-key strips, sprigs, dividers, corner brackets, swirls

NOTES
- Your files pack_art_floor1.png and pack_art_floor2.png were swapped (floor1 held the Floor 2 scenes). Fixed here.
- Button sets: parchment = set 1, terracotta = set 2, alt = a repeat of set 1 (the generator duplicated it). Order inside each set: normal, hover, pressed, disabled.
- Text is never baked into any image. Use real fonts in the engine.
- manifest.json lists every relic (number, code, name, tier), blessing, portrait and pack with file paths.

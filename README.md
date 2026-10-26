# Oddyseus - Lobby Remaster (paket siap instal)

Mengubah Lobby dari placeholder datar menjadi versi remastered (Night Sea and Bronze):
background lukisan, Polites berdiri, roda kemudi = Settings, tong gulungan = Almanac,
compass di atas, tombol chevron Story/Dungeon, dialogue box, marker "!", ribbon hover, "v0.1".
Semua UI memakai UI Kit AI-1 dan TextMeshPro. Tidak ada file di luar folder Lobby, Art/Lobby, dan satu builder.

## Isi paket (path persis)

    Assets/_Project/Art/Lobby/            7 PNG (background 4 stop, Polites, wheel_glow, scrolls_glow)
    Assets/_Project/Scripts/Lobby/        LobbyUI, LobbyHotspotInput, CharacterTalker, LobbyMenu,
                                          AlmanacPanel (baru), DungeonTableHotspot (diubah 1 baris)
    Assets/_Project/Scripts/Editor/Builders/LobbyBuilder.cs     menu "Oddyseus/Build Lobby (Remastered)"
    Assets/_Project/Scripts/Dungeon/      ScreenKit, ScreenToast, CardGlow, SlotArt (helper kecil, hanya butuh UI Kit)

Tidak diubah (tetap pakai punyamu): LobbyCameraRig, Hotspot, MenuObjectHotspot, StoryWindowHotspot,
ExitDoorHotspot, TableSilhouettes, SceneBuilder.cs, SceneFader.cs, GameScenes.cs.

Catatan art: tiga PNG (wheel, scrolls, Polites) aslinya berlatar magenta. Saya hapus latarnya jadi transparan
dan merapikan tepi pink pada glow. Nama file sama dengan kirimanmu.

## Install, klik demi klik

1. Tutup Unity.
2. Terminal di folder project:
       git status
   Kalau ada perubahan yang belum selesai, simpan dulu: `git stash push -u -m "wip"`.
       git checkout -b feature/lobby-remaster
3. Buka zip. Salin folder `Assets` dari paket ke folder project (yang berisi `Assets`, `Packages`, `ProjectSettings`).
   Pilih "Merge" untuk folder dan "Replace" untuk file yang sama. File yang akan tertimpa:
   LobbyUI.cs, LobbyHotspotInput.cs, CharacterTalker.cs, LobbyMenu.cs, DungeonTableHotspot.cs.
4. Buka Unity Hub, buka project, tunggu "Compiling" selesai. Buka tab Console (Window > General > Console).
5. Console harus tanpa error merah. Kalau ada, lihat bagian "Kalau compile error" di bawah.
6. Klik menu atas: Oddyseus > Build Lobby (Remastered). Tunggu (impor 7 PNG, sekitar setengah menit).
   Log hijau: "Lobby (remastered) built at Assets/_Project/Scenes/Lobby.unity".
   Scene lama otomatis disalin ke Assets/_Project/Scenes/_Backups/.
7. Di Project window buka Assets/_Project/Scenes/Title.unity (atau Lobby.unity), tekan Play.
8. Kalau sudah OK, commit hanya file ini:
       git add Assets/_Project/Art/Lobby Assets/_Project/Scripts/Lobby Assets/_Project/Scripts/Dungeon \
               Assets/_Project/Scripts/Editor/Builders Assets/_Project/Scenes/Lobby.unity Assets/_Project/Scenes/Lobby.unity.meta
       git diff --cached --stat
       git commit -m "Lobby remaster"
       git push -u origin feature/lobby-remaster
   Periksa `git diff --cached --stat` dulu: jangan ada TitleScreen.cs, font SDF, atau file Kit di daftarnya.

### Kalau compile error
- `GameScenes.DungeonHub` tidak ada: tambahkan `public const string DungeonHub = "DungeonHub";` di GameScenes.cs.
  Kalau belum mau, ubah baris 85 di DungeonTableHotspot.cs kembali ke `GameScenes.Dungeon`.
- Error di AlmanacPanel.cs (CharacterData, CharacterDatabase, SaveSystem tidak ditemukan atau field beda):
  data AI-5 belum ada. Hapus AlmanacPanel.cs. Lobby tetap jalan, tong gulungan hanya menulis log.
- Error `UIKit...`, `ConfirmPanel`, `SettingsPanel` tidak cocok: kamu mengubah file Kit setelah zip yang saya baca.
  Kirim teks errornya ke saya.

## Test checklist (Title -> Lobby -> semua yang bisa diklik)

1. Title: klik untuk mulai -> Lobby termuat dengan fade.
2. Terlihat: background ruang bundar, Polites berdiri, "v0.1" kiri bawah, compass atas dengan LOBBY menyala,
   chevron "STORY" kiri dan "DUNGEON" kanan, dialogue box bawah dengan nama "Polites" dan teks mengetik.
3. Tiga marker "!" muncul di Polites, roda kemudi, dan tong gulungan. Tidak menghalangi hover.
4. Arahkan mouse ke roda: glow memudar masuk, marker berdenyut, ribbon "SETTINGS" muncul. Klik -> panel Settings.
   Tutup Settings -> kamera bisa digeser lagi.
5. Arahkan ke tong: ribbon "ALMANAC". Klik -> Almanac (daftar karakter kiri, detail kanan). Tombol CLOSE menutup.
6. Klik Polites: baris baru mengetik. Klik kotak dialog setelah selesai mengetik: menutup. Diam 6 detik: menutup sendiri.
7. Chevron kiri/kanan, tombol panah keyboard, dan swipe: berpindah Story/Lobby/Dungeon/Exit. Compass ikut berubah.
   Di Story hanya chevron kanan (LOBBY), di Exit hanya chevron kiri (DUNGEON).
8. Story: klik jendela -> burung terbang, Story termuat (perilaku lama). Dungeon: klik peta -> pindah ke DungeonHub
   (perlu scene itu ada). Exit: klik pintu -> perilaku lama.
9. Ubah ukuran jendela Game ke 16:9, 16:10, 21:9: tidak ada elemen UI keluar layar.

## Yang TIDAK bisa saya tes (saya tidak punya Unity)

- Semua kode belum dikompilasi. Yang sudah saya cek: sintaks C# semua file lolos parser, semua teks ASCII,
  semua nama Kit/Hotspot/Rig yang dipanggil dicocokkan dengan kode di zip yang kamu kirim, dan tidak ada nama tipe ganda.
- Zip yang saya baca lebih lama dari projectmu. Kamu sudah mengubah UIKit.cs, ConfirmPanel.cs, SettingsPanel.cs.
- Pas atau tidaknya glow roda dan tong dengan gambar di background. Props di lobby_main.png digambar terpisah dari
  sprite glow (tong di background lebih lebar, roda sedikit miring), jadi saat hover sprite glow menggantikan prop itu.
  Lihat PREVIEW_glow_and_character_fit.png. Kalau meleset: pilih objek "Glow" di Hierarchy, geser Position/Scale di Inspector.
- Posisi marker "!", tinggi ribbon, ukuran dan posisi Polites (tinggi sekitar 51 persen layar), kejernihan tepi PNG.
- Tiga background stop lain adalah placeholder dan props jendela, peta, pintu dipindah dari builder lama tanpa diubah.
- Teks panjang di Almanac bisa meluap kotaknya.

## Asumsi

- Kamera: ukuran 5.628 supaya satu background 16:9 pas satu layar (setiap background lebar 20 unit = StopSpacing).
- CharacterData punya field: id, name, title, role, stamina, bio, traitName, traitText, talentName, talentText,
  talentUnlockFloor, moves[] (name, tag, description), unlockedByDefault, unlockConditionText.
  SaveSystem.Data.unlockedCharacters bisa dipanggil `.Contains(id)`. Hanya dipakai AlmanacPanel.
- GameSettings.CompanionChatter (sudah ada di zip) menentukan Polites bicara otomatis saat kamera tiba.

## Ganti art tanpa ubah kode

- Timpa PNG di Assets/_Project/Art/Lobby dengan nama sama, atau ubah Sprite di objek scene berikut:
  "Lobby Stop 0/1/2/3 ... Background (replace sprite)", "Character Polites (replace sprite)",
  dan objek "Glow" di bawah "Settings Wheel" dan "Scroll Barrel".
- Props placeholder stop lain bernama "PLACEHOLDER_...".

## REQUESTS TO OTHER AI

- AI-1: bekukan tanda tangan ini karena Lobby memakainya: UIKit.CreateCompass/CreateEdgeChevron/CreateHoverRibbon/
  CreateDialogueBox/CreateHotspotMarker/CreateOverlayCanvas/CreateButton/NewImage/NewRect/Stretch/Place,
  UITheme.CompassSize/EdgeChevronSize/DialogueBoxSize/Dim/SliderTrack, SettingsPanel.GetOrCreate().Open(Action),
  ConfirmPanel.GetOrCreate(). Kalau ScreenKit/SlotArt/CardGlow/ScreenToast dipakai lintas folder, pindahkan ke UI/Kit.
- AI-5: konfirmasi bentuk CharacterData dan SaveSystem.Data.unlockedCharacters (dipakai Almanac).
- AI-2 (GameScenes.cs): pastikan konstanta DungeonHub ada, dan hapus konstanta Dungeon yang lama kalau tidak dipakai.

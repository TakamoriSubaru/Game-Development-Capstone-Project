# Lobby v2 (Oddyseus) — cara pasang

## Isi paket
**Script baru** (Assets/_Project/Scripts/Lobby)
- `FireAnimator.cs` — api hidup: lidah api bergoyang & memanjang, salinan api kedua yang menari, bara naik, cahaya berkedip di perapian dan lantai, api membesar saat diklik
- `PropLife.cs` — anjing bernapas + "Zzz" + hati saat diklik; kuda mainan bergoyang seperti kuda-kudaan (per, bukan getar); lentera berayun seperti bandul dengan cahaya berkedip
- `LobbyFx.cs` — pembantu (sprite glow/hati/Z digambar dari kode)
- `AlmanacPanel.cs` — **Almanac** (tong gulungan): daftar karakter, medali, peran, stamina, stat, cerita, 4 jurus, trait/talent. Esc atau BACK untuk menutup

**Script diubah**
- `HoverRibbon.cs` — papan nama hover tidak pernah masuk ke area navbar atas (`topReserve`, bisa diatur)
- `UITheme.cs` — navbar kompas 640 → 720 px (huruf S di STORY tidak tertutup frame lagi), slider Settings lebih lebar
- `ThemedSlider.cs`, `UIKit.cs` — slider memakai art `pa_slider_track/fill`, knob dikunci PERSEGI (ini yang bikin gepeng), tombol BACK ukuran 252x84 (rasio asli sprite)
- `PropHotspot.cs` — event `Clicked` untuk animator
- `Uikitartimporter.cs` — aturan import untuk 2 PNG slider baru
- `Lobbypropsbuilder.cs` — memakai art baru + memasang animator

**Art baru**
- `Resources/UIKit/pa_slider_track.png`, `pa_slider_fill.png` (dari art `pa_slider` milikmu)
- `Art/Lobby/prop_dog_fit.png`, `prop_horse_fit.png`, `prop_lantern_fit.png` (art aslimu yang diberi warna golden-hour, tepi tinta, dan tekstur lebih lukis supaya menyatu dengan lobby_main)

## Langkah
1. Hentikan Play. Extract zip ini ke **folder project** (yang berisi `Assets/`), pilih timpa (overwrite).
   `unzip -o Oddyseus_lobby_v2.zip -d /path/ke/project`
2. Kembali ke Unity, tunggu compile selesai. Buka tab **Console** (Window > General > Console), klik **Clear**. Harus tidak ada error merah.
3. Kalau slider Settings masih terlihat lama: di Project klik kanan folder `Resources/UIKit` > **Reimport**.
4. Buka `Assets/_Project/Scenes/Lobby.unity` (klik dua kali).
5. Menu atas **Oddyseus > Rebuild Lobby (ALL in one)**. Kotak pesan harus menulis "Backgrounds with art: 4 of 4" dan "Interactable props: 12". Posisi prop yang kamu geser manual akan ter-reset.
6. Tekan **Play**.

## Yang dicek
- Api bergerak dan ada bara naik; klik perapian → api menyala besar
- Anjing bernapas, muncul Z, klik → hati keluar
- Kuda: klik → bergoyang lalu berhenti pelan; lentera berayun
- Hover benda yang tinggi (lentera, candelabra, rak) → papan nama tidak menutup navbar
- Navbar: "STORY" tidak tertutup frame
- Settings (klik roda kapal): knob bulat, slider tidak gepeng
- Almanac (klik tong gulungan di kiri): 5 karakter, klik tiap tab

## Menyetel (saat Play, lewat Inspector)
- Api: pilih objek `prop_fire` di PROPS > Fireplace → `Fire Animator`: Sway (goyang), Stretch (tinggi), Embers (jumlah bara), Glow Color
- Anjing/kuda/lentera: komponen `Prop Life` (Breath Amount, Rock Kick, Idle Angle, dst.)
- Ribbon: Hierarchy > Lobby HUD Canvas > Hover Ribbon > `Topreserve`

Semua kode ini belum diuji di Unity. Kalau ada error merah atau tampilan aneh, kirim teks error / screenshot.

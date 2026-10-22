# Oddyseus: UI Dungeon Mode (versi awal)

BELUM DIUJI DI UNITY. Logika (peta, jalur, Floor/Act, party, relic) sudah dites di luar Unity,
tetapi layar UI dibangun lewat kode dan belum pernah dijalankan di editor. Ikuti checklist di bawah.

## Cara pasang
1. Buka project Unity-mu. Pastikan paket "Unity UI" (uGUI) aktif (bawaan di Unity 2021+).
2. Di Project window, salin folder `_Project` dari zip ini ke dalam folder `Assets`.
   Hasilnya: `Assets/_Project/Scripts/Core`, `.../UI`, `.../Editor`.
3. Tunggu Unity selesai compile. Kalau ada error merah di Console, kirim teks errornya ke saya.
4. Di menu atas Unity klik **Oddyseus > Build Scenes**. Ini membuat scene `MainMenu` dan `Dungeon`
   di `Assets/_Project/Scenes` dan mendaftarkannya di Build Settings.
5. Buka scene `MainMenu` lalu tekan Play.

## Isi
- `Core/` murni C# (tanpa UnityEngine): battle system lama + `MapGraph`, `DungeonState`, `PackCatalog`.
- `UI/DungeonController.cs`: bar atas (Equipment, Resource, Party, Gear), pop-up, pengaturan.
- `UI/SelectMapView.cs`, `MapView.cs`, `PartyView.cs`, `EquipmentView.cs`, `ResourceView.cs`: satu file per layar wireframe.
- `UI/UIFactory.cs`: pembuat UI lewat kode, dan `Theme` (warna). Ganti warna di sini.
- `Editor/OddyseusSceneBuilder.cs`: pembuat scene (hanya jalan di editor).

## Checklist tes
- [ ] MainMenu tampil, tombol "Dungeon Mode" membuka scene Dungeon.
- [ ] Layar pertama: 3 kartu Map dengan nama, environment, boss, tema. Klik satu kartu membuka peta.
- [ ] Peta: node berbeda tiap kali Reset, semua jalur bertemu di satu node akhir, garis tidak saling silang.
- [ ] Hanya node yang terhubung dengan posisimu yang bisa diklik (node lain memunculkan pesan di bawah).
- [ ] Klik node "?" membuka pop-up yang menyebut jenis aslinya; setelah Continue, node itu berubah dari "?".
- [ ] Zoom In / Zoom Out bekerja; peta bisa digeser dengan drag saat di-zoom. Tombol Legend membuka daftar node.
- [ ] Selesai node akhir Act 1 memulai Act 2 (akhir Boss), selesai Act 2 kembali ke pilih Map Floor 2.
- [ ] Tab Party: klik karakter di daftar, tombol Add/Remove Party, skill 1-8 bisa dipilih, Equip/Unequip maksimal 4 skill.
- [ ] Tab Equipment: 7 slot equipment + 3 slot Blessing; tombol "+ Test" mengisi slot; slot penuh memunculkan pesan.
- [ ] Tab Resource: Men, Food, Gold tampil; tombol test mengubah angka.
- [ ] Gear: Restart (ke node pertama), Reset (ulang dungeon), Exit (ke MainMenu), slider Music dan SFX.
- [ ] Klik tab yang sama dua kali kembali ke peta.

## Asumsi yang saya ambil
- Layout peta 5 kolom mengikuti wireframe (Act 1: Battle, 3 kolom Random, Shop; Act 2: Random x3, Shop/Rest, Boss).
- Node Random menjadi Battle 35%, Event 25%, Resource 20%, Shop 10%, Elite 10% (placeholder).
- Teks Resource Node di wireframe tampak salinan Elite, jadi saya tulis ulang: "gain Food (and sometimes Men)".
- Simbol node masih huruf (X, ?, !, $, Zz, E, Rs, BOSS, G) sampai ikon Artist siap.
- Isi node (Battle, Shop, Event, dll) belum ada: klik node hanya membuka pop-up placeholder.
- Belum ada save ke disk; progres bertahan selama aplikasi terbuka.

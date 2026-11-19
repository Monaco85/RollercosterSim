# Unity-Fassung – Entwicklung und Projektwissen

Diese Seite sammelt alles, was man braucht, um an der Unity-Fassung (`unity/`) weiterzuarbeiten: Regeln, Aufbau, Werkzeuge, Bauen, Release und offene Punkte. Stand: 19.11.2026, Release v1.3.

## Feste Regeln

- **Physik, Bewegungswerte und SimHub-Paketformat nicht ändern.** Neue Bahnen nutzen dieselbe Physik. Frei sind nur Optik, Ton und Bedienung.
- **Sitz-Sicherheit:** Auf dem Sitz können Kinder sitzen. Tests dürfen nie unbemerkt Bewegungsdaten an die echte Brücke schicken.
  - Im Unity-Editor ist der Sitz gesperrt. Freigabe nur bewusst über das Menü **Achterbahn > Sitz im Editor verbinden** (PlayerPrefs `editorSitzFreigabe`). Für automatische Tests nie freigeben.
  - Die gebaute EXE verbindet sich sofort mit der Brücke. Zum Testen ohne Sitz `-ohnesitz` verwenden.
- **Assets nur mit freier Lizenz** (CC0 oder gleichwertig), weil das Repository öffentlich ist. Kein Unity Asset Store, kein Mixamo.

## Wie Unity mit SimHub spricht

```
Unity-Spiel ──WebSocket──▶ Brücke (Achterbahn.exe, Go) ──UDP 20790──▶ SimHub ──SMC3/COM4──▶ Arduino ──▶ Motoren
            /telemetrie                                   188 Bytes        Motion-Profil
            ◀──────── Status 1×/s ────────
```

1. `RideModel.cs` rechnet die Fahrt mit 240 Hz, `Cues.cs` daraus die Sitzbewegung. Beides ist 1:1 aus der gebauten `spiel.html` portiert und auf 4 Nachkommastellen abgeglichen.
2. `GameController.SendFrame` schickt etwa 125-mal pro Sekunde einen Frame mit 31 Float32 (124 Byte, little endian) über `BridgeClient.cs` an `ws://127.0.0.1:8765/telemetrie`.
3. Die Brücke (`bridge/main.go`, Version 3) baut daraus alle 10 ms ein 188-Byte-Paket für SimHub („External Sims“) und meldet einmal pro Sekunde den Status zurück (Anzeige oben rechts im Spiel).
4. Ohne Daten für 300 ms blendet die Brücke die Bewegung selbst auf null. Verliert das Unity-Fenster den Fokus, pausiert das Spiel und der Sitz fährt in die Mitte.

Für Unity startet man die Brücke mit `Bruecke fuer Unity starten.bat` (= `Achterbahn.exe -ohnefenster`), damit nicht zusätzlich das HTML-Spiel in Edge Daten schickt.

**Frame-Belegung:** 0 running, 1 paused, 2 discont, 3 sessionT, 4 yaw, 5 pitch, 6 roll, 7–9 yaw/pitch/rollRate, 10 surge, 11 sway, 12 heave, 13 v, 16 p.z, 17 −p.x, 18 p.y, 19 km/h, 20 rpm, 21 drive, 22 brake, 23 gSide, 24 gUp, 25 gFwd, 26 airtime, 27 lift, 28 launch, 29 brake, 30 landing.

## Projekt öffnen und bauen

- Unity **6000.0.85f2**, URP, Forward+. Ordner `unity` im Unity Hub hinzufügen.
- Pakete lädt Unity selbst, darunter `com.unity.cloud.gltfast` für die Menschen-Modelle.
- Fehlen Materialien oder Texturen: Menü **Achterbahn > Assets einrichten** (`Editor/ThemeAssetSetup.cs`). Es legt `Resources/ThemeAssets.asset`, Materialien und generierte Texturen in `Assets/Generated` an.
- Bauen: Menü **Achterbahn > Programm bauen** → `Build/Achterbahn.exe`.

## Code-Überblick (`Assets/Scripts`)

**Nicht anfassen (1:1 portiert):** `TrackModel.cs` (Catmull-Rom, Glättung, Rahmen, Neigung), `RideModel.cs` (240 Hz, Reibung, Luftwiderstand, AutoBank), `Cues.cs` (Stufen sanft/normal/kräftig, Kraft 0,5–5).

| Datei | Aufgabe |
|---|---|
| `Vec3d.cs` | JS (x,y,z) → Unity (x,y,−z) über `ToUnity()` |
| `TrackDefs.cs` | Sechs Bahnen: Raupe, Bergblitz (Thema stahl), Dino-Looping (`drache`, dino), Schüttelmonster (holz), Ritterburg (mittelalter), Wilde Maus (rummel) |
| `GameController.cs` | Zustände, Eingaben, 4 Kameras, IMGUI-Menü, Pause bei Fokusverlust, Editor-Sitzsperre, Startschalter, lesende Werte `RideSpeed/OnLift/Riding/Onboard` für Töne |
| `BridgeClient.cs`, `Bootstrap.cs` | WebSocket-Sender; erzeugt den GameController nach Szenenstart |
| `WorldBuilder.cs` | Baut je Thema die Welt, Züge, Spiegelung, Musik |
| `Look.cs` | Himmel (HDRI), Nebel, Sonne, Schatten, Post-Processing; Rummel spielt in der Dämmerung. Läuft vor dem Weltaufbau |
| `MeshBuilder.cs` | Prozedurale Formen. Front-Faces im Uhrzeigersinn, Quads (i,i+1,i+2),(i,i+2,i+3) |
| `Ground.cs`, `Scenery.cs`, `CastleBuilder.cs` | Gelände, Holzbahn, Dorf, Tannen, Liftkette, Rauch, Ritterburg |
| `DinoWorld.cs`, `Creatures.cs` | Dschungel, See, 30 Dinosaurier mit Kopf- und Kieferbewegung |
| `TrainBuilder.cs`, `Effects.cs` | Runde Wagen je Thema; Flaggen, Fackeln, Feuer |
| `FairWorld.cs` | Rummel: Platz, Station, Lauflichter, Riesenrad, Karussell, Wellenflug, Freifallturm, Schiffschaukel, Autoscooter, Lichterketten (etwa 115 echte Punktlichter) |
| `FairBooths.cs` | 45 Buden mit Details je Art (Glücksrad, Schießbude, Dosenwerfen, Entenangeln, Lebkuchen, Grill, Popcorn, Slush, Spielhalle) |
| `FairPeople.cs` | `FairPerson`: Spaziergänger, Paare, Verkäufer, Kunden, Schützen, Werfer, Schlangen, Zuschauer (winken dem Wagen), Fahrgäste |
| `FairFx.cs` | Feuerwerk, Himmelsstrahler, Moving Heads, Rummel-Töne, `RideAudio` (Fahrtwind, Rollen, Kettenlift für alle Bahnen), `ThemeMusic` (Musik je Bahn) |
| `Bulbs.cs` | `BulbSet`: Glühbirnen und Lichthöfe als zwei Meshes; Blinkmuster rechnet der Shader |
| `Crowd.cs` | `CrowdBank` backt beim ersten Laden pro Figur ein Mesh und eine Knochen-Textur (15 Bilder/s, mit Sitz-, Reit- und Arme-hoch-Haltung); `CrowdAnim` spielt sie ab |

**Shader (`Assets/Shaders`):** `Water` (Dino-See), `Bulb` + `BulbHalo` + `BulbPattern.hlsl` (Glühbirnen mit GPU-Mustern), `LightBeam` (Lichtkegel), `NeonText` (Neon-Schrift), `Additive` (Feuerwerk), `Crowd` (Menschen, GPU-Skinning aus Textur, SRP-Batcher-tauglich).

**Warum die Menschen gebacken werden:** Mit normalen SkinnedMeshRenderern kostete jede Figur fast 1 ms, der Rummel lief mit 8 FPS. Gebacken läuft er im Editor mit etwa 50 FPS (die anderen Bahnen mit 86–103), in der EXE flüssig.

## Assets und Quellen

Alle frei nutzbar (CC0). Jeder Ordner hat eine eigene `QUELLE_CC0.txt`, die Übersicht steht in [DRITTANBIETER.md](../DRITTANBIETER.md).

| Ordner | Quelle | Hinweise |
|---|---|---|
| `Assets/PolyHaven` | Poly Haven | Himmel (Tag und Dämmerung), Texturen, Felsen, Büsche. Baummodelle sind zu schwer, Bäume sind prozedural |
| `Assets/AmbientCG` | ambientCG | Asphalt, Planenstoff, lackiertes Holz, Wellblech, Lack. Rauheit wird beim Einrichten zu Glätte gepackt |
| `Assets/Quaternius/People` | Quaternius über poly.pizza | 13 animierte Menschen (GLB). Quaternius' Google Drive sperrt oft wegen Quota. Auf poly.pizza nur CC0-markierte Modelle nehmen, einige dort sind CC-BY |
| `Assets/Audio` | freesound.org | Rummel-Töne, Feuerwerk, Kettenlift und `musik_*.mp3` je Bahn. Lizenz jeweils auf der Seite geprüft |

## Startschalter der Unity-EXE

| Schalter | Wirkung |
|---|---|
| `-ohnesitz` | Verbindet sich nie mit der Brücke. Anzeige „Vorschau ohne Sitz“ |
| `-bahn N` | Lädt Bahn N (1–6), nur zusammen mit `-ohnesitz` |
| `-fahrt` | Startet die Fahrt automatisch, nur mit `-ohnesitz` |
| `-bilder <Ordner>` | Speichert 8 Bildschirmfotos im Abstand von 5 s und beendet sich, nur mit `-ohnesitz` |

Beispiel: `Achterbahn.exe -ohnesitz -bahn 6 -fahrt`

## Arbeiten mit der Unity-CLI

Die Unity-CLI (`%LOCALAPPDATA%\Unity\bin\unity.exe`) steuert den offenen Editor (Port 7800):

- `unity status`, `unity open <Projekt>`, `unity recompile`
- `unity command console get` / `console clear`
- `unity command editor invoke_menu "Achterbahn/Programm bauen"` → im Log „Achterbahn-Build: Succeeded“
- `unity command editor.playmode start` / `stop`
- `unity command script run --file AgentScripts/X.cs --entry Klasse.Methode --args '[…]'`
- Bilder ohne Menü: `unity command capture game_view --source camera --camera PruefKamera --overlay false`

Die Hilfsskripte liegen in [`unity/AgentScripts`](../unity/AgentScripts), außerhalb von `Assets`, damit sie keinen Import auslösen:

| Skript | Zweck |
|---|---|
| `Drive.cs` | Bahn laden, Fahrt starten, Ansicht wählen, FPS (per Reflection auf den GameController) |
| `Compare.cs`, `CompareCues.cs` | Abgleich von Physik und Bewegung mit dem Original |
| `CheckTrack.cs`, `CheckMaus.cs` | G-Kräfte neuer Bahnen prüfen |
| `Perf.cs` | Statistik, Gruppen ein- und ausschalten |
| `Cam.cs`, `Follow.cs`, `Freeze.cs` | Feste Prüfkamera, Figur anvisieren, Zeit anhalten für Bilder |
| `Setup.cs` | Audio und ambientCG neu importieren, „Assets einrichten“ ausführen |
| weitere | `Riders.cs`, `Props.cs`, `Music.cs`, `Boom.cs`, `Inspect*.cs`, `Pose.cs` u. a. für Einzelprüfungen |

Wichtig dabei: Der Sitz bleibt im Editor gesperrt, die Skripte schicken nichts an die Brücke.

## Release

- GitHub Actions baut bei jedem Push die Brücke. Ein Tag `v*` (oder „Run workflow“ mit Version) legt ein Release mit `Achterbahn-Windows.zip` an. Text kommt aus `RELEASE.md`.
- Unity kann auf GitHub nicht gebaut werden. Die Unity-EXE wird lokal gebaut, als `Achterbahn-Unity-Windows.zip` gepackt (Ordner `Achterbahn-Unity` mit Build und `LIESMICH-Unity.txt`, ohne `*_DoNotShip`) und von Hand an das Release gehängt.
- v1.3 (19.11.2026) enthält beide ZIPs.

## Stand und offene Punkte

- Release v1.3: sechs Bahnen, Rummel in der Dämmerung mit Lichtern, Feuerwerk, Buden und rund 370 animierten Menschen, Musik je Bahn, Fahrgeräusche. Vom Nutzer getestet: läuft flüssig.
- Noch nicht bestätigt: die gedämpfte Feuerwerk-Lautstärke, die Musik-Auswahl, Esc/F11/Vollbild in der EXE.
- Ideen: Dächer der Buden von oben detaillierter, eigene Lautstärkeregler im Menü.

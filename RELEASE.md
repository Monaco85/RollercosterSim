## Achterbahn für den Rennsitz

Achterbahn-Simulator, der deinen 2DOF-Sitz über SimHub bewegt.

**Herunterladen** (unten unter Assets):
- `Achterbahn-Windows.zip`: Brücke zu SimHub und das HTML-Spiel (klein, wie bisher).
- `Achterbahn-Unity-Windows.zip`: **neu**, die realistische 3D-Fassung als eigene Windows-EXE (rund 245 MB). Braucht für den Sitz zusätzlich die Brücke aus `Achterbahn-Windows.zip`.

### Wichtig vor dem Entpacken
Die EXEs sind nicht signiert, deshalb blockiert Windows neue Fassungen. Rechtsklick auf die ZIP-Datei, „Eigenschaften“, unten „Zulassen“ anhaken, OK. Erst dann entpacken. Den alten Achterbahn-Ordner ganz ersetzen. Kommt trotzdem eine Warnung: „Weitere Informationen“ und „Trotzdem ausführen“.

### Neu in v1.3: Unity-Fassung
- Sechs Bahnen mit eigener Welt und eigener Musik: Kleine Raupe, Bergblitz, Dino-Looping (Urwald, See, Dinosaurier), Schüttelmonster, Ritterburg und neu die **Wilde Maus** auf einem Rummel in der Dämmerung mit Lichtern, Feuerwerk und animierten Besuchern.
- Physik, Bewegungswerte und SimHub-Paketformat sind dieselben wie im HTML-Spiel.
- So fährst du mit Sitz: SimHub starten, im Brücken-Ordner `Bruecke fuer Unity starten.bat` doppelklicken (nur das schwarze Fenster, kein Edge), dann `Achterbahn-Unity\Achterbahn.exe` starten. Oben rechts muss „Sitz verbunden“ stehen. Nie gleichzeitig das HTML-Spiel offen haben.
- Ohne Sitz anschauen: die Brücke einfach weglassen, oder die Unity-EXE mit `-ohnesitz` starten.
- Tasten: Leertaste/Enter/Lenkradtaste Start, 1–6 Bahn, C Kamera, Esc Bahnwahl, 2× Esc Beenden, F11 Vollbild/Fenster.

### Neu in der Brücke
- Startschalter `-ohnefenster` und dazu `Bruecke fuer Unity starten.bat`: Die Brücke läuft ohne Spielfenster in Edge, für die Unity-Fassung.

### Unverändert
- HTML-Spiel wie in v1.2, `spiel.html` neben der EXE.
- SimHub-Anbindung: Spiel „Achterbahn“, UDP-Port 20790, Einstellungen bleiben erhalten.

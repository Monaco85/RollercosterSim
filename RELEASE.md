## Achterbahn für den Rennsitz

Achterbahn-Simulator, der deinen 2DOF-Sitz über SimHub bewegt.

**Herunterladen** (unten unter Assets):
- `Achterbahn-Windows.zip`: Brücke zu SimHub und das HTML-Spiel (klein, wie bisher).
- `Achterbahn-Unity-Windows.zip`: die realistische 3D-Fassung als eigene Windows-EXE, mit eingebauter Brücke zu SimHub.

### Wichtig vor dem Entpacken
Die EXEs sind nicht signiert, deshalb blockiert Windows neue Fassungen. Rechtsklick auf die ZIP-Datei, „Eigenschaften“, unten „Zulassen“ anhaken, OK. Erst dann entpacken. Den alten Ordner ganz ersetzen. Kommt trotzdem eine Warnung: „Weitere Informationen“ und „Trotzdem ausführen“.

### Neu in v1.5: nur noch die Unity-EXE starten
- Die Unity-Fassung bringt die Brücke zu SimHub jetzt im Ordner `Bruecke` mit und **startet sie selbst**, unsichtbar im Hintergrund. Kein schwarzes Fenster und keine .bat-Datei mehr nötig: SimHub starten, dann `Achterbahn.exe`.
- Beim Beenden fährt die Brücke den Sitz wie bisher sanft in die Mitte und schließt sich nach ein paar Sekunden selbst.
- Läuft schon eine Brücke (z. B. von Hand gestartet), nutzt das Spiel diese. Mit `-ohnesitz` startet keine Brücke.
- Meldungen der Brücke stehen in `Bruecke\bruecke.log`. Nach einem SimHub-Update: Spiel schließen, `Bruecke\SimHub neu einrichten.bat`.
- Brücke und SimHub-Paketformat selbst sind unverändert. `Achterbahn-Windows.zip` brauchst du für die Unity-Fassung nicht mehr, nur noch für das HTML-Spiel.

### Neu in v1.4 (Unity-Fassung)
- **Neun Bahnen**: dazu gekommen sind die **Wikinger-Bahn** (Taste 7, Abschuss und drei Loopings am Fjord), die **Schiffsschaukel** (Taste 8) und das **Boxauto** (Taste 9).
- **Boxauto in einer Autoscooter-Halle bei Nacht**: Podest mit Stahlplatten, Bande, Lauflichter, Neon-Schriftzug, Moving Heads, Zuschauer, Fahrer in den Wagen. Lenkrad und Pedale werden automatisch erkannt (MOZA, Logitech, Thrustmaster, Fanatec …), dazu Tastatur und Gamepad.
- **Lenkarten** für das Boxauto: „Echt 1:1“ wie im echten Autoscooter, „Leicht“ (Standard, man muss richtig kurbeln: quer erst bei halbem Einschlag, rückwärts erst am Anschlag) und „Gas/Bremse“ (vorwärts mit Gas, rückwärts mit dem zweiten Pedal).
- **KI-Wagen an/aus**: allein auf der Bahn fahren.
- **Auto-Gas** für Kinder: Der Wagen fährt von selbst, nur lenken.
- **LAN-Mehrspieler** für das Boxauto: zwei PCs im selben Netz, einer klickt „Spiel öffnen“, der andere „Beitreten“. Jeder PC fährt seinen eigenen Sitz. Beide PCs brauchen v1.4.
- Lenkbereich (360–1080°) in der Bahnwahl einstellbar, muss zur Einstellung des Lenkrads passen (z. B. MOZA Pit House).
- Physik der Achterbahnen, Bewegungswerte und SimHub-Paketformat sind unverändert.

### Hinweis zur Wikinger-Bahn
Die Fassung im Release zeigt bei der Wikinger-Bahn ein einfaches Dorf. Die aufwendigere Kulisse nutzt ein Asset aus dem Unity Asset Store, das nicht weitergegeben werden darf.

### Unverändert
- Brücke und HTML-Spiel wie in v1.3, `spiel.html` neben der EXE.
- SimHub-Anbindung: Spiel „Achterbahn“, UDP-Port 20790, Einstellungen bleiben erhalten.
- Mit Sitz fahren: SimHub starten, dann `Achterbahn.exe` aus der Unity-ZIP starten. Oben rechts muss „Sitz verbunden“ stehen. Nie gleichzeitig das HTML-Spiel offen haben. Vor einer Fahrt mit Kind erst ohne Mitfahrer „Sitz testen“.

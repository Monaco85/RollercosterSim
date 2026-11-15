## Achterbahn für den Rennsitz

Achterbahn-Simulator, der deinen 2DOF-Sitz über SimHub bewegt.

**Herunterladen:** `Achterbahn-Windows.zip` unten unter Assets.

### Wichtig vor dem Entpacken
Die EXE ist nicht signiert, deshalb blockiert Windows neue Fassungen. Rechtsklick auf die ZIP-Datei, „Eigenschaften“, unten „Zulassen“ anhaken, OK. Erst dann entpacken. Den alten Achterbahn-Ordner ganz ersetzen. Kommt trotzdem eine Warnung: „Weitere Informationen“ und „Trotzdem ausführen“.

### Neu in v1.2
- Das Spiel liegt jetzt als `spiel.html` neben der EXE. Künftige Spiel-Updates tauschen nur diese Datei. `Achterbahn.exe` bleibt dann gleich, und Windows fragt nicht jedes Mal neu.
- Ist SimHub schon eingerichtet, startet die EXE kein PowerShell mehr. Sie nimmt die gespeicherte Kennung und startet dadurch schneller.
- Neu: `SimHub neu einrichten.bat` für den Fall, dass nach einem SimHub-Update nichts mehr ankommt.

### Unverändert
- Spiel wie in v1.1: Der Sitz legt sich in Kurven nach innen, mit Kulisse und Kuppen-Fix.
- SimHub-Anbindung: Spiel „Achterbahn“, UDP-Port 20790, Einstellungen bleiben erhalten.

# RollercosterSim – Hinweise für Claude

Achterbahn-Simulator für einen 2DOF-Rennsitz (SimHub). Der Nutzer schreibt Deutsch: Antworten und Texte im Spiel auf Deutsch.

## Feste Regeln

- **Physik, Bewegungswerte und SimHub-Paketformat nicht ändern** (HTML-Spiel und Unity). Frei sind nur Optik, Ton und Bedienung.
- **Sitz-Sicherheit:** Tests dürfen nie Bewegungsdaten an die echte Brücke oder den Sitz schicken, auf dem Sitz können Kinder sitzen. Die Editor-Sitzsperre nie für Tests aufheben. Die Unity-EXE nur mit `-ohnesitz` starten, und auch dann nur mit Erlaubnis des Nutzers.
- **Nur freie Assets** (CC0 oder gleichwertig), das Repository ist öffentlich.
- Pushen, Releases und Uploads nur nach Zustimmung des Nutzers.

## Wo was steht

- Unity-Fassung, Aufbau, Werkzeuge, Bauen und Release: [docs/UNITY-ENTWICKLUNG.md](docs/UNITY-ENTWICKLUNG.md)
- HTML-Spiel, Brücke, SimHub, Ports: [README.md](README.md) und [LIESMICH.txt](LIESMICH.txt)
- Lizenzen und Herkunft der Assets: [DRITTANBIETER.md](DRITTANBIETER.md)

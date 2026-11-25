@echo off
rem Rechnet die SimHub-Kennung neu aus, z. B. nach einem SimHub-Update. Vorher das Spiel schliessen.
rem Danach dieses Fenster schliessen und das Spiel wie gewohnt starten.
cd /d "%~dp0"
Achterbahn-Bruecke.exe -einrichten -ohnefenster

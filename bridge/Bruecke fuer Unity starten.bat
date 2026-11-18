@echo off
rem Startet nur die Bruecke zu SimHub, ohne das HTML-Spiel in Edge.
rem Danach das Unity-Spiel starten. Dieses Fenster offen lassen, solange gefahren wird.
cd /d "%~dp0"
Achterbahn.exe -ohnefenster

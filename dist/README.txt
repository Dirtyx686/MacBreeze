MacBreeze 2.0 - Windows / Boot Camp
==================================

Lueftersteuerung und Systemmonitor fuer Intel-Macs unter Windows.

1. Alle Dateien gemeinsam in einen dauerhaften Ordner entpacken.
2. MacBreeze.exe starten und die Administratorabfrage bestaetigen.
3. Die App legt eine Desktop-Verknuepfung und geplante Aufgaben an und
   aktiviert beim ersten Start den Autostart. Unter Einstellungen kann
   "Mit Windows starten" wieder ausgeschaltet werden.
4. Zunaechst Sensoren, Drehzahlen und Treiberstatus pruefen.

Die App arbeitet mit erhoehten Rechten. Beim Schliessen des Fensters kann
sie im Infobereich weiterlaufen. Zum Beenden dessen Kontextmenue verwenden.
Einstellungen und Protokolle: %APPDATA%\MacBreeze

T2-Macs: "Automatisch reparieren" startet setup-pawnio-t2.ps1. Der Helfer
aktiviert Testsignatur, kann vorhandene PawnIO-Treiber entfernen und
installiert PawnIO 2.0.1 unrestricted. Das verringert Sicherheitsbeschraenkungen
und erfordert einen Neustart. Vorher die vollstaendige Anleitung lesen.

Anleitung, Quellcode und Lizenzhinweise:
https://github.com/Dirtyx686/MacBreeze
https://github.com/Dirtyx686/MacBreeze/blob/v2.0.0/docs/INSTALLATION.md

Hauptprogramm: GPL-2.0-only (LICENSE.txt und NOTICE.txt).
InpOut32: MIT (LICENSE-InpOut32.txt).
AppleT2Smc: LGPL-2.1-or-later (LICENSE-AppleT2Smc.txt).
Der PawnIO-Treiber selbst ist nicht enthalten.

Build und statische Zuordnung zum Quellcode wurden geprueft; kein neuer
Hardwaretest. Keine Zusage der Unterstuetzung jedes Mac-Modells.

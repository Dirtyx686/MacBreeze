@echo off
REM Fotografiert MacBreeze: jede Seite in jedem Look -> build\screenshots\
REM Vorher MacBreeze beenden (Rechtsklick auf das Symbol neben der Uhr -^> Beenden).
echo MacBreeze macht jetzt Fotos von allen Seiten in allen Looks.
echo Das dauert ca. 2 Minuten. Die App startet dabei mehrmals neu - bitte nichts anklicken.
start "" "%~dp0build\MacBreeze.exe" --screenshots

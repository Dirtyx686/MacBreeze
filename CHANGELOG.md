# Änderungen

## MacBreeze 2.0

Erste veröffentlichte Version von MacBreeze, entstanden aus RPMac 1.10.1.

**Neu**
- Neuer Name und neues Symbol
- Alle 24 EU-Amtssprachen – automatisch nach Windows-Sprache, Auswahl oben in der Menüleiste
- Übersetzungen als JSON-Dateien in `tools/i18n/` (Generator `tools/gen_i18n.py`)
- 5 Looks: Modern dunkel, Gaming / RGB, macOS hell, macOS dunkel, Minimal – mit runder Drehzahl-Anzeige
- Smarter Systemmonitor: CPU, Arbeitsspeicher, Netzwerk, Systemlaufwerk, Akku, Programme mit der meisten Last, Hinweise zur Hitze-Ursache
- Kurzinfo beim Drüberfahren über das Symbol neben der Uhr
- Fenster frei veränderbar und maximierbar, Größe/Position werden gemerkt
- Menüleiste wahlweise links oder oben
- Start als Administrator ohne UAC-Abfrage (Desktop-Verknüpfung) und Autostart beim ersten Start
- Eigener Einstellungsordner `%APPDATA%\MacBreeze` (Einstellungen von RPMac werden einmalig übernommen)
- Foto-Modus (`FOTOS.bat`) für Screenshots

**Behoben (T2-Macs)**
- Lüftersteuerung war auf T2-Macs blockiert: Die Markierung „SMC antwortet nicht“ aus der Port-Prüfung wurde nach dem Wechsel auf PawnIO nie zurückgesetzt
- Fehlermeldung verwies auf den falschen PawnIO-Installer; jetzt mit klarer Anleitung und Knopf „Automatisch reparieren“
- Einrichtungs-Skript lief aus der 32-Bit-App in der 32-Bit-PowerShell, in der `bcdedit` fehlt
- Overlay-Umschaltung vertikal/horizontal konnte hängen bleiben

Die Versionsgeschichte von RPMac: [`docs/RPMac-RELEASE_NOTES.md`](docs/RPMac-RELEASE_NOTES.md)

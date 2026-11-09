# Selbst bauen

## GUI und Kommandozeilenprogramm

Voraussetzungen: Windows, .NET Framework 4.x mit `csc.exe`, WPF-Referenzen und Schreibrechte im Projektordner. Für den normalen Build sind weder Administratorrechte noch Treiberinstallation erforderlich.

```bat
build.bat
```

Das Skript verwendet zuerst `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319`, alternativ den entsprechenden `Framework`-Ordner. Es erzeugt x86-Programme und kopiert Abhängigkeiten und Lizenzdateien nach `build\`.

`build.bat` kompiliert die Programme, startet sie jedoch nicht. Erst ein späterer App-Start fordert Administratorrechte an und kann Treiber sowie geplante Aufgaben einrichten.

## Projektstruktur

| Pfad | Inhalt |
|---|---|
| `src/gui/App.cs` | Oberfläche, Profile, Einstellungen, Infobereich und Startaufgaben |
| `src/gui/Smc.cs` | SMC-Zugriff über InpOut32 beziehungsweise PawnIO |
| `src/gui/SysMon.cs`, `TrayPeek.cs` | Systemmonitor und Kurzinfo |
| `src/gui/I18n*.cs` | Übersetzungslogik und erzeugte Sprachdaten |
| `src/smccore.cs` | Kommandozeilenprogramm |
| `src/pawnio/AppleT2Smc.p` | Quellcode des T2-Moduls, LGPL-2.1-or-later |
| `dist/AppleT2Smc.bin` | Mitgeliefertes kompiliertes T2-Modul |
| `third_party/InpOut32/` | Native Bibliotheken, zugehörige Quellen und MIT-Lizenz |
| `tools/setup-pawnio-t2.ps1` | Optionaler Einrichtungshelfer mit Systemänderungen |

## Übersetzungen

`tools/i18n/*.json` bearbeiten und anschließend aus dem Projektordner ausführen:

```text
python tools/gen_i18n.py
build.bat
```

Englische Texte dienen als Schlüssel. Der Generator meldet fehlende Einträge; diese werden zur Laufzeit auf Englisch angezeigt.

## Native Komponenten

Der normale Build baut InpOut32 und AppleT2Smc nicht neu. Er verwendet die beigelegten Dateien. Die InpOut32-DLL wurde über ihren SHA-256-Wert dem offiziellen Paket `InpOutBinaries_1501.zip` zugeordnet. Dessen Lizenztext ist unverändert beigefügt.

Für einen eigenen Neubau des T2-Moduls werden zusätzlich der PawnIO-kompatible Compiler und dessen Include-Dateien `pawnio.inc` und `registry.inc` benötigt. Sie gehören zum externen [PawnIO-Projekt](https://github.com/namazso/PawnIO) und dessen Werkzeugen. Dieser separate Modul-Build wurde hier nicht ausgeführt; die Identität eines selbst erzeugten Moduls mit der mitgelieferten BIN-Datei ist nicht bestätigt. Änderungen am Modul erfordern eine erneute Prüfung der Treiber- und Signaturanforderungen.

Der [Prüfbericht](VERIFICATION.md) beschreibt, was für diese Version tatsächlich validiert wurde.

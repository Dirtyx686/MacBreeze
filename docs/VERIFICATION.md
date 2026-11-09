# Prüfung zur Bereitstellung von MacBreeze 2.0

Stand: 9. November 2026.

## Geprüft

- Windows-Upload und Quellcode-Archiv vollständig inventarisiert; Archivpfade vor dem Entpacken geprüft.
- GUI und `smccore.exe` aus dem zugeordneten Quellcode mit `build.bat` erfolgreich kompiliert.
- Übersetzungsgenerator ausgeführt: alle 23 Übersetzungsdateien enthalten 258 Einträge; die neu erzeugte C#-Datei stimmt mit dem Upload überein. Englisch ist die Quellsprache.
- Die neu erzeugten EXE-Dateien mit dem Windows-Upload verglichen: nach Ausblenden ausschließlich des PE-Build-Zeitstempels, der MVID und ihres Vorkommens im vom Compiler erzeugten Typnamen sind alle übrigen Bytes identisch.
- Konfiguration, InpOut32-DLL, T2-Modul und Einrichtungshelfer zwischen Quellcode- und Windows-Paket abgeglichen.
- InpOut32-DLL mit dem offiziellen Paket `InpOutBinaries_1501.zip` per SHA-256 verglichen; identisch. Original-MIT-Lizenz ergänzt.
- GPL-Lizenz und ursprüngliche Herkunftshinweise erhalten; LGPL-2.1-Text für das T2-Modul ergänzt.
- Dateinamen, Textdateien und lesbare Zeichenfolgen der Binärdateien auf übliche Zugangsschlüssel, private Schlüssel und persönliche Benutzerpfade geprüft. Dabei keine solchen Geheimnisse gefunden. Öffentliche Urheberkontakte in Lizenzhinweisen bleiben erhalten.
- Bereitgestellte Screenshots visuell und auf PNG-Textmetadaten geprüft. 20 ausgewählt; fünf Systemansichten mit sichtbaren Prozesslisten nicht übernommen.
- Persönliche IDE-Dateien, alte Quellverwaltungs-Metadaten und doppelte Beispiel-Builds nicht übernommen. Eigene Build-Ausgaben und ZIP-Pakete bleiben außerhalb der Git-Historie.

## Grenzen

Die Programme und der Einrichtungshelfer wurden für diese Prüfung nicht gestartet. Es wurden keine Hardwarezugriffe, Treiberinstallationen, Lüftertests oder Änderungen an der Startsicherheit vorgenommen. Der separate Neubau von AppleT2Smc und InpOut32 wurde nicht ausgeführt. Die statische Prüfung ist keine vollständige Sicherheits- oder Malwareanalyse. Aus den Screenshots wird keine allgemeine Modellkompatibilität abgeleitet.

## Unveränderte Programmdateien aus dem Upload

| Datei | SHA-256 |
|---|---|
| `MacBreeze.exe` | `a53ac87b0e5191a0a57d2a91996e0028e8610b1a17ea14f1fdceda42e722aa9e` |
| `smccore.exe` | `9784d2d3b51cc0f52e380876b137720c359ec38c1962261080cf4e297d4c69da` |
| `inpout32.dll` | `01bcec6ddb4964e1f5b69ba1bd3876221d8de7ae17cacfac66f095013434a78f` |
| `AppleT2Smc.bin` | `6b81fc1dad1f62406b3f5111fea33ef6c111b6d7d3db0a895a3f7c35c89211c1` |

Das Windows-Release erhält ergänzte Dokumentation und Lizenzdateien; sein ZIP-Hash unterscheidet sich deshalb vom ursprünglichen Upload. Maßgeblich sind die Prüfsummen im jeweiligen Release.

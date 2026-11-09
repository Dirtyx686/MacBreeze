# Herkunft und Drittanbieter

| Komponente | Lizenz | Herkunft / Text |
|---|---|---|
| MacBreeze und RPMac-Hauptcode | GPL-2.0-only | [RPMac](https://github.com/golirt1/RPMAC), [LICENSE](LICENSE), [NOTICE](NOTICE) |
| AppleT2Smc-Modul | LGPL-2.1-or-later | Copyright 2026 golirt1, [Quellcode](src/pawnio/AppleT2Smc.p), [LGPL-2.1](LICENSES/LGPL-2.1.txt) |
| InpOut32 / InpOutx64 | MIT | Phil Gibbons und Logix4U, [Original-Lizenz](third_party/InpOut32/LICENSE-MIT.txt), [offizielle Downloads](https://www.highrez.co.uk/downloads/inpout32/) |
| SMC-Protokoll-Herkunft | GPL-2.0-only | Linux `applesmc.c` und smcFanControl; ursprüngliche Copyright-Hinweise stehen in [NOTICE](NOTICE) |

Die Datei `third_party/InpOut32/LICENSE-MIT.txt` stammt unverändert aus dem offiziellen Paket `InpOutBinaries_1501.zip`. Die enthaltene 32-Bit-DLL stimmt mit der beigelegten Laufzeitdatei überein (siehe [Prüfbericht](docs/VERIFICATION.md)). Die Quellen und benötigten nativen Eingabedateien sind unter `third_party/InpOut32` erhalten.

Der PawnIO-Treiber selbst wird nicht mitgeliefert. Der optionale Helfer lädt den Installer von [PawnIO.Setup](https://github.com/namazso/PawnIO.Setup) herunter. Für PawnIO gelten dessen eigene Lizenztexte und Bedingungen; siehe [PawnIO](https://github.com/namazso/PawnIO). Die Lizenz des lokalen AppleT2Smc-Moduls ist davon gesondert im Modulquellcode ausgewiesen.

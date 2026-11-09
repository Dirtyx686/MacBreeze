# Prüfung MacBreeze 2.1

Stand: 10. November 2026.

- GUI und Kommandozeilenprogramm erfolgreich gebaut.
- Alle drei neuen nativen WPF-Layouts in einem isolierten UI-Test mit simuliertem SMC geöffnet und gerendert. Einstellungen geöffnet und Speicherung/erneutes Laden der Akzentfarbe geprüft.
- Die Testausführung verwendet einen eigenen Einstellungsordner. Geplante Aufgaben, Tray-Start, Treiberinstallation und Hardwarezugriffe sind im Test ausgeschaltet beziehungsweise durch eine Simulation ersetzt. Der Testcode ist nicht im Laufzeitpaket enthalten.
- Setup-EXE erfolgreich kompiliert; eingebettetes Paket mit `/verify` vollständig gelesen, Dateipfade und Pflichtdateien geprüft. Keine echte Installation im Nutzerprofil ausgeführt.
- SMC-/Hardwarecode und PawnIO-Einrichtungshelfer aus 2.0 unverändert. Die neue Bestätigungsabfrage wird vor dem vorhandenen Helfer ausgeführt.
- Version 2.0 bleibt unter dem Tag und Release `v2.0.0` erhalten. Ein zusätzliches lokales Archiv enthält Quellcode und Windows-Paket mit SHA-256-Prüfsummen.

Es wurde kein neuer Test an echter Mac-Hardware durchgeführt. Die Bilder `v21-*` zeigen ausdrücklich simulierte UI-Vorschauen. Anwendung und Setup sind nicht digital signiert. Der Installer verändert weder Testsignatur noch Treiber; hierfür ist die gesonderte, bestätigte Einrichtung in der App zuständig.

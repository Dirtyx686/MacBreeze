# Installation und Nutzung

## Windows-Paket

Das Paket im [Release v2.0.0](https://github.com/Dirtyx686/MacBreeze/releases/tag/v2.0.0) vollständig entpacken. Das Repository enthält den Quellcode; das Windows-ZIP enthält die fertigen Programme.

Alle Laufzeitdateien zusammen in einem dauerhaften Ordner lassen. Die App braucht beim ersten Start Administratorrechte und richtet die Aufgaben `MacBreeze` (Autostart) und `MacBreeze-Start` (erhöhter Start über die Desktop-Verknüpfung) ein. Beim ersten Start wird der Autostart aktiviert. Unter **Einstellungen → Mit Windows starten** lässt er sich ausschalten.

Die Programme sind nicht digital signiert. Die mitgelieferten SHA-256-Prüfsummen erlauben einen Vergleich mit den heruntergeladenen Dateien; sie sind kein Nachweis einer Sicherheitsprüfung durch einen unabhängigen Anbieter.

## Erste Kontrolle

1. Zunächst **Auto** verwenden und Sensoren sowie Drehzahlen prüfen.
2. Den Treiberstatus am unteren Fensterrand beachten. Im Nur-Lese-Modus keine wirksame Lüfterregelung erwarten.
3. Keine zweite Lüftersteuerung gleichzeitig verwenden.
4. Manuelle Werte und Kurven schrittweise einstellen und Temperaturen beobachten.

Nicht jeder Mac stellt dieselben Sensoren bereit. Es gibt in diesem Repository keine unabhängig verifizierte Liste aller unterstützten Modelle.

## T2-Macs

Bei T2-Macs verwendet der enthaltene Code das Modul `AppleT2Smc.bin` über PawnIO. Der Quellcode des Moduls liegt unter `src/pawnio/AppleT2Smc.p`.

**Vor „Automatisch reparieren“ beachten:** `setup-pawnio-t2.ps1` aktiviert die Windows-Testsignatur, kann einen vorhandenen PawnIO-Treiber deinstallieren und dessen zwischengespeicherte Treiberpakete entfernen. Anschließend lädt es PawnIO 2.0.1 von einer fest eingetragenen GitHub-Adresse herunter, prüft dessen SHA-256 und installiert die unrestricted-Ausgabe. Ein Neustart ist erforderlich.

Testsignatur und unrestricted-Treiber verringern Sicherheitsbeschränkungen. Bei aktivem Secure Boot bricht das Skript ab. Die Secure-Boot-Einstellung nicht als beiläufigen Installationsschritt verändern; vorher die Auswirkungen für das eigene Gerät prüfen. Die Eignung der Treiberkonfiguration wurde bei dieser Veröffentlichung nicht auf T2-Hardware getestet.

Der Aufruf über die App startet denselben Helfer mit erhöhten Rechten. Alternativ kann ein Administrator den geprüften Helfer gezielt ausführen:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\setup-pawnio-t2.ps1
```

Zum Rückgängigmachen der Testsignatur ist laut Helfer `bcdedit /set testsigning off` als Administrator und ein Neustart vorgesehen. Den PawnIO-Treiber bei Bedarf über dessen Installer deinstallieren. Danach kann die T2-Lüftersteuerung nicht mehr verfügbar sein. Zuvor geänderte Startsicherheitseinstellungen müssen gesondert wiederhergestellt werden.

## Profile, Infobereich und Daten

Lüftereinstellungen lassen sich als Profil speichern. Ein Startprofil kann automatisch angewendet werden. Die App kann beim Schließen des Fensters im Infobereich weiterlaufen; zum vollständigen Beenden dessen Kontextmenü verwenden.

Profile, Einstellungen und Protokolle liegen in `%APPDATA%\MacBreeze`. Beim ersten Start können vorhandene `.txt`-Einstellungen aus `%APPDATA%\RPMac` übernommen werden. Protokolle können Gerätenamen, Prozesse oder lokale Dateipfade enthalten: vor dem Teilen prüfen.

## Entfernen

Autostart in der App ausschalten und die App über das Symbol im Infobereich beenden. Danach Programmordner und Desktop-Verknüpfung entfernen. Die verbleibende Aufgabe `MacBreeze-Start` kann in der Windows-Aufgabenplanung entfernt werden; auch `MacBreeze` auf einen verbleibenden Autostarteintrag prüfen. Persönliche Einstellungen in `%APPDATA%\MacBreeze` nur löschen, wenn sie nicht mehr gebraucht werden. PawnIO und Testsignatur werden durch das Entfernen des Programmordners nicht zurückgesetzt.

# SpielfeldTV

Windows-Anwendung für lokale IP-Kameras auf bis zu drei Bildschirmen, mit frei belegbaren Rastern und Tabs.

## Version 0.6.0

- Kameras per ONVIF suchen oder manuell eintragen; XM/DVR-IP und RTSP wiedergeben.
- Kamera-Namen, Zugangsdaten, Stream-Einstellungen, Raster und Tabs als passwortgeschützte `.sftv`-Datei übertragen.
- Windows-Monitornamen anzeigen, Bildschirme identifizieren und Zuschaueranzeigen beim Start öffnen.
- Optional beim Windows-Login starten. Diese Option gilt nur für den jeweiligen PC.
- Update-Center für öffentliche GitHub-Releases, mit SHA-256-Prüfung vor dem Start des Installers.

## Installation

Unter **Releases** das Komplett-Setup herunterladen. Bei vorhandener Installation genügt das Update. Kameraeinstellungen bleiben in `%LOCALAPPDATA%\SpielfeldTV\settings.dat` unter dem jeweiligen Windows-Konto geschützt gespeichert.

## Zum Hallen-PC übertragen

In Tab 4 die Konfiguration exportieren und ein eigenes Passwort vergeben. Die `.sftv`-Datei zum Ziel-PC kopieren, dort importieren und die Monitore zuordnen. Vor dem Ersetzen legt die Anwendung eine lokale Sicherung an. Das Exportpasswort separat aufbewahren.

## Bauen und veröffentlichen

`./Build.ps1` auf Windows mit .NET Framework 4.8 erstellt EXE und Update-Installer. `./Build.ps1 -FullSetup` lädt zusätzlich die offizielle VLC-3.0.24-Laufzeit und prüft deren SHA-256. Der Release-Workflow baut bei Tags `v*` beide Installer und veröffentlicht sie als GitHub-Release.

Quellcode: MIT. VLC wird unter seinen eigenen Lizenzbedingungen verteilt; Lizenztexte und Plugins bleiben im Komplettpaket enthalten. Die Anwendung zeichnet nicht auf. Tatsächliche Bildrate und Verzögerung hängen von Kamera, PC und Netzwerk ab.

## Datenschutz

Keine echten Kameradaten, Konfigurationsexporte, Passwörter oder Aufnahmen in dieses öffentliche Repository laden. Update-Prüfungen senden keine Kamerakonfiguration; sie rufen die öffentlichen GitHub-Releases ab. Exportdateien enthalten Zugangsdaten und benötigen ein starkes Passwort.

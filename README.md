# SpielfeldTV
Windows-Anwendung für lokale IP-Kameras auf bis zu drei Bildschirmen, mit frei belegbaren Rastern und Tabs.
## Version 0.6.0
- Kamerasuche per ONVIF; native XM/DVR-IP- und RTSP-Wiedergabe.
- Passwortgeschützter Export und Import von Kamera-Namen, Zugängen, Streams, Rastern und Tabs.
- Windows-Monitornamen, Bildschirm-Identifizierung und Start-Tab je Anzeige.
- Optionaler Start bei der Windows-Anmeldung.
- GitHub Update-Center mit SHA-256-Prüfung.
## Installation
Unter Releases das Komplett-Setup herunterladen. Bei vorhandener Installation genügt das Update. Vor dem Installieren SpielfeldTV schließen. Kein ZIP-Entpacken nötig. Einstellungen bleiben in `%LOCALAPPDATA%\SpielfeldTV\settings.dat` geschützt gespeichert.
## Zum Hallen-PC übertragen
In Tab 4 die Konfiguration exportieren, ein eigenes Passwort vergeben, die `.sftv`-Datei zum Ziel-PC kopieren und dort importieren. Anschließend die dortigen Monitore zuordnen. Vor dem Ersetzen wird die bisherige Konfiguration lokal gesichert.
## Bauen und veröffentlichen
`./Build.ps1` auf Windows mit .NET Framework 4.8 erstellt Anwendung und Update-Installer. `./Build.ps1 -FullSetup` lädt die offizielle VLC-3.0.24-Laufzeit und prüft deren SHA-256.
Der GitHub-Workflow wird manuell gestartet und stellt geprüfte Installer als Build-Artefakte bereit. Die Veröffentlichung als Release erfolgt separat. Er benötigt keine Schreibrechte am Repository.
Quellcode: MIT. VLC unter seinen eigenen Lizenzbedingungen; Lizenztexte und Plugins sind im Komplettpaket enthalten. Tatsächliche Bildrate und Verzögerung hängen von Kamera, PC und Netzwerk ab. Keine Aufzeichnung.
## Datenschutz
Keine echten Kameradaten, Konfigurationsexporte, Passwörter oder Aufnahmen in dieses öffentliche Repository laden. Die Update-Prüfung ruft öffentliche Releases ab und sendet keine Kamerakonfiguration. Exportdateien enthalten Zugangsdaten und benötigen ein starkes Passwort.

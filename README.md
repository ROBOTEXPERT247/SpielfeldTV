# SpielfeldTV

Windows-Anwendung für lokale IP-Kameras auf bis zu drei Bildschirmen, mit frei belegbaren Rastern und Ansichten.

## Version 0.8.2

Leere, nicht zugewiesene Kameraplätze zeigen eine durchgehende grüne Fläche mit mittigem, freigestelltem Bunker-73-Logo als dezentes, entsättigtes Wasserzeichen. Keine Rahmen, Kameranamen oder Statusmeldungen auf leeren Plätzen. Zugewiesene Kameras behalten auch bei einem Ausfall ihre normale Kachel; dort erscheint kein Logo. Das Logo ist in der Anwendung eingebettet. Hinweise zu den Rechten am Logo stehen in BRANDING.txt.

- Zuschaueransicht in dunklen Olivtönen, mit großer mittiger Spielfeldüberschrift und Kameranamen unter den Bildern.
- Überschrift pro Bildschirm in Tab 3 festlegen; Standard: Spielfeld 1, Spielfeld 2 usw.
- Kameras per ONVIF finden; native XM/DVR-IP- und RTSP-Wiedergabe.
- Gleichzeitige Wiedergabe mit begrenzter Decoderlast und Hintergrundinitialisierung für einen bedienbaren Viewer-Start.
- Passwortgeschützter Konfigurationsexport/-import inklusive Kamerazugängen, Rastern, Ansichten und Bildschirmüberschriften.
- Windows-Monitornamen, Bildschirmzuordnung, Startansichten und optionaler Windows-Autostart.
- GitHub Update-Center mit SHA-256-Prüfung; Installer-Korrektur gegen selbst verursachte Ordnersperren.

## Installation und Updates

Unter Releases das Komplett-Setup herunterladen. Für eine vorhandene Installation genügt das kleine Update oder Tab 4 > GitHub Update-Center. SpielfeldTV und alte Update-Fenster vor der Installation schließen. Kein ZIP-Entpacken nötig; VLC ist im Komplett-Setup enthalten.

Kameraeinstellungen bleiben in `%LOCALAPPDATA%\SpielfeldTV\settings.dat` unter dem Windows-Konto geschützt gespeichert. In Tab 4 lässt sich eine portable, separat passwortgeschützte Konfiguration erzeugen. Am Ziel-PC importieren und die dortigen Monitore zuordnen. Vor dem Ersetzen wird eine lokale Sicherung angelegt.

## TV-Bedienung

- Doppelklick auf Kamerabild oder Namen: Einzelbild / Raster.
- Rechtsklick auf die Beschriftung: Ansicht wechseln, Vollbild oder Fenster schließen.
- Bild auf / Bild ab oder Strg+Tab: Ansichten wechseln.
- F11: Vollbild / Fenster. Esc: Einzelbild verlassen oder Zuschauerfenster schließen.

## Bauen

`./Build.ps1` auf Windows mit .NET Framework 4.8 erstellt Anwendung und Update-Installer. `./Build.ps1 -FullSetup` lädt die offizielle VLC-3.0.24-Laufzeit und prüft deren SHA-256. Der GitHub-Workflow wird manuell gestartet und liefert Build-Artefakte; Releases werden separat veröffentlicht. Der Workflow hat keine Schreibrechte am Repository.

Quellcode: MIT. VLC unter seinen eigenen Lizenzbedingungen; Lizenztexte und Plugins sind im Komplettpaket enthalten. Die Anwendung zeichnet nicht auf. Tatsächliche Bildrate, Verzögerung und Startdauer hängen von Kamera, PC und Netzwerk ab; lokale Testwerte sind keine Zusicherung für andere Hardware.

## Datenschutz

Keine echten Kameradaten, Konfigurationsexporte, Passwörter oder Aufnahmen in dieses öffentliche Repository laden. Die Update-Prüfung sendet keine Kamerakonfiguration. Exportdateien enthalten Zugangsdaten und benötigen ein starkes Passwort. Vorschaubilder im Release sind als künstliche Layoutansicht gekennzeichnet.



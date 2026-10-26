<p align="center"><img src="assets/TokenMonitor.png" width="72" alt="Token Monitor"></p>
<h1 align="center">Token Monitor</h1>
<p align="center"><strong>Kontingente, Guthaben und API-Nutzung auf einen Blick.</strong><br>Ein Windows-Desktop-Monitor für Codex / ChatGPT Work, DeepSeek und OpenAI API.</p>
<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4" alt="Windows 10 / 11 x64">
  <a href="https://github.com/Ricki0613/TokenMonitor/releases/latest"><img src="https://img.shields.io/badge/version-3.2.1-10A37F" alt="Version 3.2.1"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License"></a>
</p>
<p align="center"><a href="https://github.com/Ricki0613/TokenMonitor/releases/download/v3.2.1/TokenMonitor-v3.2.1-windows-x64.zip"><strong>Für Windows herunterladen</strong></a> · <a href="#schnellstart">Schnellstart</a> · <a href="docs/USER_GUIDE.zh-CN.md">Benutzerhandbuch (Chinesisch)</a></p>

<p align="center">
  <a href="README.md">简体中文</a> · <a href="README.en.md">English</a> · <a href="README.ja.md">日本語</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.de.md">Deutsch</a> · <a href="README.ru.md">Русский</a>
</p>

[![Token Monitor](assets/readme-hero.png)](assets/readme-hero.png)

<p align="center"><sub>Dashboard und schwebendes Widget · Beispieldaten · Für die hochauflösende Ansicht anklicken</sub></p>

## Nutzung immer im Blick

Behalte drei Dienste beim Programmieren oder bei Modellaufrufen in einem Dashboard im Blick und zeige wichtige Werte direkt auf dem Desktop an.

- **Kompaktes schwebendes Widget**: Zwei Prozentwerte zeigen das verbleibende 5-Stunden- und Wochenkontingent. Verschieben per Drag-and-drop, Details per Doppelklick, fünf Größen.
- **Unabhängige schwebende Fenster**: Für jeden Dienst separat einblenden, ausblenden, verschieben und in der Größe ändern. Verbundene Dienste aktualisieren sich auch bei ausgeblendeten Fenstern weiter.
- **Lokaler Verlauf**: Diagramme für 7 und 30 Tage, Speicherung für 90 Tage. Fehlende Messungen werden nicht ergänzt.
- **Anpassbare Darstellung**: Helles, dunkles oder Systemdesign, drei Schriftgrößen und eine ständig sichtbare Seitenleiste.
- **Updates und Wiederherstellung**: Paket in der Anwendung herunterladen und prüfen, Neustart bestätigen und bei Bedarf zur vorherigen Version zurückkehren.

## Unterstützte Daten

| Dienst | Angezeigte Daten | Verbindung |
| --- | --- | --- |
| **ChatGPT / Codex** | Gemeinsames Restkontingent von Codex / ChatGPT Work, Rücksetzzeiten und verfügbare Rücksetzvorgänge | Lokal angemeldetes Codex |
| **DeepSeek** | Verfügbares, aufgeladenes und geschenktes Guthaben nach Währung | API Key oder lokale Anmeldung bei DeepSeek Harness |
| **OpenAI API** | Tokens heute/diesen Monat, Eingabe/Ausgabe/gecachte Eingabe und Anfragen für eine API Key ID | Normaler API Key; für den Verlauf zusätzlich Admin API Key und passende Key ID |

Die ChatGPT-Daten beziehen sich auf das Codex / ChatGPT Work-Kontingent, nicht auf sämtliche Nachrichtenlimits im Chat. Die OpenAI-Statistik umfasst Anfragen aus dem offiziellen Endpoint `usage/completions`. Gecachte Eingaben sind bereits in den Eingabe-Tokens enthalten.

## Schnellstart

**Voraussetzungen: Windows 10 / 11 x64 und .NET Framework 4.8. Die Oberfläche der Anwendung ist derzeit auf Chinesisch.** Python, Node und das .NET SDK werden zur Nutzung nicht benötigt.

1. [Windows-ZIP herunterladen](https://github.com/Ricki0613/TokenMonitor/releases/download/v3.2.1/TokenMonitor-v3.2.1-windows-x64.zip) und vollständig entpacken. Das ausführbare Paket auswählen, nicht das automatisch erzeugte GitHub-Archiv `Source code`.
2. **Install.cmd** starten und Programm- und Datenverzeichnis wählen. Das Installationsprogramm legt Verknüpfungen auf dem Desktop und im Startmenü an. Alternativ kann die entpackte `TokenMonitor.exe` direkt gestartet werden.
3. Benötigte Dienste über **连接设置** (Verbindungseinstellungen) auf den Karten verbinden und anschließend die schwebenden Fenster oder das Widget aktivieren.

Standardmäßig wird alle **30 Sekunden** aktualisiert. Im Dashboard sind 15 / 30 / 60 / 120 Sekunden oder eine manuelle Aktualisierung wählbar. Beim Schließen des Hauptfensters läuft die Anwendung im Infobereich weiter. Ein Doppelklick auf das Symbol öffnet sie wieder; über das Menü kann sie vollständig beendet werden.

[Installation und Fehlerbehebung](docs/START_HERE.zh-CN.md) · [Ausführliches Handbuch](docs/USER_GUIDE.zh-CN.md) (beide auf Chinesisch)

### DeepSeek: API Key oder Harness

In **DeepSeek 密钥** einen API Key eingeben oder sich unter demselben Windows-Benutzer bei Harness anmelden und **DeepSeek 密钥 → DeepSeek 余额来源 → DeepSeek Harness 登录账户** auswählen. Auswahl speichern; Harness muss nicht geöffnet bleiben.

Im Harness-Modus werden aufgeladenes und geschenktes Guthaben je Währung zusammengefasst. Die Zugangsdaten werden bei Bedarf am standardmäßigen lokalen Speicherort gelesen und nicht in Token Monitor kopiert. Zwischen beiden Quellen kann gewechselt werden. Benutzerdefinierte Harness-Datenverzeichnisse und inoffizielle Server werden nicht unterstützt. Siehe [Umgang mit Zugangsdaten](SECURITY.md).

### OpenAI: Schlüsselprüfung und Zugriff auf Nutzungsdaten

Zuerst den normalen API Key prüfen, anschließend den **Admin API Key** der Organisation und die passende **API Key ID** (`key_…`) eintragen. Ein gültiger normaler Schlüssel allein gewährt keinen Zugriff auf den Nutzungsverlauf. Bei unvollständiger Einrichtung zeigt die Anwendung den nächsten Schritt an, statt fälschlich null Nutzung zu melden.

## Verlauf, Updates und lokale Daten

### Verlauf über 7 oder 30 Tage

Die Diagramme verwenden Pekinger Zeit (UTC+8). ChatGPT und DeepSeek speichern alle 15 Minuten den letzten erfolgreich abgerufenen Wert. Der Verlauf hängt von der lokalen Quelle ab; ein Kontowechsel beeinflusst die Kurve.

Die täglichen OpenAI-Werte werden getrennt nach einem Hash der Key ID gespeichert. Jeder Abruf korrigiert die verfügbaren Tageswerte des laufenden Monats. Für frühere Monate bleiben nur bereits erfasste Werte erhalten; der heutige Wert ist vorläufig.

**Eine Guthabenänderung ist kein Verbrauchsbetrag.** Aufladungen, Gutschriften und Rückerstattungen verändern ebenfalls das DeepSeek-Guthaben.

### Updates in der Anwendung

Unter **关于软件** (Über die Software) können Updates gesucht, Änderungen gelesen und Pakete heruntergeladen werden. SHA-256 und die Version der ausführbaren Datei werden geprüft, bevor ein Neustart und die Installation bestätigt werden müssen. Einstellungen, Zugangsdaten und Verlauf bleiben erhalten; die vorherige Programmversion lässt sich auf derselben Seite wiederherstellen.

Beim Start und nach einer erfolgreichen Prüfung alle sechs Stunden wird nach stabilen Releases gesucht. Netzwerkfehler und GitHub-Limits unterbrechen die Überwachung nicht; ein Link zum manuellen Download bleibt verfügbar. Von v3.1.0 und älter muss das erste Upgrade manuell installiert werden.

[Releases](https://github.com/Ricki0613/TokenMonitor/releases) · [Änderungsprotokoll](CHANGELOG.md)

### Daten bleiben lokal

Für den normalen Betrieb und das Speichern von Schlüsseln sind keine Administratorrechte nötig. Das Standardverzeichnis ist `%LOCALAPPDATA%\TokenMonitor`. In **数据目录** kann ein leeres beschreibbares Verzeichnis gewählt werden; Einstellungen, Zugangsdaten und Verlauf werden vor dem Wechsel kopiert und geprüft. Das ursprüngliche Verzeichnis bleibt als Sicherung bestehen.

- DeepSeek- und OpenAI-Schlüssel werden mit Windows DPAPI für den aktuellen Benutzer verschlüsselt.
- Harness-Zugangsdaten werden nur bei Bedarf gelesen und ausschließlich an den offiziellen DeepSeek-Dienst gesendet.
- Nutzungsabfragen lösen keine Modell-Inferenzanfragen aus. Das Einlösen einer offiziellen Kontingentrücksetzung erfordert eine eigene Bestätigung.
- Update-Prüfungen übermitteln weder lokale Einstellungen noch Zugangsdaten oder Kontonutzungsdaten an GitHub.

Weitere Informationen stehen in [SECURITY.md](SECURITY.md). Statistiken können serverseitig verzögert sein. Automatischer Start ist nicht eingerichtet; die veröffentlichten Programme sind derzeit nicht codesigniert.

## Entwicklung und Beiträge

[Fehlerberichte und Vorschläge](https://github.com/Ricki0613/TokenMonitor/issues/new/choose) sowie Pull Requests sind willkommen. Echte Zugangsdaten und Kontodaten gehören nicht in Screenshots, Protokolle oder Commits. Siehe [CONTRIBUTING.md](CONTRIBUTING.md).

<details>
<summary>Aus Quellcode bauen und prüfen</summary>

```powershell
git clone https://github.com/Ricki0613/TokenMonitor.git
cd TokenMonitor
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui.ps1
```

Ausgabe in `build/`. Verwendet wird der C#-Compiler von .NET Framework. Offline-Tests nutzen Beispieldaten und benötigen keine echten Konten oder Schlüssel. Pakete mit `scripts/package.ps1` erstellen und die Installation mit `scripts/test-package.ps1 -Archive <ZIP>` prüfen.

Das README-Bild wird mit `scripts/render-readme.ps1` aus nativen WPF-Steuerelementen und Beispieldaten in doppelter Auflösung erzeugt. Für Update-Hinweise muss ein stabiles Release mit ausführbarem ZIP veröffentlicht und als Latest markiert werden; ein Git-Tag allein genügt nicht.

</details>

## Lizenz

[MIT License](LICENSE) · Copyright (c) 2026 Ricki0613

Unabhängiges Projekt ohne offizielle Zugehörigkeit zu OpenAI oder DeepSeek.

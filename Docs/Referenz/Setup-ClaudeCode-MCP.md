# Claude Code MCP Setup — BPM Entwicklungs-PC

Anleitung um Claude Desktop so einzurichten, dass Claude im Chat direkt Dateien auf dem PC lesen und schreiben kann (über Claude Code als MCP-Server).

## Voraussetzungen

- Windows 10/11
- Claude Desktop App installiert (claude.ai/download)
- Claude Max Plan (Max empfohlen, Pro geht auch)
- Git für Windows installiert (git-scm.com)

## Schritt 1: Node.js installieren

Von https://nodejs.org die LTS Version herunterladen und installieren. "Add to PATH" muss angehakt bleiben (ist Standard). Prüfen in PowerShell:

```powershell
node --version
```

## Schritt 2: Claude Code über WinGet installieren

Der PowerShell-Installer (`irm https://claude.ai/install.ps1 | iex`) wird oft von Windows Defender blockiert. WinGet ist der saubere Weg:

```powershell
winget install Anthropic.ClaudeCode
```

PowerShell schließen und neu öffnen, dann prüfen:

```powershell
where.exe claude
```

Es muss ein Pfad mit `claude.exe` dabei sein. Diesen Pfad merken — wird in Schritt 5 gebraucht.

## Schritt 3: Claude Code Permissions akzeptieren (einmalig)

```powershell
claude --dangerously-skip-permissions
```

Bedingungen akzeptieren, dann `/exit` eingeben.

## Schritt 4: Prüfen ob der MCP-Server startet

```powershell
npx -y @steipete/claude-code-mcp@latest
```

Sollte ausgeben: `Claude Code MCP server running on stdio`. Mit Ctrl+C beenden.

## Schritt 5: Claude Desktop Config bearbeiten

Datei öffnen — Win+R, dann eingeben: `%APPDATA%\Claude\claude_desktop_config.json`

**Mit VS Code öffnen** (nicht Notepad — Encoding-Probleme). Inhalt:

```json
{
  "mcpServers": {
    "github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "DEIN_GITHUB_TOKEN"
      }
    },
    "claude-code": {
      "command": "cmd",
      "args": ["/c", "npx", "-y", "@steipete/claude-code-mcp@latest"],
      "env": {
        "CLAUDE_CLI_NAME": "PFAD_ZUR_CLAUDE_EXE_AUS_SCHRITT_2"
      }
    }
  },
  "preferences": {
    "coworkWebSearchEnabled": true,
    "coworkScheduledTasksEnabled": false,
    "ccdScheduledTasksEnabled": true
  }
}
```

### Wichtig:

- **DEIN_GITHUB_TOKEN**: Fine-grained Token von https://github.com/settings/tokens?type=beta erstellen (Repo: BauProjektManager, Permissions: Contents R/W, Metadata R, Pull Requests R/W, Issues R/W)
- **PFAD_ZUR_CLAUDE_EXE**: Den vollen Pfad aus `where.exe claude` mit doppelten Backslashes (`\\`) einsetzen
- **Encoding**: UTF-8 (in VS Code rechts unten prüfen)

## Schritt 6: Config validieren

```powershell
Get-Content "$env:APPDATA\Claude\claude_desktop_config.json" | ConvertFrom-Json
```

Kein Fehler = JSON ist gültig.

## Schritt 7: Claude Desktop neu starten und testen

1. Claude Desktop beenden (Tray-Icon → Beenden, nicht nur X)
2. Neu starten
3. Im Chat testen: "Lies die README.md aus meinem BPM-Repo"

## Troubleshooting

### `spawn claude ENOENT`

Claude Code wurde nur über npm installiert (`.cmd`-Datei). Lösung: Nativ über WinGet installieren (Schritt 2) und den `.exe`-Pfad in `CLAUDE_CLI_NAME` eintragen.

### `spawn EINVAL`

Der `CLAUDE_CLI_NAME` zeigt auf eine `.cmd`-Datei statt auf eine `.exe`. Lösung: Pfad zur `claude.exe` verwenden.

### Config wird beim Neustart zurückgesetzt

Ungültiges JSON. Lösung: Config in VS Code mit UTF-8 Encoding speichern und mit dem PowerShell-Befehl aus Schritt 6 validieren.

### Windows Defender blockiert den PowerShell-Installer

Lösung: WinGet verwenden statt `irm https://claude.ai/install.ps1 | iex`

## PC-spezifische Werte

| Wert | Haupt-PC | Surface |
|------|----------|---------|
| BPM Repo | `D:\OneDrive\Dokumente\02 Arbeit\05 Vorlagen - Scripte\05_BauProjekteManager` | `C:\Users\herbe\OneDrive\Dokumente\02 Arbeit\05 Vorlagen - Scripte\05_BauProjekteManager` |
| claude.exe | Aus `where.exe claude` ermitteln | Aus `where.exe claude` ermitteln |
| GitHub Token | Gleicher Token auf beiden PCs | Gleicher Token |

## Versionen (Stand: April 2026)

- Claude Code: v2.1.92
- steipete/claude-code-mcp: v1.10.12
- Node.js: v24.13.0
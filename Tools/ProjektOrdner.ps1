# Get-ProjektOrdner.ps1
# Gibt alle Projektordner mit Unterordnern (2 Ebenen tief) als TXT aus.
# Öffnet einen Ordnerauswahl-Dialog.
#
# ANLEITUNG:
# 1. Rechtsklick → "Mit PowerShell ausführen"
# 2. Ordner auswählen (dein Arbeitsordner mit den Projektordnern)
# 3. Datei "ProjektOrdner.txt" liegt danach am Desktop

Add-Type -AssemblyName System.Windows.Forms

$dialog = New-Object System.Windows.Forms.FolderBrowserDialog
$dialog.Description = "Wähle den Arbeitsordner mit deinen Projektordnern"
$dialog.ShowNewFolderButton = $false

if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
    Write-Host "Abgebrochen." -ForegroundColor Yellow
    pause
    exit
}

$BasePath = $dialog.SelectedPath

$OutputFile = [System.IO.Path]::Combine(
    [Environment]::GetFolderPath('Desktop'),
    "ProjektOrdner.txt"
)

$result = @()
$result += "========================================"
$result += "PROJEKTORDNER-STRUKTUR"
$result += "Pfad: $BasePath"
$result += "Datum: $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
$result += "========================================"
$result += ""

$projektOrdner = Get-ChildItem -Path $BasePath -Directory | Sort-Object Name

foreach ($projekt in $projektOrdner) {
    $result += "--- $($projekt.Name)"
    
    $subDirs = Get-ChildItem -Path $projekt.FullName -Directory -ErrorAction SilentlyContinue | Sort-Object Name
    
    foreach ($sub in $subDirs) {
        $result += "       +-- $($sub.Name)"
        
        $subSubDirs = Get-ChildItem -Path $sub.FullName -Directory -ErrorAction SilentlyContinue | Sort-Object Name
        foreach ($subSub in $subSubDirs) {
            $result += "       |      +-- $($subSub.Name)"
        }
    }
    
    $result += ""
}

$result += "========================================"
$result += "Anzahl Projektordner: $($projektOrdner.Count)"
$result += "========================================"

$result | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host ""
Write-Host "Fertig! Datei erstellt:" -ForegroundColor Green
Write-Host $OutputFile -ForegroundColor Cyan
Write-Host ""
pause

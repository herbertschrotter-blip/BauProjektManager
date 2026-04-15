Add-Type -AssemblyName System.Windows.Forms

# Quellordner wählen
$sourceBrowser = New-Object System.Windows.Forms.FolderBrowserDialog
$sourceBrowser.Description = "Quellordner auswählen (wird rekursiv durchsucht)"
$sourceBrowser.ShowNewFolderButton = $false
if ($sourceBrowser.ShowDialog() -ne 'OK') { Write-Host "Abgebrochen."; exit }
$source = $sourceBrowser.SelectedPath

# Zielordner wählen
$destBrowser = New-Object System.Windows.Forms.FolderBrowserDialog
$destBrowser.Description = "Zielordner auswählen (Dateien werden hierhin verschoben)"
$destBrowser.ShowNewFolderButton = $true
if ($destBrowser.ShowDialog() -ne 'OK') { Write-Host "Abgebrochen."; exit }
$dest = $destBrowser.SelectedPath

# Dateien sammeln
$files = Get-ChildItem -Path $source -Recurse -File
Write-Host "`n$($files.Count) Dateien gefunden in: $source"
Write-Host "Ziel: $dest`n"

if ($files.Count -eq 0) { Write-Host "Keine Dateien gefunden."; exit }

$confirm = Read-Host "Alle $($files.Count) Dateien nach '$dest' verschieben? (j/n)"
if ($confirm -ne 'j') { Write-Host "Abgebrochen."; exit }

$moved = 0; $errors = 0
foreach ($file in $files) {
    $targetPath = Join-Path $dest $file.Name
    # Bei Namenskollision: Datei umbenennen
    if (Test-Path $targetPath) {
        $base = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $ext = [System.IO.Path]::GetExtension($file.Name)
        $i = 1
        do {
            $targetPath = Join-Path $dest "${base}_($i)${ext}"
            $i++
        } while (Test-Path $targetPath)
    }
    try {
        Move-Item -Path $file.FullName -Destination $targetPath -ErrorAction Stop
        $moved++
    } catch {
        Write-Host "FEHLER: $($file.Name) — $_" -ForegroundColor Red
        $errors++
    }
}

Write-Host "`n--- Fertig ---"
Write-Host "$moved Dateien verschoben" -ForegroundColor Green
if ($errors -gt 0) { Write-Host "$errors Fehler" -ForegroundColor Red }
Read-Host "`nEnter zum Schliessen"

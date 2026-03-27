Add-Type -AssemblyName System.Windows.Forms

# Ziel-Datei
$outputFile = "$env:USERPROFILE\Desktop\PlanListe.txt"

# Datei neu erstellen
"" | Out-File $outputFile

Write-Host "=== Planliste Generator ==="
Write-Host "1. Projekt wählen"
Write-Host "2. Planordner wählen"
Write-Host "Abbrechen = fertig`n"

while ($true) {

    # --- PROJEKTORDNER ---
    $projectDialog = New-Object System.Windows.Forms.FolderBrowserDialog
    $projectDialog.Description = "Projektordner auswählen"

    if ($projectDialog.ShowDialog() -ne "OK") {
        break
    }

    $projectPath = $projectDialog.SelectedPath
    $defaultProjectName = Split-Path $projectPath -Leaf

    $projectName = Read-Host "Projektname (Enter = $defaultProjectName)"
    if ([string]::IsNullOrWhiteSpace($projectName)) {
        $projectName = $defaultProjectName
    }

    # --- PLANORDNER ---
    $planDialog = New-Object System.Windows.Forms.FolderBrowserDialog
    $planDialog.Description = "Planordner innerhalb des Projekts auswählen"

    if ($planDialog.ShowDialog() -ne "OK") {
        Write-Host "Kein Planordner gewählt, Projekt übersprungen.`n"
        continue
    }

    $planPath = $planDialog.SelectedPath

    Write-Host "Scanne Projekt: $projectName"
    Write-Host "Planordner: $planPath"

    # Dateien sammeln
    Get-ChildItem -Path $planPath -Recurse -File -ErrorAction SilentlyContinue |
    ForEach-Object {
        "$projectName | $($_.FullName) | $($_.Name)" | Out-File -Append $outputFile
    }

    Write-Host "Fertig: $projectName`n"
}

Write-Host "`n=== FERTIG ==="
Write-Host "Datei: $outputFile"
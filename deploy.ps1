param(
    [switch]$Force
)

$modDir = "C:\Program Files (x86)\Steam\SteamApps\common\RimWorld\Mods\LabelsOnFloor"
$distDir = "$PSScriptRoot\dist\LabelsOnFloor"

if (!(Test-Path $distDir)) {
    Write-Host -ForegroundColor Red "Error: dist folder not found. Run build first."
    exit 1
}

# Try to remove old mod folder
if (Test-Path $modDir) {
    Write-Host "Removing old mod folder..."
    
    # Try once
    Remove-Item -Path $modDir -Recurse -Force -ErrorAction SilentlyContinue
    
    # If still exists, warn user
    if (Test-Path $modDir) {
        if ($Force) {
            Write-Host -ForegroundColor Yellow "Warning: Could not remove old folder (probably locked by BitDefender)"
            Write-Host -ForegroundColor Yellow "Attempting to overwrite files instead..."
        } else {
            Write-Host -ForegroundColor Red "Error: Could not remove $modDir"
            Write-Host -ForegroundColor Yellow "This is likely because BitDefender is scanning the files."
            Write-Host -ForegroundColor Yellow "Options:"
            Write-Host -ForegroundColor Yellow "  1. Close RimWorld if it's running"
            Write-Host -ForegroundColor Yellow "  2. Wait a moment and try again"
            Write-Host -ForegroundColor Yellow "  3. Temporarily disable BitDefender's real-time protection"
            Write-Host -ForegroundColor Yellow "  4. Run with -Force flag to attempt overwrite"
            exit 1
        }
    }
}

# Copy new files
Write-Host "Copying mod to RimWorld Mods folder..."
Copy-Item -Path $distDir -Destination $modDir -Recurse -Force

Write-Host -ForegroundColor Green "Deployment complete!"
Write-Host "Mod deployed to: $modDir"
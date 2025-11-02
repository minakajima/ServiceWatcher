# ServiceWatcher Manual Test Helper Script
# Based on quickstart.md scenarios

param(
    [Parameter(Mandatory=$false)]
    [ValidateRange(1,10)]
    [int]$Scenario = 0,
    
    [Parameter(Mandatory=$false)]
    [switch]$Cleanup
)

$ErrorActionPreference = "Stop"
$configPath = "$env:LOCALAPPDATA\ServiceWatcher\config.json"
$backupPath = "$env:LOCALAPPDATA\ServiceWatcher\config.backup.json"
$logPath = "$env:LOCALAPPDATA\ServiceWatcher\logs\servicewatcher.log"

function Show-Menu {
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  ServiceWatcher Test Scenarios" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. First Launch (Default Configuration)" -ForegroundColor White
    Write-Host "2. Add Service to Monitoring List" -ForegroundColor White
    Write-Host "3. Start Monitoring & Detect Service Stop" -ForegroundColor White
    Write-Host "4. Multiple Service Monitoring" -ForegroundColor White
    Write-Host "5. Configuration Persistence" -ForegroundColor White
    Write-Host "6. Edit Configuration File Directly" -ForegroundColor White
    Write-Host "7. Invalid Configuration Handling" -ForegroundColor White
    Write-Host "8. Performance Test (50 Services)" -ForegroundColor White
    Write-Host "9. Service Not Found Handling" -ForegroundColor White
    Write-Host "10. Notification Display Time" -ForegroundColor White
    Write-Host ""
    Write-Host "C. Cleanup test data" -ForegroundColor Yellow
    Write-Host "Q. Quit" -ForegroundColor Yellow
    Write-Host ""
}

function Test-Scenario1 {
    Write-Host "`n=== Scenario 1: First Launch ===" -ForegroundColor Green
    Write-Host "Cleaning existing config..." -ForegroundColor Yellow
    
    if (Test-Path $configPath) { Remove-Item $configPath -Force }
    if (Test-Path $backupPath) { Remove-Item $backupPath -Force }
    
    Write-Host "✓ Config cleaned" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Launch: .\bin\Debug\net8.0-windows\ServiceWatcher.exe" -ForegroundColor White
    Write-Host "2. Verify main window appears" -ForegroundColor White
    Write-Host "3. Check config created: Test-Path '$configPath'" -ForegroundColor White
}

function Test-Scenario3 {
    Write-Host "`n=== Scenario 3: Service Stop Detection ===" -ForegroundColor Green
    Write-Host "`nTest service: Print Spooler (Spooler)" -ForegroundColor Yellow
    
    $service = Get-Service -Name Spooler -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "Current status: $($service.Status)" -ForegroundColor White
    } else {
        Write-Host "⚠ Print Spooler service not found!" -ForegroundColor Red
        return
    }
    
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Add 'Spooler' service in app" -ForegroundColor White
    Write-Host "2. Start monitoring" -ForegroundColor White
    Write-Host "3. Run: Stop-Service -Name Spooler -Force" -ForegroundColor White
    Write-Host "4. Wait for notification (within 6 seconds)" -ForegroundColor White
    Write-Host "5. Restart: Start-Service -Name Spooler" -ForegroundColor White
}

function Test-Scenario4 {
    Write-Host "`n=== Scenario 4: Multiple Services ===" -ForegroundColor Green
    
    $testServices = @("Spooler", "BITS", "wuauserv")
    Write-Host "`nTest services:" -ForegroundColor Yellow
    
    foreach ($svc in $testServices) {
        $service = Get-Service -Name $svc -ErrorAction SilentlyContinue
        if ($service) {
            Write-Host "  ✓ $svc - $($service.Status)" -ForegroundColor Green
        } else {
            Write-Host "  ✗ $svc - Not found" -ForegroundColor Red
        }
    }
    
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Add all services to monitoring list" -ForegroundColor White
    Write-Host "2. Start monitoring" -ForegroundColor White
    Write-Host "3. Run: Stop-Service -Name Spooler, BITS -Force" -ForegroundColor White
    Write-Host "4. Verify 2 notifications appear" -ForegroundColor White
    Write-Host "5. Restart: Start-Service -Name Spooler, BITS" -ForegroundColor White
}

function Test-Scenario7 {
    Write-Host "`n=== Scenario 7: Invalid Config ===" -ForegroundColor Green
    
    if (Test-Path $configPath) {
        Write-Host "Backing up current config..." -ForegroundColor Yellow
        Copy-Item $configPath "$configPath.good" -Force
        Write-Host "✓ Backup created: $configPath.good" -ForegroundColor Green
    }
    
    Write-Host "`nCreating corrupted config..." -ForegroundColor Yellow
    "{ invalid json }" | Out-File $configPath -Encoding UTF8
    Write-Host "✓ Corrupted config created" -ForegroundColor Green
    
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Launch application" -ForegroundColor White
    Write-Host "2. Verify app starts without crash" -ForegroundColor White
    Write-Host "3. Check if default config loaded or backup restored" -ForegroundColor White
    Write-Host "4. Restore: Copy-Item '$configPath.good' '$configPath' -Force" -ForegroundColor White
}

function Test-Scenario8 {
    Write-Host "`n=== Scenario 8: Performance (50 Services) ===" -ForegroundColor Green
    Write-Host "Generating config with 50 services..." -ForegroundColor Yellow
    
    $services = Get-Service | Select-Object -First 50
    $monitoredServices = $services | ForEach-Object {
        @{
            serviceName = $_.Name
            displayName = $_.DisplayName
            notificationEnabled = $true
            lastKnownStatus = 0
            isAvailable = $true
            errorMessage = ""
            lastChecked = Get-Date -Format "o"
        }
    }
    
    $config = @{
        monitoringIntervalSeconds = 5
        notificationDisplayTimeSeconds = 30
        monitoredServices = $monitoredServices
        configurationVersion = "1.0"
        lastModified = Get-Date -Format "o"
        startMinimized = $false
        autoStartMonitoring = $false
    }
    
    $configDir = Split-Path $configPath -Parent
    if (-not (Test-Path $configDir)) {
        New-Item -ItemType Directory -Path $configDir -Force | Out-Null
    }
    
    $config | ConvertTo-Json -Depth 10 | Out-File $configPath -Encoding UTF8
    Write-Host "✓ Config created with $($services.Count) services" -ForegroundColor Green
    
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Launch application (measure startup time <3s)" -ForegroundColor White
    Write-Host "2. Start monitoring" -ForegroundColor White
    Write-Host "3. Open Task Manager" -ForegroundColor White
    Write-Host "4. Verify: Memory <50MB, CPU <1%" -ForegroundColor White
    Write-Host "5. Let run for 5 minutes, monitor stability" -ForegroundColor White
}

function Invoke-Cleanup {
    Write-Host "`n=== Cleanup Test Data ===" -ForegroundColor Yellow
    
    $items = @(
        $configPath,
        $backupPath,
        "$configPath.good"
    )
    
    foreach ($item in $items) {
        if (Test-Path $item) {
            Remove-Item $item -Force
            Write-Host "✓ Removed: $item" -ForegroundColor Green
        }
    }
    
    Write-Host "`nRestarting test services..." -ForegroundColor Yellow
    $testServices = @("Spooler", "BITS", "wuauserv")
    foreach ($svc in $testServices) {
        $service = Get-Service -Name $svc -ErrorAction SilentlyContinue
        if ($service -and $service.Status -ne "Running") {
            Start-Service -Name $svc -ErrorAction SilentlyContinue
            Write-Host "✓ Started: $svc" -ForegroundColor Green
        }
    }
    
    Write-Host "`n✓ Cleanup complete!" -ForegroundColor Green
}

# Main execution
if ($Cleanup) {
    Invoke-Cleanup
    exit
}

if ($Scenario -gt 0) {
    switch ($Scenario) {
        1 { Test-Scenario1 }
        3 { Test-Scenario3 }
        4 { Test-Scenario4 }
        7 { Test-Scenario7 }
        8 { Test-Scenario8 }
        default {
            Write-Host "Scenario $Scenario not yet implemented in helper script." -ForegroundColor Yellow
            Write-Host "Please refer to quickstart.md for manual steps." -ForegroundColor White
        }
    }
} else {
    # Interactive mode
    while ($true) {
        Show-Menu
        $choice = Read-Host "Select scenario (1-10, C, Q)"
        
        switch ($choice.ToUpper()) {
            "1" { Test-Scenario1 }
            "3" { Test-Scenario3 }
            "4" { Test-Scenario4 }
            "7" { Test-Scenario7 }
            "8" { Test-Scenario8 }
            "C" { Invoke-Cleanup }
            "Q" { Write-Host "`nGoodbye!" -ForegroundColor Cyan; exit }
            default {
                if ($choice -match "^[2-6,9-10]$") {
                    Write-Host "`nScenario $choice: Please refer to quickstart.md" -ForegroundColor Yellow
                } else {
                    Write-Host "`nInvalid choice!" -ForegroundColor Red
                }
            }
        }
        
        Write-Host "`nPress any key to continue..." -ForegroundColor Gray
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    }
}

# Quickstart Guide: Windows Service Monitor

**Feature**: 001-service-monitor  
**Date**: 2025-10-30  
**Target Audience**: Developers, QA Testers

## Purpose

This guide provides step-by-step instructions for manually testing the Windows Service Monitor application during development. Use this to verify functionality before automated tests are written.

## Prerequisites

### Environment Setup

- **OS**: Windows 10 (21H2+), Windows 11, or Windows Server 2016+
- **Runtime**: .NET 8.0 Runtime (or SDK for development)
- **Permissions**: Standard user (no admin required for most operations)
- **Test Services**: At least 2-3 Windows services available (see Test Services section)

### Test Services

**Recommended Services for Testing**:

| Service Name | Display Name | Notes |
|--------------|--------------|-------|
| `wuauserv` | Windows Update | Safe to stop, automatically restarts |
| `Spooler` | Print Spooler | Safe to stop, easy to restart |
| `W32Time` | Windows Time | Low impact if stopped temporarily |
| `BITS` | Background Intelligent Transfer Service | Used by Windows Update, safe to test |

**⚠ Warning**: Do NOT test with critical services like:
- `WinDefend` (Windows Defender)
- `LanmanServer` (Server service)
- `Dhcp` (DHCP Client)

### Build the Application

```powershell
# From project root
cd ServiceWatcher
dotnet build -c Debug
```

Expected output: `ServiceWatcher.exe` in `bin\Debug\net8.0-windows\`

## Test Scenarios

### Scenario 1: First Launch (Default Configuration)

**Objective**: Verify application creates default configuration on first run.

#### Steps

1. **Clean Start**:
   ```powershell
   # Delete config if it exists
   Remove-Item "$env:LOCALAPPDATA\ServiceWatcher\config.json" -ErrorAction SilentlyContinue
   Remove-Item "$env:LOCALAPPDATA\ServiceWatcher\config.backup.json" -ErrorAction SilentlyContinue
   ```

2. **Launch Application**:
   ```powershell
   .\bin\Debug\net8.0-windows\ServiceWatcher.exe
   ```

3. **Verify UI**:
   - Main window appears
   - Title: "Service Watcher"
   - Monitoring status shows "停止中" (Stopped)
   - Service list shows 1 default service (Windows Update)

4. **Verify Configuration File**:
   ```powershell
   # Check config file was created
   Test-Path "$env:LOCALAPPDATA\ServiceWatcher\config.json"
   
   # View content
   Get-Content "$env:LOCALAPPDATA\ServiceWatcher\config.json"
   ```

#### Expected Results

✅ **PASS** if:
- Application starts without errors
- Config file created at `%LOCALAPPDATA%\ServiceWatcher\config.json`
- JSON file is valid and readable
- Contains 1 default monitored service

❌ **FAIL** if:
- Application crashes on startup
- Config file not created
- Invalid JSON format

---

### Scenario 2: Add Service to Monitoring List

**Objective**: Add a Windows service to the monitoring list.

#### Steps

1. **Launch Application** (if not running)

2. **Open Service Selection**:
   - Click "サービスを追加" (Add Service) button

3. **Select Service**:
   - From dropdown, select "Print Spooler" (or another test service)
   - Or type service name: `Spooler`
   - Click "追加" (Add)

4. **Verify Service Added**:
   - Service appears in monitoring list
   - Shows current status (likely "Running")
   - Notification checkbox is enabled

5. **Verify Configuration Saved**:
   ```powershell
   Get-Content "$env:LOCALAPPDATA\ServiceWatcher\config.json" | ConvertFrom-Json | 
       Select-Object -ExpandProperty monitoredServices
   ```

#### Expected Results

✅ **PASS** if:
- Service appears in list with correct display name
- Configuration file updated with new service
- UI refreshes immediately

❌ **FAIL** if:
- Service not added to list
- Configuration not saved
- Duplicate service allowed

---

### Scenario 3: Start Monitoring & Detect Service Stop

**Objective**: Verify application detects when a monitored service stops.

#### Steps

1. **Add Test Service**:
   - Add "Print Spooler" service (as in Scenario 2)
   - Verify it shows "Running" status

2. **Start Monitoring**:
   - Click "監視開始" (Start Monitoring) button
   - Verify status changes to "監視中" (Monitoring)
   - Button becomes disabled, "監視停止" (Stop Monitoring) enabled

3. **Stop Service Manually**:
   ```powershell
   # Open PowerShell as Administrator
   Stop-Service -Name Spooler -Force
   ```

4. **Wait for Detection**:
   - Wait up to 5-6 seconds (default polling interval)
   - Notification popup should appear

5. **Verify Notification**:
   - Popup shows at bottom-right of screen
   - Contains:
     - Service name: "Print Spooler"
     - Message: "サービスが停止しました"
     - Timestamp (current time)
     - OK button

6. **Verify Service List Updates**:
   - Service status in main list changes to "Stopped" or "停止中"
   - Last checked timestamp updates

7. **Close Notification**:
   - Click "OK" button
   - Notification closes

8. **Restart Service** (cleanup):
   ```powershell
   Start-Service -Name Spooler
   ```

#### Expected Results

✅ **PASS** if:
- Notification appears within 6 seconds of service stop
- Notification contains correct service info
- UI updates to show "Stopped" status
- Notification can be closed
- No errors in application

❌ **FAIL** if:
- No notification appears
- Notification missing information
- Detection takes >10 seconds
- Application crashes
- Multiple notifications for same event

---

### Scenario 4: Multiple Service Monitoring

**Objective**: Monitor multiple services simultaneously.

#### Steps

1. **Add Multiple Services**:
   - Add 3 services: Print Spooler, Windows Update, BITS
   - Verify all show "Running" status

2. **Start Monitoring**

3. **Stop Two Services Quickly**:
   ```powershell
   # Run as Administrator
   Stop-Service -Name Spooler, BITS -Force
   ```

4. **Verify Notifications**:
   - Two notifications should appear
   - Stacked vertically at bottom-right
   - Each shows correct service name

5. **Verify No Duplicate Notifications**:
   - Wait 15 seconds (3 monitoring cycles)
   - Ensure no duplicate notifications appear

6. **Close All Notifications**:
   - Click each OK button individually
   - Or use "すべて閉じる" (Close All) button if available

7. **Restart Services** (cleanup):
   ```powershell
   Start-Service -Name Spooler, BITS
   ```

#### Expected Results

✅ **PASS** if:
- Both notifications appear
- Notifications stack correctly (don't overlap)
- No duplicate notifications
- Each notification is independent

❌ **FAIL** if:
- Missing notifications
- Overlapping notifications
- Duplicate notifications
- Only one notification when two services stopped

---

### Scenario 5: Configuration Persistence

**Objective**: Verify configuration persists across application restarts.

#### Steps

1. **Configure Application**:
   - Add 2-3 services
   - Change monitoring interval to 10 seconds
   - Enable/disable notifications for some services

2. **Verify Configuration Saved**:
   ```powershell
   Get-Content "$env:LOCALAPPDATA\ServiceWatcher\config.json"
   ```
   - Verify JSON contains all services and settings

3. **Close Application**

4. **Restart Application**

5. **Verify Settings Restored**:
   - All services still in list
   - Monitoring interval is 10 seconds
   - Notification settings match previous state

#### Expected Results

✅ **PASS** if:
- All services restored after restart
- Settings match previous configuration
- Monitoring can start immediately with restored config

❌ **FAIL** if:
- Services lost after restart
- Settings reset to defaults
- Configuration corrupted

---

### Scenario 6: Edit Configuration File Directly

**Objective**: Verify application handles external config file changes.

#### Steps

1. **Stop Application** (if running)

2. **Edit Config File**:
   ```powershell
   notepad "$env:LOCALAPPDATA\ServiceWatcher\config.json"
   ```
   
   Modify:
   ```json
   {
     "monitoringIntervalSeconds": 15,
     "notificationDisplayTimeSeconds": 60
   }
   ```

3. **Save and Close Notepad**

4. **Restart Application**

5. **Verify Settings Applied**:
   - Monitoring interval shows 15 seconds in UI
   - Notification display time is 60 seconds

6. **Test Notification Display**:
   - Start monitoring
   - Stop a service
   - Verify notification stays open for ~60 seconds

#### Expected Results

✅ **PASS** if:
- Manual config changes are loaded
- Application doesn't crash on startup
- Settings applied correctly

❌ **FAIL** if:
- Config changes ignored
- Application crashes with manual config
- Settings not reflected in UI

---

### Scenario 7: Invalid Configuration Handling

**Objective**: Verify graceful handling of corrupted/invalid configuration.

#### Steps

1. **Stop Application**

2. **Corrupt Config File**:
   ```powershell
   # Backup first
   Copy-Item "$env:LOCALAPPDATA\ServiceWatcher\config.json" `
             "$env:LOCALAPPDATA\ServiceWatcher\config.good.json"
   
   # Create invalid JSON
   "{ invalid json }" | Out-File "$env:LOCALAPPDATA\ServiceWatcher\config.json"
   ```

3. **Restart Application**

4. **Verify Recovery**:
   - Application should start (not crash)
   - Shows default configuration or restores from backup
   - Logs error about invalid config

5. **Check Backup Used**:
   ```powershell
   # If backup exists, it should be loaded
   Test-Path "$env:LOCALAPPDATA\ServiceWatcher\config.backup.json"
   ```

6. **Restore Good Config**:
   ```powershell
   Copy-Item "$env:LOCALAPPDATA\ServiceWatcher\config.good.json" `
             "$env:LOCALAPPDATA\ServiceWatcher\config.json" -Force
   ```

#### Expected Results

✅ **PASS** if:
- Application starts despite invalid config
- Loads backup or creates default
- Shows error message to user
- Application remains functional

❌ **FAIL** if:
- Application crashes on startup
- No error message shown
- User data lost (backup not used)

---

### Scenario 8: Performance Test (50 Services)

**Objective**: Verify performance with maximum supported service count.

#### Steps

1. **Stop Application**

2. **Create Large Config**:
   ```powershell
   # PowerShell script to generate config with 50 services
   $services = Get-Service | Select-Object -First 50
   $monitoredServices = $services | ForEach-Object {
       @{
           serviceName = $_.Name
           displayName = $_.DisplayName
           notificationEnabled = $true
           lastKnownStatus = 0
           isAvailable = $true
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
   
   $config | ConvertTo-Json -Depth 10 | 
       Out-File "$env:LOCALAPPDATA\ServiceWatcher\config.json" -Encoding UTF8
   ```

3. **Start Application**:
   - Measure startup time (should be <3 seconds)

4. **Start Monitoring**:
   - Verify all 50 services are monitored
   - Check Task Manager for:
     - Memory usage: <50MB
     - CPU usage: <1% (average over 1 minute)

5. **Let Run for 5 Minutes**:
   - Monitor memory (should not increase significantly)
   - Verify no crashes or errors

6. **Stop Monitoring**

#### Expected Results

✅ **PASS** if:
- Application loads 50 services without issue
- Memory stays under 50MB
- CPU usage under 1%
- No performance degradation over time
- UI remains responsive

❌ **FAIL** if:
- Startup takes >5 seconds
- Memory exceeds 50MB
- CPU usage spikes >5%
- Application becomes unresponsive

---

### Scenario 9: Service Not Found Handling

**Objective**: Handle monitoring a service that doesn't exist.

#### Steps

1. **Manually Edit Config**:
   Add fake service:
   ```json
   {
     "serviceName": "FakeService12345",
     "displayName": "Non-Existent Service",
     "notificationEnabled": true
   }
   ```

2. **Restart Application**

3. **Start Monitoring**

4. **Check Service List**:
   - "FakeService12345" should show error state
   - Error message: "サービスが見つかりません" or similar
   - Other services continue monitoring normally

5. **Verify Logs**:
   ```powershell
   Get-Content "$env:LOCALAPPDATA\ServiceWatcher\logs\servicewatcher.log" -Tail 20
   ```
   - Should contain warning about service not found

#### Expected Results

✅ **PASS** if:
- Application handles missing service gracefully
- Error shown in UI for that service
- Other services continue monitoring
- No crash or hang

❌ **FAIL** if:
- Application crashes
- All monitoring stops
- No error message shown

---

### Scenario 10: Notification Display Time

**Objective**: Verify notification auto-close and manual close.

#### Steps

1. **Configure Short Display Time**:
   - Set `notificationDisplayTimeSeconds` to 10 in config

2. **Start Monitoring**

3. **Stop a Service**

4. **Wait for Notification**:
   - Notification appears
   - Start timer on phone or watch

5. **Verify Auto-Close**:
   - Notification should auto-close after ~10 seconds
   - Verify it closes without user interaction

6. **Test Manual Close**:
   - Stop another service
   - Click "OK" button immediately
   - Verify notification closes

7. **Test Infinite Display** (Optional):
   - Set `notificationDisplayTimeSeconds` to 0
   - Stop a service
   - Notification should stay open indefinitely
   - Must manually close

#### Expected Results

✅ **PASS** if:
- Auto-close works after specified time
- Manual close works immediately
- Infinite display (0) keeps notification open

❌ **FAIL** if:
- Auto-close doesn't work
- Manual close doesn't respond
- Timing is off by >2 seconds

---

### Scenario 11: Language Switching (i18n)

**Objective**: Verify UI language switching between Japanese and English without restart.

#### Steps

1. **Verify Initial Language**:
   - Check OS language settings
   - Launch application
   - If OS is Japanese → UI should be in Japanese
   - If OS is English or other → UI should be in English

2. **Open Settings**:
   - Navigate to Settings screen/dialog
   - Locate language dropdown control

3. **Switch to English**:
   - Select "English" from dropdown
   - Observe UI immediately (no restart)
   - All labels, buttons, menus should change to English
   - Time to complete: <1 second (SC-008)

4. **Verify All Elements**:
   - Main window title
   - Menu items
   - Button labels
   - Status bar text
   - Notification messages (trigger a service stop event)

5. **Switch Back to Japanese**:
   - Select "日本語" from dropdown
   - Verify all UI elements switch back
   - Time to complete: <1 second

6. **Verify Persistence**:
   - Close application
   - Check config.json:
     ```powershell
     Get-Content "$env:LOCALAPPDATA\ServiceWatcher\config.json" | ConvertFrom-Json | Select-Object uiLanguage
     ```
   - Verify `uiLanguage` matches last selection

7. **Restart Application**:
   - Launch application again
   - Verify language persists from config (not reset to OS language)

#### Expected Results

✅ **PASS** if:
- Language detection works on first launch
- Language dropdown is accessible in Settings
- Switching completes in <1 second
- All UI elements (forms, labels, buttons, notifications) reflect language change immediately
- Language selection persists across restarts
- No restart required for change

❌ **FAIL** if:
- Language switch takes >1 second
- Some UI elements don't translate
- Restart required for change to take effect
- Language doesn't persist in config.json
- Invalid language in config causes crash

---

## Testing Checklist

Copy this checklist for each test session:

```markdown
## Test Session: [Date] - [Tester Name]

### Environment
- [ ] OS: ______________
- [ ] .NET Version: ______________
- [ ] Build Configuration: [ ] Debug [ ] Release

### Scenario Results
- [ ] 1. First Launch
- [ ] 2. Add Service
- [ ] 3. Detect Service Stop
- [ ] 4. Multiple Services
- [ ] 5. Configuration Persistence
- [ ] 6. Edit Config Directly
- [ ] 7. Invalid Config Handling
- [ ] 8. Performance (50 services)
- [ ] 9. Service Not Found
- [ ] 10. Notification Display Time

### Issues Found
1. ___________________________________
2. ___________________________________
3. ___________________________________

### Notes
_____________________________________
_____________________________________
```

## Common Issues & Troubleshooting

### Issue: Notification Doesn't Appear

**Possible Causes**:
- Service status polling hasn't run yet (wait 5-10 seconds)
- Notification disabled for that service in config
- Service was already stopped before monitoring started
- UI thread blocked

**Debugging**:
```powershell
# Check if service actually stopped
Get-Service -Name Spooler | Select-Object Status

# Check logs
Get-Content "$env:LOCALAPPDATA\ServiceWatcher\logs\servicewatcher.log" -Tail 30
```

### Issue: "Access Denied" Error

**Cause**: User lacks permissions to query certain services.

**Solution**:
- Run application as Administrator (right-click → Run as administrator)
- Or avoid monitoring system-critical services

### Issue: Configuration File Missing

**Debugging**:
```powershell
# Check expected location
Test-Path "$env:LOCALAPPDATA\ServiceWatcher\config.json"

# Verify directory exists
Test-Path "$env:LOCALAPPDATA\ServiceWatcher"

# Check permissions
(Get-Acl "$env:LOCALAPPDATA").Access | Format-Table
```

### Issue: High CPU Usage

**Debugging**:
- Check monitoring interval (should be ≥5 seconds)
- Check number of monitored services (<50)
- Look for infinite loops in logs
- Profile with Visual Studio Diagnostic Tools

### Issue: Memory Leak

**Debugging**:
- Let application run for 30 minutes
- Monitor memory in Task Manager
- Take memory snapshot in Visual Studio
- Check for undisposed ServiceController instances

## Test Data Cleanup

After testing, clean up test data:

```powershell
# Stop application first!

# Remove configuration
Remove-Item "$env:LOCALAPPDATA\ServiceWatcher\*" -Recurse -Force

# Ensure all test services are running
Start-Service -Name Spooler, BITS, wuauserv -ErrorAction SilentlyContinue
```

## Automated Testing Notes

This manual quickstart will be supplemented with automated tests:

- **Unit Tests**: xUnit tests for Services, Models, Utils
- **Integration Tests**: Test with real ServiceController
- **UI Tests**: Consider Appium or WinAppDriver for UI automation

Automated tests should cover:
- All data model validation
- Configuration serialization
- Service monitoring logic
- Error handling paths

Manual testing remains important for:
- UI/UX verification
- Visual notification appearance
- Multi-monitor scenarios
- Real-world service interactions

## Next Steps

After completing quickstart testing:
1. Log all issues in project tracking system
2. Write automated tests for scenarios 1-7
3. Create performance benchmark baseline (scenario 8)
4. Document any deviations from spec
5. Update constitution if constraints need adjustment

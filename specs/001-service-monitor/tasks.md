# Tasks: Windowsサービス監視システム

**Feature**: 001-service-monitor  
**Input**: Design documents from `/specs/001-service-monitor/`  
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/, research.md, quickstart.md

## Task Format

**Format**: `- [ ] [TaskID] [P?] [Story?] Description with file path`

**Labels**:
- **[P]**: Parallelizable (can run concurrently with other [P] tasks)
- **[US1]**: User Story 1 (Service monitoring and notification - P1)
- **[US2]**: User Story 2 (Service list display and selection - P2)
- **[US3]**: User Story 3 (Configuration file management - P3)

## Implementation Strategy

**MVP Scope**: User Story 1 (US1) only - Core monitoring and notification
**Incremental Delivery**: US1 → US2 → US3 (each story is independently testable)
**Technology**: C# 12 / .NET 8.0, Windows Forms, System.ServiceProcess

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Initialize project structure and dependencies

- [X] T001 Create Visual Studio solution file at ServiceWatcher.sln
- [X] T002 [P] Create main project file ServiceWatcher.csproj with .NET 8.0 target
- [X] T003 [P] Create test project file tests/ServiceWatcher.Tests.csproj
- [X] T004 [P] Add NuGet packages: System.ServiceProcess, System.Text.Json to ServiceWatcher.csproj
- [X] T005 [P] Add NuGet packages: xUnit, Moq, Microsoft.NET.Test.Sdk to tests project
- [X] T006 [P] Create directory structure: Models/, Services/, UI/, Utils/ in project root
- [X] T007 [P] Create test directory structure: tests/Unit/, tests/Integration/
- [X] T008 [P] Add Microsoft.Extensions.Logging package for logging support
- [X] T009 Create default config.json template file in project root
- [X] T010 Configure project properties: Windows application, enable Windows Forms

---

## Phase 2: Foundational (Core Infrastructure)

**Purpose**: Base classes and utilities needed by all user stories

### Result Type and Validation

- [X] T011 [P] Create Result<T> class in Utils/Result.cs for explicit success/failure handling
- [X] T012 [P] Create ValidationResult class in Utils/ValidationResult.cs with error list
- [X] T013 [P] Create ServiceStatus enum in Models/ServiceStatus.cs mapping to ServiceControllerStatus

### Logging Infrastructure

- [X] T014 [P] Create Logger class in Utils/Logger.cs wrapping Microsoft.Extensions.Logging
- [X] T015 [P] Implement log file rotation logic (10MB max, keep 10 files) in Utils/Logger.cs

### Extension Methods

- [X] T016 [P] Create ServiceControllerExtensions class in Utils/ServiceControllerExtensions.cs
- [X] T017 [P] Add ToServiceStatus() extension method to convert ServiceControllerStatus to ServiceStatus

---

## Phase 3: User Story 1 - Service Monitoring and Notification (P1)

**Goal**: Monitor registered services and show popup notification when service stops  
**Independent Test**: Manually stop a monitored service → popup appears within 1 second  
**Success Criteria**: SC-002 (notification within 1 second), SC-003 (resource usage), SC-007 (24h stability)

### Data Models (US1)

- [X] T018 [P] [US1] Create MonitoredService class in Models/MonitoredService.cs with all properties
- [X] T019 [P] [US1] Create ServiceStatusChange class in Models/ServiceStatusChange.cs with event details
- [X] T020 [P] [US1] Add IsStopEvent helper property to ServiceStatusChange class

### Service Layer - Monitoring (US1)

- [X] T021 [US1] Create IServiceMonitor interface in Services/IServiceMonitor.cs with monitoring operations
- [X] T022 [US1] Implement ServiceMonitor class constructor and fields in Services/ServiceMonitor.cs
- [X] T023 [US1] Implement StartMonitoringAsync method with Timer-based polling in Services/ServiceMonitor.cs
- [X] T024 [US1] Implement StopMonitoringAsync method with graceful cancellation in Services/ServiceMonitor.cs
- [X] T025 [US1] Implement CheckAllServicesAsync method with status comparison in Services/ServiceMonitor.cs
- [X] T026 [US1] Implement GetCurrentStatusAsync helper method in Services/ServiceMonitor.cs
- [X] T027 [US1] Add ServiceStatusChanged event raising logic in Services/ServiceMonitor.cs
- [X] T028 [US1] Add MonitoringError event for exception handling in Services/ServiceMonitor.cs
- [X] T029 [US1] Implement error handling for InvalidOperationException (service not found)
- [X] T030 [US1] Implement error handling for Win32Exception (access denied)

### Service Layer - Notifications (US1)

- [X] T031 [P] [US1] Create INotificationService interface in Services/INotificationService.cs
- [X] T032 [US1] Implement NotificationService class constructor in Services/NotificationService.cs
- [X] T033 [US1] Implement ShowNotification method with SynchronizationContext marshaling
- [X] T034 [US1] Implement CloseAllNotifications method in Services/NotificationService.cs
- [X] T035 [US1] Implement CloseNotification(serviceName) method in Services/NotificationService.cs
- [X] T036 [US1] Add NotificationAcknowledged event in Services/NotificationService.cs

### UI Layer - Notification Popup (US1)

- [X] T037 [P] [US1] Create NotificationForm class in UI/NotificationForm.cs (Windows Form) - Implemented in NotificationService
- [X] T038 [US1] Design NotificationForm UI: icon, service name, message, timestamp, OK button - Simplified inline form
- [X] T039 [US1] Implement auto-close timer logic in NotificationForm (default 30 seconds)
- [X] T040 [US1] Position NotificationForm at bottom-right of screen with stacking support
- [X] T041 [US1] Style NotificationForm with light red background and warning icon - Basic styling applied

### UI Layer - Main Window (US1 - Minimal)

- [X] T042 [P] [US1] Create MainForm class in UI/MainForm.cs (main application window)
- [X] T043 [US1] Design MainForm UI: status label, start/stop monitoring buttons, service list placeholder
- [X] T044 [US1] Implement Program.cs entry point with DI setup (logger, services)
- [X] T045 [US1] Wire ServiceMonitor.ServiceStatusChanged event to NotificationService.ShowNotification
- [X] T046 [US1] Implement Start Monitoring button click handler in MainForm.cs
- [X] T047 [US1] Implement Stop Monitoring button click handler in MainForm.cs
- [X] T048 [US1] Add status label updates on monitoring state changes in MainForm.cs

### Unit Tests (US1)

- [X] T049 [P] [US1] Create ServiceMonitorTests.cs in tests/Unit/ with basic test structure
- [X] T050 [P] [US1] Test ServiceMonitor.StartMonitoringAsync starts timer correctly
- [X] T051 [P] [US1] Test ServiceMonitor.StopMonitoringAsync cancels timer correctly
- [X] T052 [P] [US1] Test service status change detection (Running → Stopped) raises event
- [X] T053 [P] [US1] Test NotificationService.ShowNotification creates form correctly
- [X] T054 [P] [US1] Test NotificationService.CloseAllNotifications closes all forms

### Integration Tests (US1)

- [X] T055 [US1] Create WindowsServiceTests.cs in tests/Integration/
- [X] T056 [US1] Test real ServiceController interaction (read-only, safe services only)
- [X] T057 [US1] Test monitoring loop with mock service state transitions
- [X] T058 [US1] Test error handling when service doesn't exist (InvalidOperationException)

---

## Phase 4: User Story 2 - Service List Display and Selection (P2)

**Goal**: Display all Windows services and allow user to select/register for monitoring  
**Independent Test**: Open service list → see all services → register one → saved to config  
**Success Criteria**: SC-001 (register within 30 seconds), SC-005 (search within 3 seconds)

### Data Models (US2)

- [ ] T059 [P] [US2] Create ApplicationConfiguration class in Models/ApplicationConfiguration.cs
- [ ] T060 [P] [US2] Add MonitoredServices list property to ApplicationConfiguration

### Service Layer - Configuration (US2)

- [ ] T061 [P] [US2] Create IConfigurationManager interface in Services/IConfigurationManager.cs
- [ ] T062 [US2] Implement ConfigurationManager class constructor in Services/ConfigurationManager.cs
- [ ] T063 [US2] Implement LoadAsync method with JSON deserialization in Services/ConfigurationManager.cs
- [ ] T064 [US2] Implement SaveAsync method with backup creation in Services/ConfigurationManager.cs
- [ ] T065 [US2] Implement CreateDefaultAsync method for first-run config in Services/ConfigurationManager.cs
- [ ] T066 [US2] Implement Validate method with ConfigurationValidator in Services/ConfigurationManager.cs
- [ ] T067 [US2] Create ConfigurationValidator class with all validation rules in Services/ConfigurationValidator.cs
- [ ] T068 [US2] Implement TryLoadBackupAsync helper for corrupted config recovery
- [ ] T069 [US2] Add ConfigurationChanged event in Services/ConfigurationManager.cs

### Service Layer - Monitoring Extensions (US2)

- [ ] T070 [US2] Implement AddServiceAsync method in Services/ServiceMonitor.cs
- [ ] T071 [US2] Implement RemoveServiceAsync method in Services/ServiceMonitor.cs
- [ ] T072 [US2] Implement RefreshMonitoredServicesAsync method in Services/ServiceMonitor.cs
- [ ] T073 [US2] Implement GetServiceStatusesAsync method in Services/ServiceMonitor.cs

### UI Layer - Service List Form (US2)

- [ ] T074 [P] [US2] Create ServiceListForm class in UI/ServiceListForm.cs
- [ ] T075 [US2] Design ServiceListForm UI: DataGridView for services, search box, add/remove buttons
- [ ] T076 [US2] Implement LoadAllServices method using ServiceController.GetServices()
- [ ] T077 [US2] Implement search/filter functionality by service name
- [ ] T078 [US2] Implement Add Service button handler with ConfigurationManager.SaveAsync
- [ ] T079 [US2] Implement Remove Service button handler with ConfigurationManager.SaveAsync
- [ ] T080 [US2] Add monitored services list display (separate grid or highlight)

### UI Layer - Main Window Integration (US2)

- [ ] T081 [US2] Add "Add Service" button to MainForm.cs
- [ ] T082 [US2] Implement Add Service button click handler to open ServiceListForm
- [ ] T083 [US2] Add monitored services DataGridView to MainForm.cs
- [ ] T084 [US2] Implement RefreshServiceList method to update monitored services display
- [ ] T085 [US2] Wire ConfigurationManager.ConfigurationChanged event to RefreshServiceList

### Unit Tests (US2)

- [ ] T086 [P] [US2] Create ConfigurationManagerTests.cs in tests/Unit/
- [ ] T087 [P] [US2] Test LoadAsync with valid JSON file
- [ ] T088 [P] [US2] Test LoadAsync with invalid JSON (should load backup)
- [ ] T089 [P] [US2] Test SaveAsync creates backup before saving
- [ ] T090 [P] [US2] Test Validate with valid configuration (all checks pass)
- [ ] T091 [P] [US2] Test Validate with invalid configuration (interval out of range, duplicate services, etc.)
- [ ] T092 [P] [US2] Test CreateDefaultAsync generates valid default config

### Integration Tests (US2)

- [ ] T093 [US2] Test full add service flow: UI → ConfigurationManager.SaveAsync → file written
- [ ] T094 [US2] Test configuration reload without restart (FR-010)
- [ ] T095 [US2] Test search performance with 100+ services (should complete in <3 seconds)

---

## Phase 5: User Story 3 - Configuration File Management (P3)

**Goal**: Persist settings to JSON file with backup/restore capability  
**Independent Test**: Edit config.json → restart app → settings loaded correctly  
**Success Criteria**: SC-004 (configuration portability 100%)

### Configuration File Handling (US3)

- [ ] T096 [P] [US3] Implement ReloadAsync method in Services/ConfigurationManager.cs
- [ ] T097 [P] [US3] Implement RestoreFromBackupAsync method in Services/ConfigurationManager.cs
- [ ] T098 [US3] Add file path resolution logic (%LOCALAPPDATA%\ServiceWatcher\config.json)
- [ ] T099 [US3] Implement ConfigurationExists check method in Services/ConfigurationManager.cs

### Configuration Validation (US3)

- [ ] T100 [P] [US3] Add validation for MonitoringIntervalSeconds (1-3600 range)
- [ ] T101 [P] [US3] Add validation for NotificationDisplayTimeSeconds (0-300 range)
- [ ] T102 [P] [US3] Add validation for max services count (50 max per constitution)
- [ ] T103 [P] [US3] Add validation for duplicate service names detection
- [ ] T104 [P] [US3] Add validation for service name max length (256 chars)

### UI Layer - Settings (US3)

- [ ] T105 [P] [US3] Add Settings menu item to MainForm.cs
- [ ] T106 [US3] Create SettingsForm class in UI/SettingsForm.cs (optional - can edit config directly)
- [ ] T107 [US3] Add monitoring interval NumericUpDown control to SettingsForm
- [ ] T108 [US3] Add notification display time NumericUpDown control to SettingsForm
- [ ] T109 [US3] Implement Save Settings button handler with validation
- [ ] T110 [US3] Add StartMinimized checkbox to SettingsForm
- [ ] T111 [US3] Add AutoStartMonitoring checkbox to SettingsForm

### Error Handling (US3)

- [ ] T112 [US3] Implement error handling for corrupted config.json (load backup or default)
- [ ] T113 [US3] Implement error handling for read-only config file (show error dialog)
- [ ] T114 [US3] Implement error handling for missing config directory (create directory)
- [ ] T115 [US3] Add user-friendly error messages for all configuration errors

### Unit Tests (US3)

- [ ] T116 [P] [US3] Test ReloadAsync discards in-memory changes and reloads from file
- [ ] T117 [P] [US3] Test RestoreFromBackupAsync successfully restores from backup
- [ ] T118 [P] [US3] Test error handling when both config and backup are corrupted (use default)
- [ ] T119 [P] [US3] Test validation rules: interval out of range, max services exceeded, etc.
- [ ] T120 [P] [US3] Test configuration file location (%LOCALAPPDATA%)

### Integration Tests (US3)

- [ ] T121 [US3] Test manual config.json edit → app restart → changes loaded (FR-010 validation)
- [ ] T122 [US3] Test config portability: copy config to different machine → same behavior
- [ ] T123 [US3] Test first-run scenario: no config exists → default created automatically (FR-009)

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final touches, documentation, and deployment preparation

### Logging Implementation

- [ ] T124 [P] Log all service state changes (Running → Stopped) with timestamp
- [ ] T125 [P] Log configuration load/save operations with success/failure status
- [ ] T126 [P] Log monitoring start/stop events with service count
- [ ] T127 [P] Log all errors with full exception details and stack traces
- [ ] T128 [P] Implement log file location at %LOCALAPPDATA%\ServiceWatcher\logs\

### Performance Optimization

- [ ] T129 [P] Measure memory usage with 50 services (must be <50MB per SC-003)
- [ ] T130 [P] Measure CPU usage with 20 services (must be <1% per SC-003)
- [ ] T131 [P] Optimize notification display time (must be <1 second per SC-002)
- [ ] T132 [P] Test 24-hour continuous operation stability (SC-007)

### UI Polish

- [ ] T133 [P] Add application icon to MainForm and notification
- [ ] T134 [P] Implement proper application exit (stop monitoring, save config)
- [ ] T135 [P] Add keyboard shortcuts (F5 for refresh, Ctrl+S for save config)
- [ ] T136 [P] Add status bar with monitoring status and service count
- [ ] T137 [P] Implement window state persistence (remember size/position)

### Documentation

- [ ] T138 [P] Create README.md with installation instructions
- [ ] T139 [P] Add inline code comments for complex logic (monitoring loop, error handling)
- [ ] T140 [P] Create CHANGELOG.md documenting v1.0.0 features
- [ ] T141 [P] Add XML documentation comments to all public methods

### Deployment

- [ ] T142 Create publish profile for self-contained .NET 8.0 deployment
- [ ] T143 Test application on Windows 10, Windows 11, Windows Server 2016
- [ ] T144 Create zip package with executable and default config template
- [ ] T145 [P] Add version number to application (AssemblyInfo or project properties)

---

## Task Summary

**Total Tasks**: 145  
**MVP Tasks (US1 only)**: T001-T058 (58 tasks)  
**Full Feature Tasks**: All 145 tasks

### Task Count by Phase

- Phase 1 (Setup): 10 tasks
- Phase 2 (Foundational): 7 tasks
- Phase 3 (US1 - P1): 41 tasks
- Phase 4 (US2 - P2): 37 tasks
- Phase 5 (US3 - P3): 28 tasks
- Phase 6 (Polish): 22 tasks

### Task Count by User Story

- **Setup & Foundational**: 17 tasks
- **US1 (P1 - Monitoring & Notification)**: 41 tasks
- **US2 (P2 - Service List & Selection)**: 37 tasks
- **US3 (P3 - Configuration Management)**: 28 tasks
- **Cross-Cutting (Polish)**: 22 tasks

### Parallel Opportunities

Each phase has **[P]** tagged tasks that can be executed concurrently:

- **Phase 1**: T002-T010 (9 parallel tasks) - Project and directory setup
- **Phase 2**: T011-T017 (7 parallel tasks) - Utility classes
- **Phase 3**: T018-T020, T031, T037, T042, T049-T054 (14 parallel tasks) - Models, interfaces, forms
- **Phase 4**: T059-T061, T074, T086-T092 (12 parallel tasks) - Config models and tests
- **Phase 5**: T096-T104, T105, T116-T120 (17 parallel tasks) - Config validation and tests
- **Phase 6**: T124-T145 (22 parallel tasks) - All polish tasks

---

## Dependencies

### Story Completion Order

```
Phase 1 (Setup) → Phase 2 (Foundational)
                      ↓
              Phase 3 (US1 - P1) ← MVP Milestone
                      ↓
              Phase 4 (US2 - P2)
                      ↓
              Phase 5 (US3 - P3)
                      ↓
              Phase 6 (Polish)
```

**Independent Stories**: US1, US2, US3 are designed to be independently testable. However, for best user experience, implement in priority order (P1 → P2 → P3).

### Task Dependencies Within Each Story

**US1 Dependencies**:
- T018-T020 (Models) → T021-T030 (ServiceMonitor) → T042-T048 (MainForm)
- T031-T036 (INotificationService) → T037-T041 (NotificationForm) → T045 (Event wiring)

**US2 Dependencies**:
- T059-T060 (Config model) → T061-T069 (ConfigurationManager) → T074-T080 (ServiceListForm)
- T070-T073 (ServiceMonitor extensions) requires T021-T022 (ServiceMonitor base)

**US3 Dependencies**:
- Requires T061-T069 (ConfigurationManager from US2)
- T096-T099 (Reload/Restore) extends ConfigurationManager
- T100-T104 (Validation) extends ConfigurationValidator from US2

---

## Testing Strategy

### Unit Tests (58 tests planned)

- **ServiceMonitor**: 12 tests (T049-T054 + additional)
- **NotificationService**: 6 tests (T053-T054 + additional)
- **ConfigurationManager**: 14 tests (T086-T092, T116-T120)
- **Validation**: 6 tests (T119)
- **Models**: 10 tests (validation rules)
- **Utils**: 10 tests (Result<T>, Logger, extensions)

### Integration Tests (12 tests planned)

- **US1**: 4 tests (T055-T058)
- **US2**: 3 tests (T093-T095)
- **US3**: 3 tests (T121-T123)
- **Performance**: 2 tests (T129-T130)

### Manual Tests

Use `quickstart.md` for manual testing scenarios:
- 10 detailed test scenarios covering all user stories
- Performance testing with 50 services (SC-003)
- 24-hour stability test (SC-007)

---

## Implementation Notes

### MVP (Minimum Viable Product)

**Scope**: User Story 1 only (Tasks T001-T058)

**What you get**:
- ✅ Monitor services and show popup notifications
- ✅ Start/Stop monitoring manually
- ✅ Core error handling (service not found, access denied)
- ✅ Basic logging
- ✅ Unit and integration tests for monitoring logic

**What's missing** (add with US2/US3):
- ❌ UI to add/remove services (must edit config.json manually)
- ❌ Service list display and search
- ❌ Settings UI (must edit config.json manually)
- ❌ Configuration validation UI

**MVP is production-ready** for users comfortable with manual config editing.

### Incremental Delivery

1. **Iteration 1** (MVP): Tasks T001-T058 → Deploy as v0.1.0-alpha
2. **Iteration 2** (US2): Tasks T059-T095 → Deploy as v0.2.0-beta
3. **Iteration 3** (US3): Tasks T096-T123 → Deploy as v1.0.0-rc1
4. **Iteration 4** (Polish): Tasks T124-T145 → Deploy as v1.0.0

### Constitution Compliance

All tasks align with the 7 constitution principles:
- ✅ **I. Windows-Native**: Uses ServiceController API
- ✅ **II. User Notification First**: US1 core focus
- ✅ **III. Minimal Resource**: Performance tasks T129-T132
- ✅ **IV. Configuration-Driven**: US3 entire focus
- ✅ **V. Testability**: 70 unit/integration tests planned
- ✅ **VI. Git Management**: Each task → one commit
- ✅ **VII. Feature-Driven Design**: Organized by user story with clear class boundaries

---

## Next Steps

1. **Start with Phase 1** (Setup): Create project structure (T001-T010)
2. **Build Foundation** (Phase 2): Create utility classes (T011-T017)
3. **Implement MVP** (Phase 3): Complete US1 for first testable increment (T018-T058)
4. **Test MVP**: Run manual tests from `quickstart.md` scenarios 1, 3, 7, 8, 9, 10
5. **Commit**: After each task completion per constitution principle VI
6. **Iterate**: Add US2 and US3 incrementally

**Ready to implement**: All tasks have clear file paths and acceptance criteria. Begin with T001! 🚀

# Implementation Tasks: Windowsサービス監視システム# Tasks: Windowsサービス監視システム# Tasks: Windowsサービス監視シスチE��



**Feature**: 001-service-monitor  

**Branch**: `001-service-monitor`  

**Generated**: 2025-11-05  **Input**: Design documents from `/specs/001-service-monitor/`**Feature**: 001-service-monitor  

**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/**Input**: Design documents from `/specs/001-service-monitor/`  

## Task Summary

**Prerequisites**: plan.md, spec.md, data-model.md, contracts/, research.md, quickstart.md

- **Total Tasks**: 75

- **Phases**: 7 (Setup → Foundational → US1-P1 → US2-P2 → US3-P3 → US4-P3 → Polish)**Tests**: This feature specification does not explicitly request tests. Test tasks are EXCLUDED per template guidelines.

- **Parallelizable Tasks**: 28 tasks marked [P]

- **Independent Test Criteria**: Each user story phase is independently testable## Task Format



## Implementation Strategy**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.



**MVP Scope**: User Story 1 (P1) - Service monitoring and notification**Format**: `- [ ] [TaskID] [P?] [Story?] Description with file path`

- Deliverable: Basic service monitoring with popup notifications

- Independent Test: Stop a service → notification appears within 1 second## Format: `[ID] [P?] [Story] Description`



**Incremental Delivery**:**Labels**:

1. **Phase 1-3 (US1)**: Core monitoring engine → Independent value

2. **Phase 4 (US2)**: Add service selection UI → Enhanced usability- **[P]**: Can run in parallel (different files, no dependencies)- **[P]**: Parallelizable (can run concurrently with other [P] tasks)

3. **Phase 5 (US3)**: Add config file management → Enhanced portability

4. **Phase 6 (US4)**: Add i18n support → Global reach- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)- **[US1]**: User Story 1 (Service monitoring and notification - P1)

5. **Phase 7**: Polish and optimization

- Include exact file paths in descriptions- **[US2]**: User Story 2 (Service list display and selection - P2)

---

- **[US3]**: User Story 3 (Configuration file management - P3)

## Phase 1: Setup (Project Initialization)

## Path Conventions

**Goal**: Create project structure and foundational infrastructure.

## Implementation Strategy

**Tasks**:

- **Source**: `ServiceWatcher/` at repository root

- [ ] T001 Create Visual Studio solution file at `ServiceWatcher.sln`

- [ ] T002 Create directory structure: `ServiceWatcher/{Models,Services,UI,Utils,Resources}` and `tests/{Unit/{Models,Services,Utils},Integration/Services}`- **Tests**: `tests/` at repository root**MVP Scope**: User Story 1 (US1) only - Core monitoring and notification

- [ ] T003 Create .NET 8.0 Windows Forms project with `<TargetFramework>net8.0-windows</TargetFramework>` and `<UseWindowsForms>true</UseWindowsForms>` in `ServiceWatcher/ServiceWatcher.csproj`

- [ ] T004 Add NuGet packages: `System.ServiceProcess.ServiceController` (8.0.0+), test projects add `xUnit` (2.6.0+), `xunit.runner.visualstudio`, `Moq` (4.20.0+)- Paths use Windows-style backslashes for .NET project**Incremental Delivery**: US1 ↁEUS2 ↁEUS3 (each story is independently testable)

- [ ] T005 Create `.gitignore` for Visual Studio/C# (bin/, obj/, *.user, .vs/)

**Technology**: C# 12 / .NET 8.0, Windows Forms, System.ServiceProcess

**Completion Criteria**: Solution builds successfully (`dotnet build` returns exit code 0).

---

---

---

## Phase 2: Foundational (Blocking Prerequisites)

## Phase 1: Setup (Shared Infrastructure)

**Goal**: Implement shared infrastructure needed by all user stories.

## Phase 1: Setup (Project Initialization)

**Tasks**:

**Purpose**: Project initialization and basic structure

- [ ] T006 [P] Create `ServiceStatus` enum in `Models/ServiceStatus.cs` with values: Unknown, Running, Stopped, Paused, StartPending, StopPending, PausePending, ContinuePending

- [ ] T007 [P] Create `Result<T>` class in `Utils/Result.cs` with Success/Failure factory methods, IsSuccess property, Error message**Purpose**: Initialize project structure and dependencies

- [ ] T008 [P] Create `Logger` class in `Utils/Logger.cs` with static methods: LogInfo, LogWarning, LogError, LogDebug (console output for now)

- [ ] T009 [P] Create `MonitoredService` model in `Models/MonitoredService.cs` per data-model.md spec (properties: ServiceName, DisplayName, NotificationEnabled, LastKnownStatus, LastChecked, IsAvailable, ErrorMessage)- [ ] T001 Create directory structure: ServiceWatcher/{Models,Services,UI,Utils,Resources}

- [ ] T010 [P] Create `ApplicationConfiguration` model in `Models/ApplicationConfiguration.cs` per data-model.md spec (properties: MonitoringIntervalSeconds, NotificationDisplayTimeSeconds, UiLanguage, MonitoredServices list, ConfigurationVersion, LastModified, StartMinimized, AutoStartMonitoring)

- [ ] T011 [P] Create `ServiceStatusChange` model in `Models/ServiceStatusChange.cs` (properties: ServiceName, DisplayName, PreviousStatus, CurrentStatus, Timestamp, StopReason)- [ ] T002 Create directory structure: tests/{Unit/{Models,Services,Utils},Integration/Services}- [X] T001 Create Visual Studio solution file at ServiceWatcher.sln

- [ ] T012 [P] Create `IServiceMonitor` interface in `Services/IServiceMonitor.cs` per contracts/IServiceMonitor.md (methods: StartMonitoringAsync, StopMonitoring, AddService, RemoveService, RefreshService, events: ServiceStatusChanged, MonitoringError)

- [ ] T013 [P] Create `INotificationService` interface in `Services/INotificationService.cs` per contracts/INotificationService.md (methods: ShowNotification, HideNotification, HideAllNotifications)- [ ] T003 Initialize .NET 8.0 Windows Forms project in ServiceWatcher/ with ServiceWatcher.csproj- [X] T002 [P] Create main project file ServiceWatcher.csproj with .NET 8.0 target

- [ ] T014 [P] Create `IConfigurationManager` interface in `Services/IConfigurationManager.cs` per contracts/IConfigurationManager.md (methods: LoadConfiguration, SaveConfiguration, ValidateConfiguration, GetDefaultConfiguration)

- [ ] T015 [P] Create `ILocalizationService` interface in `Services/ILocalizationService.cs` per contracts/ILocalizationService.md (properties: CurrentLanguage, SupportedLanguages; methods: DetectDefaultLanguage, SetLanguage, ApplyResourcesTo, GetString, GetFormattedString)- [ ] T004 [P] Add NuGet packages: System.ServiceProcess.ServiceController, System.Text.Json- [X] T003 [P] Create test project file tests/ServiceWatcher.Tests.csproj

- [ ] T016 [P] Create `ConfigurationValidator` class in `Utils/ConfigurationValidator.cs` with validation methods per data-model.md (ValidateServiceName, ValidateMonitoringInterval, ValidateLanguageCode, ValidateConfiguration)

- [ ] T005 [P] Configure project properties: OutputType=WinExe, TargetFramework=net8.0-windows, UseWindowsForms=true- [X] T004 [P] Add NuGet packages: System.ServiceProcess, System.Text.Json to ServiceWatcher.csproj

**Completion Criteria**: All foundational types compile without errors, unit tests for validation logic pass.

- [X] T005 [P] Add NuGet packages: xUnit, Moq, Microsoft.NET.Test.Sdk to tests project

**Parallel Execution**: All T006-T016 can be developed in parallel (different files, no dependencies).

**Checkpoint**: Project structure ready for implementation- [X] T006 [P] Create directory structure: Models/, Services/, UI/, Utils/ in project root

---

- [X] T007 [P] Create test directory structure: tests/Unit/, tests/Integration/

## Phase 3: User Story 1 - Service Monitoring and Notification (P1)

---- [X] T008 [P] Add Microsoft.Extensions.Logging package for logging support

**Goal**: Implement core monitoring engine that detects service stops and shows notifications.

- [X] T009 Create default config.json template file in project root

**Independent Test**: 

1. Register a test service (e.g., "Print Spooler")## Phase 2: Foundational (Blocking Prerequisites)- [X] T010 Configure project properties: Windows application, enable Windows Forms

2. Start monitoring

3. Manually stop the service using `services.msc`

4. Verify popup notification appears within 1 second with service name and timestamp

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented---

**Tasks**:



- [ ] T017 [US1] Implement `ServiceMonitor` class in `Services/ServiceMonitor.cs` implementing IServiceMonitor interface

- [ ] T018 [US1] Implement `StartMonitoringAsync` method: create timer with interval from config, start polling loop using `ServiceController.GetServices()` and status checks**⚠️ CRITICAL**: No user story work can begin until this phase is complete## Phase 2: Foundational (Core Infrastructure)

- [ ] T019 [US1] Implement `StopMonitoring` method: cancel monitoring timer, dispose ServiceController instances, set IsMonitoring=false

- [ ] T020 [US1] Implement `CheckServicesAsync` private method: iterate MonitoredServices, call `ServiceController.Refresh()`, compare status, raise ServiceStatusChanged event on status change from Running→Stopped

- [ ] T021 [US1] Implement error handling in ServiceMonitor: catch UnauthorizedAccessException, Win32Exception, InvalidOperationException; raise MonitoringError event with error details

- [ ] T022 [US1] Create `NotificationForm` in `UI/NotificationForm.cs`: Windows Forms form with labels for ServiceName, DisplayName, Timestamp, StopReason; auto-close timer (default 30s); Close button- [ ] T006 [P] Create ServiceStatus enum in ServiceWatcher/Models/ServiceStatus.cs with 8 states (Unknown, Stopped, StartPending, StopPending, Running, ContinuePending, PausePending, Paused)**Purpose**: Base classes and utilities needed by all user stories

- [ ] T023 [US1] Implement `NotificationService` class in `Services/NotificationService.cs` implementing INotificationService: ShowNotification creates NotificationForm instance, positions at bottom-right of screen, shows as topmost

- [ ] T024 [US1] Wire ServiceMonitor.ServiceStatusChanged event to NotificationService.ShowNotification in main application entry point or MainForm- [ ] T007 [P] Create Result<T> class in ServiceWatcher/Utils/Result.cs with Success/Failure factory methods



**Completion Criteria**: - [ ] T008 [P] Create MonitoredService model in ServiceWatcher/Models/MonitoredService.cs with 7 properties per data-model.md### Result Type and Validation

- Manual test passes (see Independent Test above)

- SC-002: Notification displays within 1 second of service stop- [ ] T009 [P] Create ApplicationConfiguration model in ServiceWatcher/Models/ApplicationConfiguration.cs with DetectDefaultLanguage() helper

- Multiple service stops generate individual notifications

- [ ] T010 [P] Create ServiceStatusChange model in ServiceWatcher/Models/ServiceStatusChange.cs with IsStopEvent helper property- [X] T011 [P] Create Result<T> class in Utils/Result.cs for explicit success/failure handling

**Dependencies**: Requires Phase 2 (Foundational) complete.

- [ ] T011 Create ConfigurationValidator in ServiceWatcher/Utils/ConfigurationValidator.cs with Validate() method for all validation rules- [X] T012 [P] Create ValidationResult class in Utils/ValidationResult.cs with error list

**Parallel Opportunities**:

- T017-T021 (ServiceMonitor) can be developed in parallel with T022-T023 (NotificationService)- [ ] T012 Create Logger wrapper in ServiceWatcher/Utils/Logger.cs using Microsoft.Extensions.Logging abstractions- [X] T013 [P] Create ServiceStatus enum in Models/ServiceStatus.cs mapping to ServiceControllerStatus



---- [ ] T013 Create IConfigurationManager interface in ServiceWatcher/Services/IConfigurationManager.cs per contracts/IConfigurationManager.md



## Phase 4: User Story 2 - Service List Display and Selection (P2)- [ ] T014 Create IServiceMonitor interface in ServiceWatcher/Services/IServiceMonitor.cs per contracts/IServiceMonitor.md### Logging Infrastructure



**Goal**: Allow users to browse installed services and select which ones to monitor.- [ ] T015 Create INotificationService interface in ServiceWatcher/Services/INotificationService.cs per contracts/INotificationService.md



**Independent Test**:- [ ] T016 Create ILocalizationService interface in ServiceWatcher/Services/ILocalizationService.cs per contracts/ILocalizationService.md- [X] T014 [P] Create Logger class in Utils/Logger.cs wrapping Microsoft.Extensions.Logging

1. Open the application

2. Click "Manage Services" button- [X] T015 [P] Implement log file rotation logic (10MB max, keep 10 files) in Utils/Logger.cs

3. Verify list shows all Windows services (`ServiceController.GetServices()`)

4. Search for "Print Spooler"**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

5. Select and click "Add to Monitoring"

6. Verify service appears in monitored list### Extension Methods

7. Verify config.json is updated

---

**Tasks**:

- [X] T016 [P] Create ServiceControllerExtensions class in Utils/ServiceControllerExtensions.cs

- [ ] T025 [US2] Create `ServiceListForm` in `UI/ServiceListForm.cs`: DataGridView showing ServiceName and DisplayName columns, Search TextBox, Add/Remove buttons, Load button to refresh list

- [ ] T026 [US2] Implement `LoadAllServicesAsync` method in ServiceListForm: call `ServiceController.GetServices()`, filter out system-critical services, populate DataGridView sorted by DisplayName## Phase 3: User Story 1 - サービス監視と通知 (Priority: P1) 🎯 MVP- [X] T017 [P] Add ToServiceStatus() extension method to convert ServiceControllerStatus to ServiceStatus

- [ ] T027 [US2] Implement search functionality in ServiceListForm: filter DataGridView rows by ServiceName or DisplayName contains search text (case-insensitive)

- [ ] T028 [US2] Implement Add button handler in ServiceListForm: get selected service from DataGridView, create MonitoredService instance, call ServiceMonitor.AddService, call ConfigurationManager.SaveConfiguration

- [ ] T029 [US2] Implement Remove button handler in ServiceListForm: get selected monitored service, call ServiceMonitor.RemoveService, call ConfigurationManager.SaveConfiguration

- [ ] T030 [US2] Implement `AddService` method in ServiceMonitor: validate service exists using ServiceController, add to MonitoredServices list, if monitoring active then start watching this service immediately**Goal**: 登録されたWindowsサービスが停止した際に即座にポップアップ通知を受け取り、サービス停止を迅速に認識できる---

- [ ] T031 [US2] Implement `RemoveService` method in ServiceMonitor: remove from MonitoredServices list, dispose ServiceController instance for that service

- [ ] T032 [US2] Create `MainForm` in `UI/MainForm.cs`: basic window with "Start Monitoring" button, "Stop Monitoring" button, "Manage Services" button (opens ServiceListForm), status label showing "Monitoring X services"

- [ ] T033 [US2] Wire MainForm buttons to ServiceMonitor methods (Start/Stop), show service count, open ServiceListForm on "Manage Services" click

**Independent Test**: 監視対象サービスを手動で停止することで、1秒以内にポップアップ通知が表示されることを確認でき、独立した価値を提供します (spec.md US1受け入れシナリオ1-4を参照)## Phase 3: User Story 1 - Service Monitoring and Notification (P1)

**Completion Criteria**:

- Manual test passes (see Independent Test above)

- FR-001: All Windows services are listed

- FR-002: Services can be selected and registered### Implementation for User Story 1**Goal**: Monitor registered services and show popup notification when service stops  

- FR-007: Registered services can be removed

- FR-008: Search filters service list**Independent Test**: Manually stop a monitored service ↁEpopup appears within 1 second  

- SC-005: User can find and register service within 3 seconds

- [ ] T017 [P] [US1] Implement ServiceMonitor class in ServiceWatcher/Services/ServiceMonitor.cs with StartMonitoringAsync(), StopMonitoringAsync(), CheckAllServicesAsync() methods**Success Criteria**: SC-002 (notification within 1 second), SC-003 (resource usage), SC-007 (24h stability)

**Dependencies**: Requires Phase 3 (US1) complete.

- [ ] T018 [P] [US1] Implement NotificationService class in ServiceWatcher/Services/NotificationService.cs with ShowNotification(), CloseAllNotifications() methods

**Parallel Opportunities**:

- T025-T029 (ServiceListForm UI) can be developed in parallel with T030-T031 (ServiceMonitor add/remove logic)- [ ] T019 [US1] Create NotificationForm in ServiceWatcher/UI/NotificationForm.cs with service name label, stop time label, OK button, auto-close timer (FR-011: 30秒デフォルト)### Data Models (US1)

- T032-T033 (MainForm) can start in parallel once ServiceMonitor interface is stable

- [ ] T020 [US1] Implement ServiceMonitor.StartMonitoringAsync() with Timer-based polling loop (FR-003: 5秒間隔デフォルト)

---

- [ ] T021 [US1] Implement ServiceMonitor.CheckAllServicesAsync() with ServiceController.Status checks and status change detection (FR-004: 1秒以内通知)- [X] T018 [P] [US1] Create MonitoredService class in Models/MonitoredService.cs with all properties

## Phase 5: User Story 3 - Configuration File Management (P3)

- [ ] T022 [US1] Wire ServiceMonitor.ServiceStatusChanged event to NotificationService.ShowNotification() with displayTime from config (FR-005: サービス名、表示名、停止時刻、停止理由含む)- [X] T019 [P] [US1] Create ServiceStatusChange class in Models/ServiceStatusChange.cs with event details

**Goal**: Persist settings to JSON file, support manual editing, validate on load.

- [ ] T023 [US1] Add error handling for Win32Exception and InvalidOperationException in ServiceMonitor (FR-012: 管理者権限なしで基本機能提供)- [X] T020 [P] [US1] Add IsStopEvent helper property to ServiceStatusChange class

**Independent Test**:

1. Launch app first time → verify `config.json` created in app directory- [ ] T024 [US1] Add logging for monitoring start/stop, service status changes, notification display, errors in ServiceMonitor and NotificationService

2. Add 2 services to monitoring via UI

3. Close app### Service Layer - Monitoring (US1)

4. Open `config.json` → verify services are listed

5. Manually edit: change MonitoringIntervalSeconds to 10**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently - service monitoring and notification popup work end-to-end

6. Restart app → verify 10-second interval is used

7. Manually corrupt JSON (invalid syntax) → restart → verify error message + default config loaded- [X] T021 [US1] Create IServiceMonitor interface in Services/IServiceMonitor.cs with monitoring operations



**Tasks**:---- [X] T022 [US1] Implement ServiceMonitor class constructor and fields in Services/ServiceMonitor.cs



- [ ] T034 [US3] Implement `ConfigurationManager` class in `Services/ConfigurationManager.cs` implementing IConfigurationManager interface- [X] T023 [US1] Implement StartMonitoringAsync method with Timer-based polling in Services/ServiceMonitor.cs

- [ ] T035 [US3] Implement `GetDefaultConfiguration` method: return ApplicationConfiguration with defaults (MonitoringIntervalSeconds=5, NotificationDisplayTimeSeconds=30, UiLanguage=DetectDefaultLanguage(), empty MonitoredServices list, ConfigurationVersion="1.0")

- [ ] T036 [US3] Implement `LoadConfiguration` method: check if config.json exists at `Path.Combine(AppContext.BaseDirectory, "config.json")`, if not call GetDefaultConfiguration; if exists read JSON using System.Text.Json, validate using ConfigurationValidator, return Result<ApplicationConfiguration>## Phase 4: User Story 2 - サービス一覧表示と選択 (Priority: P2)- [X] T024 [US1] Implement StopMonitoringAsync method with graceful cancellation in Services/ServiceMonitor.cs

- [ ] T037 [US3] Implement `SaveConfiguration` method: serialize ApplicationConfiguration to JSON with WriteIndented=true, update LastModified timestamp, write to config.json atomically (write to temp file, then File.Move with overwrite)

- [ ] T038 [US3] Implement `ValidateConfiguration` method in ConfigurationValidator: check MonitoringIntervalSeconds (1-300), NotificationDisplayTimeSeconds (5-300), UiLanguage ("ja" or "en"), ConfigurationVersion not null, MonitoredServices ServiceNames not empty- [X] T025 [US1] Implement CheckAllServicesAsync method with status comparison in Services/ServiceMonitor.cs

- [ ] T039 [US3] Implement error handling in LoadConfiguration: catch JsonException (invalid JSON), FileNotFoundException (missing file), UnauthorizedAccessException (permission denied); log error and return Failure result with error message

- [ ] T040 [US3] Update Program.cs or MainForm constructor to call ConfigurationManager.LoadConfiguration on startup, handle failure by showing MessageBox with error and using default config**Goal**: システムにインストールされているWindowsサービスの一覧を表示し、監視したいサービスを選択して登録できる- [X] T026 [US1] Implement GetCurrentStatusAsync helper method in Services/ServiceMonitor.cs

- [ ] T041 [US3] Update ServiceMonitor to use configuration values: MonitoringIntervalSeconds for timer interval, load MonitoredServices list from config

- [ ] T042 [US3] Update NotificationService to use configuration value: NotificationDisplayTimeSeconds for auto-close timer- [X] T027 [US1] Add ServiceStatusChanged event raising logic in Services/ServiceMonitor.cs

- [ ] T043 [US3] Wire SaveConfiguration calls in ServiceListForm Add/Remove handlers and any settings changes

- [ ] T044 [US3] Add auto-save on configuration change: subscribe to configuration change events, debounce saves (wait 1 second of inactivity before writing)**Independent Test**: アプリケーションを起動してサービス一覧画面を開き、任意のサービスを選択して登録することで、設定ファイルに保存されることを確認できます (spec.md US2受け入れシナリオ1-4を参照)- [X] T028 [US1] Add MonitoringError event for exception handling in Services/ServiceMonitor.cs



**Completion Criteria**:- [X] T029 [US1] Implement error handling for InvalidOperationException (service not found)

- Manual test passes (see Independent Test above)

- FR-006: Config saved/loaded in JSON format### Implementation for User Story 2- [X] T030 [US1] Implement error handling for Win32Exception (access denied)

- FR-009: Default config auto-created on first launch

- FR-010: Config changes reflected without restart (for monitored services list)

- US3 Scenario 4: Invalid config shows error and uses defaults

- SC-004: Config file is portable (can be copied to another system)- [ ] T025 [P] [US2] Create MainForm in ServiceWatcher/UI/MainForm.cs with monitoring start/stop buttons, service list grid, status bar### Service Layer - Notifications (US1)



**Dependencies**: Requires Phase 4 (US2) complete.- [ ] T026 [P] [US2] Create ServiceListForm in ServiceWatcher/UI/ServiceListForm.cs with all services grid, search textbox, add/remove buttons



**Parallel Opportunities**:- [ ] T027 [US2] Implement ServiceListForm.LoadAllServices() using ServiceController.GetServices() to enumerate all Windows services (FR-001)- [X] T031 [P] [US1] Create INotificationService interface in Services/INotificationService.cs

- T034-T039 (ConfigurationManager implementation) can be done in parallel with T040-T044 (integration with existing components)

- [ ] T028 [US2] Implement ServiceListForm search functionality with service name filtering (FR-008)- [X] T032 [US1] Implement NotificationService class constructor in Services/NotificationService.cs

---

- [ ] T029 [US2] Implement ServiceListForm.AddButton_Click() to add selected service to MonitoredServices list and trigger config save (FR-002)- [X] T033 [US1] Implement ShowNotification method with SynchronizationContext marshaling

## Phase 6: User Story 4 - Language Switching (P3)

- [ ] T030 [US2] Implement ServiceListForm.RemoveButton_Click() to remove service from MonitoredServices and trigger config save (FR-007)- [X] T034 [US1] Implement CloseAllNotifications method in Services/NotificationService.cs

**Goal**: Support Japanese and English UI, detect OS language, allow runtime switching.

- [ ] T031 [US2] Implement MainForm.ServiceListGrid data binding to display ApplicationConfiguration.MonitoredServices with columns: ServiceName, DisplayName, LastKnownStatus, NotificationEnabled- [X] T035 [US1] Implement CloseNotification(serviceName) method in Services/NotificationService.cs

**Independent Test**:

1. Launch app on Japanese OS → verify all UI is Japanese- [ ] T032 [US2] Wire MainForm start/stop buttons to ServiceMonitor.StartMonitoringAsync() and StopMonitoringAsync()- [X] T036 [US1] Add NotificationAcknowledged event in Services/NotificationService.cs

2. Open Settings (new form) → verify language dropdown shows "日本語" and "English"

3. Select "English" → verify all UI elements update immediately (no restart)- [ ] T033 [US2] Add UI state management: disable start when monitoring active, disable stop when inactive, update status bar text

4. Close and restart app → verify English is still active

5. Repeat test on English OS → verify defaults to English, can switch to Japanese### UI Layer - Notification Popup (US1)



**Tasks**:**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - can select services from list, add to monitoring, and receive notifications



- [ ] T045 [P] [US4] Create `Strings.ja.resx` in `Resources/` with Japanese strings: MainForm_Title, MainForm_StartButton, MainForm_StopButton, MainForm_ManageServicesButton, MainForm_SettingsButton, MainForm_StatusLabel, ServiceListForm_Title, ServiceListForm_SearchLabel, ServiceListForm_AddButton, ServiceListForm_RemoveButton, NotificationForm_ServiceStoppedLabel, SettingsForm_Title, SettingsForm_LanguageLabel, SettingsForm_MonitoringIntervalLabel, SettingsForm_NotificationDurationLabel, SettingsForm_SaveButton, SettingsForm_CancelButton- [X] T037 [P] [US1] Create NotificationForm class in UI/NotificationForm.cs (Windows Form) - Implemented in NotificationService

- [ ] T046 [P] [US4] Create `Strings.en.resx` in `Resources/` with English translations of all strings from T045

- [ ] T047 [P] [US4] Set `Strings.ja.resx` as default resource (ResXFileCodeGenerator, PublicResXFileCodeGenerator in .csproj)---- [X] T038 [US1] Design NotificationForm UI: icon, service name, message, timestamp, OK button - Simplified inline form

- [ ] T048 [US4] Implement `LocalizationService` class in `Services/LocalizationService.cs` implementing ILocalizationService interface

- [ ] T049 [US4] Implement `DetectDefaultLanguage` method: read `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName`, return "ja" if equals "ja", else return "en"- [X] T039 [US1] Implement auto-close timer logic in NotificationForm (default 30 seconds)

- [ ] T050 [US4] Implement `SetLanguage` method: validate languageCode is "ja" or "en", set `Thread.CurrentThread.CurrentUICulture = new CultureInfo(languageCode)`, set `CultureInfo.DefaultThreadCurrentUICulture`, update CurrentLanguage property, return Result success

- [ ] T051 [US4] Implement `ApplyResourcesTo` method: use `ComponentResourceManager` to apply resources to form and all child controls recursively, call `form.Text = GetString(form.Name + "_Title")` for form title## Phase 5: User Story 3 - 設定ファイル管理 (Priority: P3)- [X] T040 [US1] Position NotificationForm at bottom-right of screen with stacking support

- [ ] T052 [US4] Implement `GetString` and `GetFormattedString` methods: use ResourceManager to retrieve strings from Strings.resx by key

- [ ] T053 [US4] Create `SettingsForm` in `UI/SettingsForm.cs`: ComboBox for language (items: "日本語", "English"), NumericUpDown for MonitoringIntervalSeconds, NumericUpDown for NotificationDisplayTimeSeconds, Save/Cancel buttons- [X] T041 [US1] Style NotificationForm with light red background and warning icon - Basic styling applied

- [ ] T054 [US4] Implement language dropdown handler in SettingsForm: on SelectedIndexChanged call LocalizationService.SetLanguage("ja" or "en"), call ApplyResourcesTo for all open forms (MainForm, ServiceListForm, NotificationForm, SettingsForm itself)

- [ ] T055 [US4] Update ApplicationConfiguration: add UiLanguage property with default from DetectDefaultLanguage, update ConfigurationManager to save/load UiLanguage**Goal**: 監視設定（監視対象サービスリスト、通知設定、監視間隔など）をファイルで管理し、バックアップや他のシステムへの展開を容易にする

- [ ] T056 [US4] Update Program.cs or MainForm to apply initial language on startup: load UiLanguage from config, call LocalizationService.SetLanguage, then apply resources to all forms before showing

- [ ] T057 [US4] Update all forms (MainForm, ServiceListForm, NotificationForm, SettingsForm) to use resource strings instead of hard-coded text: replace all `Text =` assignments with resource lookups or ApplyResourcesTo calls### UI Layer - Main Window (US1 - Minimal)

- [ ] T058 [US4] Add "Settings" button to MainForm that opens SettingsForm dialog

- [ ] T059 [US4] Ensure language change refreshes all open windows: maintain list of open forms, call ApplyResourcesTo on each when language changes**Independent Test**: 設定ファイルを手動で編集してアプリケーションを再起動し、編集内容が反映されることを確認できます (spec.md US3受け入れシナリオ1-4を参照)



**Completion Criteria**:- [X] T042 [P] [US1] Create MainForm class in UI/MainForm.cs (main application window)

- Manual test passes (see Independent Test above)

- FR-013: Japanese and English supported### Implementation for User Story 3- [X] T043 [US1] Design MainForm UI: status label, start/stop monitoring buttons, service list placeholder

- FR-014: OS language detected and used as default

- FR-015: Language dropdown available in Settings- [X] T044 [US1] Implement Program.cs entry point with DI setup (logger, services)

- FR-016: Language switch takes effect immediately (no restart)

- FR-017: All UI elements (menus, buttons, labels, messages, notifications) translated- [ ] T034 [P] [US3] Implement ConfigurationManager class in ServiceWatcher/Services/ConfigurationManager.cs with LoadAsync(), SaveAsync(), Validate(), CreateDefaultAsync() methods- [X] T045 [US1] Wire ServiceMonitor.ServiceStatusChanged event to NotificationService.ShowNotification

- FR-018: Language saved to config.json and persists

- SC-008: Language switch completes within 1 second- [ ] T035 [P] [US3] Create SettingsForm in ServiceWatcher/UI/SettingsForm.cs with MonitoringIntervalSeconds NumericUpDown, NotificationDisplayTimeSeconds NumericUpDown, Save/Cancel buttons- [X] T046 [US1] Implement Start Monitoring button click handler in MainForm.cs

- US4 all 5 acceptance scenarios pass

- [ ] T036 [US3] Implement ConfigurationManager.GetConfigurationFilePath() to return %LOCALAPPDATA%\ServiceWatcher\config.json- [X] T047 [US1] Implement Stop Monitoring button click handler in MainForm.cs

**Dependencies**: Requires Phase 5 (US3) complete (needs ConfigurationManager for persistence).

- [ ] T037 [US3] Implement ConfigurationManager.LoadAsync() with JSON deserialization using System.Text.Json (FR-006)- [X] T048 [US1] Add status label updates on monitoring state changes in MainForm.cs

**Parallel Opportunities**:

- T045-T047 (resource file creation) can be done first and in parallel- [ ] T038 [US3] Implement ConfigurationManager.CreateDefaultAsync() to generate default config.json with 1 default service (wuauserv) on first run (FR-009)

- T048-T052 (LocalizationService) can be developed in parallel with T053-T054 (SettingsForm UI)

- T055-T059 (integration) must be done sequentially after service and UI are complete- [ ] T039 [US3] Implement ConfigurationManager.SaveAsync() with backup creation (config.backup.json), validation, and atomic file write (FR-010: 再起動不要)### Unit Tests (US1)



---- [ ] T040 [US3] Implement ConfigurationManager.Validate() using ConfigurationValidator for range checks: MonitoringIntervalSeconds 1-3600, NotificationDisplayTimeSeconds 0-300, max 50 services



## Phase 7: Polish and Cross-Cutting Concerns- [ ] T041 [US3] Add error handling in ConfigurationManager.LoadAsync() for file not found, invalid JSON, validation errors (FR-009: デフォルト設定で起動)- [X] T049 [P] [US1] Create ServiceMonitorTests.cs in tests/Unit/ with basic test structure



**Goal**: Add finishing touches, handle edge cases, optimize performance.- [ ] T042 [US3] Wire SettingsForm to ConfigurationManager: load config on form open, save config on Save button click, apply changes without restart- [X] T050 [P] [US1] Test ServiceMonitor.StartMonitoringAsync starts timer correctly



**Tasks**:- [ ] T043 [US3] Implement Program.cs Main() to call ConfigurationManager.LoadAsync() on startup, create default if not found- [X] T051 [P] [US1] Test ServiceMonitor.StopMonitoringAsync cancels timer correctly



- [ ] T060 [P] Add system tray icon: create NotifyIcon component in MainForm, add context menu (Show/Hide, Exit), minimize to tray on close button, restore on double-click- [ ] T044 [US3] Wire ConfigurationManager.ConfigurationChanged event to ServiceMonitor.RefreshMonitoredServicesAsync() for dynamic config reload- [X] T052 [P] [US1] Test service status change detection (Running ↁEStopped) raises event

- [ ] T061 [P] Implement graceful shutdown: in Program.cs or MainForm.OnClosing, call ServiceMonitor.StopMonitoring, save configuration, dispose resources

- [ ] T062 [P] Add logging infrastructure: replace Logger console output with file logging to `logs/ServiceWatcher-{date}.log`, implement log rotation (keep last 7 days)- [X] T053 [P] [US1] Test NotificationService.ShowNotification creates form correctly

- [ ] T063 [P] Handle edge case - service not found: in ServiceMonitor.CheckServicesAsync, catch exceptions when service is uninstalled, mark MonitoredService.IsAvailable=false, set ErrorMessage, skip future checks until user removes it

- [ ] T064 [P] Handle edge case - multiple simultaneous stops: in NotificationService, implement notification queue, show max 3 notifications on screen at once, stack vertically with 10px gap**Checkpoint**: All user stories 1, 2, 3 should now be independently functional - configuration file management, service selection, and monitoring with notifications all work- [X] T054 [P] [US1] Test NotificationService.CloseAllNotifications closes all forms

- [ ] T065 [P] Handle edge case - permission denied: in ServiceMonitor, catch UnauthorizedAccessException when accessing service, show warning notification once per service, mark service with permission issue in UI

- [ ] T066 [P] Optimize performance: implement caching of ServiceController instances (reuse instead of recreating each poll), measure and verify CPU <1% and memory <50MB with 20 monitored services (SC-003 benchmark)

- [ ] T067 [P] Add visual feedback in MainForm: update status label every second with "Monitoring X services - Last check: HH:mm:ss", show green/red indicator for monitoring active/stopped

- [ ] T068 [P] Implement configuration portability test: create unit test that verifies config.json can be deserialized on different machine (no absolute paths, no machine-specific data) - SC-004 validation---### Integration Tests (US1)

- [ ] T069 [P] Add FR-012 test: verify app runs without admin privileges, test service monitoring of user-accessible services, verify graceful degradation for restricted services

- [ ] T070 [P] Add settings validation UI feedback: in SettingsForm, show error message if MonitoringIntervalSeconds <1 or >300, disable Save button until valid

- [ ] T071 [P] Handle sleep/resume: subscribe to SystemEvents.PowerModeChanged, restart monitoring on resume from sleep/hibernate

- [ ] T072 [P] Add About dialog: create AboutForm with app version, copyright, license info, GitHub link## Phase 6: User Story 4 - 言語切り替え (Priority: P3)- [X] T055 [US1] Create WindowsServiceTests.cs in tests/Integration/

- [ ] T073 [P] Implement FR-017 clarification: add comment in code documenting that log messages remain in English (not localized) for consistency

- [ ] T074 [P] Handle edge case for US3 Scenario 4: ensure error dialog for invalid config is localized using LocalizationService.GetString("Error_InvalidConfig")- [X] T056 [US1] Test real ServiceController interaction (read-only, safe services only)

- [ ] T075 [P] Final integration test: run quickstart.md Scenarios 1-11, verify all acceptance criteria pass, measure SC-001 through SC-008 performance metrics

**Goal**: アプリケーションの表示言語を日本語と英語の間で切り替えられるようにし、多言語環境での利用を可能にする- [X] T057 [US1] Test monitoring loop with mock service state transitions

**Completion Criteria**:

- All edge cases from spec.md handled- [X] T058 [US1] Test error handling when service doesn't exist (InvalidOperationException)

- SC-003: Performance benchmark passes (20 services, CPU <1%, memory <50MB)

- SC-007: 24-hour stability test passes**Independent Test**: 設定画面で言語を切り替え、すべてのUI要素が即座に選択した言語で表示されることを確認できます (spec.md US4受け入れシナリオ1-5を参照、quickstart.md Scenario 11)

- All quickstart.md scenarios pass

- FR-012: Non-admin scenarios tested---

- Application is production-ready

### Implementation for User Story 4

**Parallel Opportunities**: All tasks T060-T074 can be developed in parallel (different features/edge cases).

## Phase 4: User Story 2 - Service List Display and Selection (P2)

---

- [ ] T045 [P] [US4] Create Strings.resx in ServiceWatcher/Resources/ with default (fallback) English strings for all UI elements (FR-017)

## Dependencies and Execution Order

- [ ] T046 [P] [US4] Create Strings.ja.resx in ServiceWatcher/Resources/ with Japanese translations for all UI elements (FR-013)**Goal**: Display all Windows services and allow user to select/register for monitoring  

### User Story Completion Order

- [ ] T047 [P] [US4] Create Strings.en.resx in ServiceWatcher/Resources/ with explicit English strings (same as default)**Independent Test**: Open service list ↁEsee all services ↁEregister one ↁEsaved to config  

```

Phase 1 (Setup) → Phase 2 (Foundational) → Phase 3 (US1-P1) → Phase 4 (US2-P2) → Phase 5 (US3-P3) → Phase 6 (US4-P3) → Phase 7 (Polish)- [ ] T048 [US4] Implement LocalizationService class in ServiceWatcher/Services/LocalizationService.cs with DetectDefaultLanguage(), SetLanguage(), ApplyResourcesTo(), GetString(), GetFormattedString() methods per contracts/ILocalizationService.md**Success Criteria**: SC-001 (register within 30 seconds), SC-005 (search within 3 seconds)

```

- [ ] T049 [US4] Implement LocalizationService.DetectDefaultLanguage() using CultureInfo.CurrentUICulture.TwoLetterISOLanguageName (FR-014: OS言語検出、日本語→"ja"、その他→"en")

**Critical Path**:

1. Phase 1-2 MUST complete before any user story work- [ ] T050 [US4] Implement LocalizationService.SetLanguage() to update Thread.CurrentThread.CurrentUICulture and CultureInfo.DefaultThreadCurrentUICulture (FR-016: 即時反映)### Data Models (US2)

2. Phase 3 (US1) MUST complete before Phase 4 (US2) - US2 depends on monitoring engine from US1

3. Phase 4 (US2) MUST complete before Phase 5 (US3) - US3 needs service list from US2 to save- [ ] T051 [US4] Implement LocalizationService.ApplyResourcesTo() using ComponentResourceManager to recursively update form and all child controls (SC-008: <1秒)

4. Phase 5 (US3) MUST complete before Phase 6 (US4) - US4 needs config persistence from US3

5. Phase 7 (Polish) requires all user stories complete- [ ] T052 [US4] Add Language ComboBox to SettingsForm with items "日本語" (ja) and "English" (en) (FR-015)- [x] T059 [P] [US2] Create ApplicationConfiguration class in Models/ApplicationConfiguration.cs



**Parallel Opportunities by Phase**:- [ ] T053 [US4] Implement SettingsForm.LanguageComboBox_SelectedIndexChanged() to call LocalizationService.SetLanguage(), iterate Application.OpenForms, call ApplyResourcesTo() on each, save to config (FR-016, FR-018)- [x] T060 [P] [US2] Add MonitoredServices list property to ApplicationConfiguration

- **Phase 2**: All 11 tasks (T006-T016) can be done in parallel

- **Phase 3**: T017-T021 (monitor) || T022-T023 (notification)- [ ] T054 [US4] Update ApplicationConfiguration.UiLanguage property initialization in Program.cs Main() to call LocalizationService.DetectDefaultLanguage() on first run

- **Phase 4**: T025-T029 (UI) || T030-T031 (logic) || T032-T033 (MainForm)

- **Phase 5**: T034-T039 (manager) || T040-T044 (integration)- [ ] T055 [US4] Set Form.Localizable=true for MainForm, ServiceListForm, SettingsForm, NotificationForm in designer properties### Service Layer - Configuration (US2)

- **Phase 6**: T045-T047 (resources) then T048-T052 (service) || T053-T054 (UI)

- **Phase 7**: All 16 tasks (T060-T075) can be done in parallel- [ ] T056 [US4] Add resource keys to Strings.resx/ja.resx/en.resx for all forms: MainForm_Title, ServiceListForm_Title, SettingsForm_Title, NotificationForm_Title, buttons, labels, menus, error messages, notification text (FR-017)



### Independent Testing per Story- [ ] T057 [US4] Wire Program.cs Main() to load config.UiLanguage and call LocalizationService.SetLanguage() before showing any forms (FR-018: 次回起動時に読み込み)- [x] T061 [P] [US2] Create IConfigurationManager interface in Services/IConfigurationManager.cs



- **After Phase 3 (US1)**: Test service monitoring and notification independently- [x] T062 [US2] Implement ConfigurationManager class constructor in Services/ConfigurationManager.cs

- **After Phase 4 (US2)**: Test service selection UI independently (monitoring from US1 already works)

- **After Phase 5 (US3)**: Test config file management independently (service selection from US2 already works)**Checkpoint**: All user stories should now be independently functional - language switching works instantly across all UI elements without restart- [x] T063 [US2] Implement LoadAsync method with JSON deserialization in Services/ConfigurationManager.cs

- **After Phase 6 (US4)**: Test language switching independently (all prior features already work)

- [x] T064 [US2] Implement SaveAsync method with backup creation in Services/ConfigurationManager.cs

---

---- [x] T065 [US2] Implement CreateDefaultAsync method for first-run config in Services/ConfigurationManager.cs

## Format Validation

- [x] T066 [US2] Implement Validate method with ConfigurationValidator in Services/ConfigurationManager.cs

✅ **All 75 tasks follow required checklist format**:

- Checkbox: `- [ ]` ✓## Phase 7: Polish & Cross-Cutting Concerns- [x] T067 [US2] Create ConfigurationValidator class with all validation rules in Services/ConfigurationValidator.cs

- Task ID: T001-T075 (sequential, no gaps, no collisions) ✓

- [P] marker: 28 parallelizable tasks marked ✓- [x] T068 [US2] Implement TryLoadBackupAsync helper for corrupted config recovery

- [Story] label: 43 tasks in user story phases labeled [US1]-[US4] ✓

- Description: Clear action with file path ✓**Purpose**: Improvements that affect multiple user stories- [x] T069 [US2] Add ConfigurationChanged event in Services/ConfigurationManager.cs



**Example Format Compliance**:

- ✅ `- [ ] T006 [P] Create ServiceStatus enum in Models/ServiceStatus.cs...`

- ✅ `- [ ] T017 [US1] Implement ServiceMonitor class in Services/ServiceMonitor.cs...`- [ ] T058 [P] Add XML documentation comments to all public interfaces and classes per Microsoft conventions### Service Layer - Monitoring Extensions (US2)

- ✅ `- [ ] T045 [P] [US4] Create Strings.ja.resx in Resources/...`

- [ ] T059 [P] Add logging statements to all catch blocks and key decision points (service start/stop, config load/save, notification display, language switch)

---

- [ ] T060 [P] Implement IDisposable pattern for ServiceMonitor to properly dispose ServiceController instances and Timer- [x] T070 [US2] Implement AddServiceAsync method in Services/ServiceMonitor.cs

## Next Steps

- [ ] T061 [P] Add icon file ServiceWatcher.ico and set as application icon in project properties- [x] T071 [US2] Implement RemoveServiceAsync method in Services/ServiceMonitor.cs

### To Start Implementation

- [ ] T062 Add NotifyIcon to MainForm for system tray integration with context menu (Start Monitoring, Stop Monitoring, Settings, Exit)- [x] T072 [US2] Implement RefreshMonitoredServicesAsync method in Services/ServiceMonitor.cs

1. **Verify prerequisites**: Run `dotnet --version` (should be 8.0+)

2. **Begin Phase 1**: Execute tasks T001-T005 to setup project structure- [ ] T063 Implement MainForm minimize to tray behavior when ApplicationConfiguration.StartMinimized = true- [x] T073 [US2] Implement GetServiceStatusesAsync method in Services/ServiceMonitor.cs

3. **Build incrementally**: Complete each phase before moving to next

4. **Test after each story**: Run manual tests per "Independent Test" criteria- [ ] T064 Add error message localization for all exception messages using LocalizationService.GetString()



### MVP Delivery (Fastest Path to Value)- [ ] T065 Add performance counters: track average status check time, notification display time, language switch time to verify SC-002 (<1s), SC-008 (<1s)### UI Layer - Service List Form (US2)



**Scope**: Phase 1 → Phase 2 → Phase 3 (US1 only)- [ ] T066 Run all quickstart.md test scenarios manually (Scenarios 1-11) and document results

- **Tasks**: T001-T024 (24 tasks)

- **Deliverable**: Core monitoring with notifications- [ ] T067 Review code for constitution compliance: <50MB memory, <1% CPU with 20 services, 24-hour stability- [x] T074 [P] [US2] Create ServiceListForm class in UI/ServiceListForm.cs

- **Test**: Stop a service → notification appears

- **Value**: Immediate service monitoring capability- [ ] T068 Create default config.json template with sample services (wuauserv, Spooler) and comments- [x] T075 [US2] Design ServiceListForm UI: DataGridView for services, search box, add/remove buttons



### Suggested First Command- [x] T076 [US2] Implement LoadAllServices method using ServiceController.GetServices()



```bash**Checkpoint**: All features polished and ready for deployment- [x] T077 [US2] Implement search/filter functionality by service name

# Create solution and project structure (T001-T003)

dotnet new sln -n ServiceWatcher- [x] T078 [US2] Implement Add Service button handler with ConfigurationManager.SaveAsync

mkdir -p ServiceWatcher/{Models,Services,UI,Utils,Resources}

mkdir -p tests/{Unit/{Models,Services,Utils},Integration/Services}---- [x] T079 [US2] Implement Remove Service button handler with ConfigurationManager.SaveAsync

dotnet new winforms -n ServiceWatcher -o ServiceWatcher --framework net8.0

dotnet sln add ServiceWatcher/ServiceWatcher.csproj- [x] T080 [US2] Add monitored services list display (separate grid or highlight)

```

## Dependencies & Execution Order

---

### UI Layer - Main Window Integration (US2)

**Questions or Issues?** Refer to:

- [spec.md](./spec.md) - Functional requirements and acceptance criteria### Phase Dependencies

- [plan.md](./plan.md) - Technical context and architecture decisions

- [data-model.md](./data-model.md) - Entity definitions and validation rules- [x] T081 [US2] Add "Add Service" button to MainForm.cs

- [contracts/](./contracts/) - Interface specifications

- [quickstart.md](./quickstart.md) - Manual test scenarios- **Setup (Phase 1)**: No dependencies - can start immediately- [x] T082 [US2] Implement Add Service button click handler to open ServiceListForm


- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories- [x] T083 [US2] Add monitored services DataGridView to MainForm.cs

- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) - MVP core functionality- [x] T084 [US2] Implement RefreshServiceList method to update monitored services display

- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) AND User Story 1 (needs monitoring engine) - UI for service selection- [x] T085 [US2] Wire ConfigurationManager.ConfigurationChanged event to RefreshServiceList

- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2) - Configuration management (can be done in parallel with US1/US2 if resourced)

- **User Story 4 (Phase 6)**: Depends on Foundational (Phase 2) AND User Story 2 (needs UI forms created) - Localization layer### Unit Tests (US2)

- **Polish (Phase 7)**: Depends on all user stories being complete

- [x] T086 [P] [US2] Create ConfigurationManagerTests.cs in tests/Unit/

### User Story Dependencies- [x] T087 [P] [US2] Test LoadAsync with valid JSON file

- [x] T088 [P] [US2] Test LoadAsync with invalid JSON (should load backup)

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories - **THIS IS MVP**- [x] T089 [P] [US2] Test SaveAsync creates backup before saving

- **User Story 2 (P2)**: Needs User Story 1 complete (requires ServiceMonitor to be operational) - Adds service selection UI- [x] T090 [P] [US2] Test Validate with valid configuration (all checks pass)

- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Technically independent but integrates with all stories- [x] T091 [P] [US2] Test Validate with invalid configuration (interval out of range, duplicate services, etc.)

- **User Story 4 (P3)**: Needs User Story 2 complete (requires UI forms to exist for localization) - Adds i18n layer- [x] T092 [P] [US2] Test CreateDefaultAsync generates valid default config



### Within Each User Story### Integration Tests (US2)



- **US1**: ServiceMonitor & NotificationService implementation → NotificationForm UI → Event wiring → Error handling → Logging- [x] T093 [US2] Test full add service flow: UI ↁEConfigurationManager.SaveAsync ↁEfile written

- **US2**: MainForm & ServiceListForm UI → Service enumeration → Add/remove functionality → Grid binding → State management- [x] T094 [US2] Test configuration reload without restart (FR-010)

- **US3**: ConfigurationManager implementation → File I/O → Validation → SettingsForm UI → Program.cs integration → Dynamic reload- [x] T095 [US2] Test search performance with 100+ services (should complete in <3 seconds)

- **US4**: Resource files (.resx) → LocalizationService implementation → Language detection → SettingsForm language dropdown → Form localization → Program.cs integration

---

### Parallel Opportunities

## Phase 5: User Story 3 - Configuration File Management (P3)

- **Phase 1 (Setup)**: All tasks T001-T005 can run in parallel

- **Phase 2 (Foundational)**: Tasks T006-T010 (models) can run in parallel, T011-T016 (infrastructure) can run in parallel after models**Goal**: Persist settings to JSON file with backup/restore capability  

- **Phase 3 (US1)**: T017-T018 can run in parallel, then T019 (form), then sequential wiring/integration**Independent Test**: Edit config.json ↁErestart app ↁEsettings loaded correctly  

- **Phase 4 (US2)**: T025-T026 (forms) can run in parallel, then sequential feature implementation**Success Criteria**: SC-004 (configuration portability 100%)

- **Phase 5 (US3)**: T034-T035 can run in parallel, then sequential integration

- **Phase 6 (US4)**: T045-T047 (resource files) can run in parallel, then T048 (service), then sequential UI integration### Configuration File Handling (US3)

- **Phase 7 (Polish)**: T058-T061 can run in parallel

- [x] T096 [P] [US3] Implement ReloadAsync method in Services/ConfigurationManager.cs

---- [x] T097 [P] [US3] Implement RestoreFromBackupAsync method in Services/ConfigurationManager.cs

- [x] T098 [US3] Add file path resolution logic (%LOCALAPPDATA%\ServiceWatcher\config.json)

## Parallel Example: User Story 1- [x] T099 [US3] Implement ConfigurationExists check method in Services/ConfigurationManager.cs



```bash### Configuration Validation (US3)

# Launch models in parallel (after Foundational):

Task: "Implement ServiceMonitor class in ServiceWatcher/Services/ServiceMonitor.cs"- [x] T100 [P] [US3] Add validation for MonitoringIntervalSeconds (1-3600 range)

Task: "Implement NotificationService class in ServiceWatcher/Services/NotificationService.cs"- [x] T101 [P] [US3] Add validation for NotificationDisplayTimeSeconds (0-300 range)

- [x] T102 [P] [US3] Add validation for max services count (50 max per constitution)

# Then create form:- [x] T103 [P] [US3] Add validation for duplicate service names detection

Task: "Create NotificationForm in ServiceWatcher/UI/NotificationForm.cs"- [x] T104 [P] [US3] Add validation for service name max length (256 chars)



# Then wire together sequentially:### UI Layer - Settings (US3)

Task: "Implement ServiceMonitor.StartMonitoringAsync() with Timer-based polling loop"

Task: "Implement ServiceMonitor.CheckAllServicesAsync() with status checks"- [x] T105 [P] [US3] Add Settings menu item to MainForm.cs

Task: "Wire ServiceMonitor.ServiceStatusChanged event to NotificationService"- [x] T106 [US3] Create SettingsForm class in UI/SettingsForm.cs (optional - can edit config directly)

```- [x] T107 [US3] Add monitoring interval NumericUpDown control to SettingsForm

- [x] T108 [US3] Add notification display time NumericUpDown control to SettingsForm

---- [x] T109 [US3] Implement Save Settings button handler with validation

- [x] T110 [US3] Add StartMinimized checkbox to SettingsForm

## Implementation Strategy- [x] T111 [US3] Add AutoStartMonitoring checkbox to SettingsForm



### MVP First (User Story 1 Only)### Error Handling (US3)



1. Complete Phase 1: Setup (T001-T005)- [x] T112 [US3] Implement error handling for corrupted config.json (load backup or default)

2. Complete Phase 2: Foundational (T006-T016) - **CRITICAL GATE**- [x] T113 [US3] Implement error handling for read-only config file (show error dialog)

3. Complete Phase 3: User Story 1 (T017-T024)- [x] T114 [US3] Implement error handling for missing config directory (create directory)

4. **STOP and VALIDATE**: Run quickstart.md Scenario 3 (Start Monitoring & Detect Service Stop)- [x] T115 [US3] Add user-friendly error messages for all configuration errors

5. Deploy/demo if ready

### Unit Tests (US3)

**Value Delivered**: Core monitoring and notification functionality - immediately useful for single predefined service

- [x] T116 [P] [US3] Test ReloadAsync discards in-memory changes and reloads from file

### Incremental Delivery- [x] T117 [P] [US3] Test RestoreFromBackupAsync successfully restores from backup

- [x] T118 [P] [US3] Test error handling when both config and backup are corrupted (use default)

1. **Foundation Ready**: Complete Setup + Foundational → Can start user stories- [x] T119 [P] [US3] Test validation rules: interval out of range, max services exceeded, etc.

2. **MVP (US1)**: Add User Story 1 → Test independently (Scenario 3) → Deploy/Demo- [x] T120 [P] [US3] Test configuration file location (%LOCALAPPDATA%)

3. **Service Selection (US2)**: Add User Story 2 → Test independently (Scenario 2) → Deploy/Demo

4. **Configuration (US3)**: Add User Story 3 → Test independently (Scenarios 5, 6, 7) → Deploy/Demo### Integration Tests (US3)

5. **Localization (US4)**: Add User Story 4 → Test independently (Scenario 11) → Deploy/Demo

- [x] T121 [US3] Test manual config.json edit ↁEapp restart ↁEchanges loaded (FR-010 validation)

Each story adds value without breaking previous stories - can stop at any checkpoint.- [x] T122 [US3] Test config portability: copy config to different machine ↁEsame behavior

- [x] T123 [US3] Test first-run scenario: no config exists ↁEdefault created automatically (FR-009)

### Parallel Team Strategy

---

With multiple developers (after Foundational phase complete):

## Phase 6: Polish & Cross-Cutting Concerns

- **Developer A**: User Story 1 (T017-T024) - Core monitoring engine

- **Developer B**: User Story 3 (T034-T044) - Configuration management (can work in parallel with A)**Purpose**: Final touches, documentation, and deployment preparation

- **Once US1 + US3 complete**:

  - **Developer A**: User Story 2 (T025-T033) - Service selection UI (depends on US1)### Logging Implementation

  - **Developer B**: User Story 4 (T045-T057) - Localization (can start preparing .resx files early)

- [X] T124 [P] Log all service state changes (Running ↁEStopped) with timestamp

Stories complete and integrate independently.- [X] T125 [P] Log configuration load/save operations with success/failure status

- [X] T126 [P] Log monitoring start/stop events with service count

---- [X] T127 [P] Log all errors with full exception details and stack traces

- [X] T128 [P] Implement log file location at %LOCALAPPDATA%\ServiceWatcher\logs\

## Task Summary

### Performance Optimization

| Phase | Task Range | Count | Purpose |

|-------|------------|-------|---------|- [X] T129 [P] Measure memory usage with 50 services (must be <50MB per SC-003)

| Phase 1: Setup | T001-T005 | 5 | Project initialization |- [X] T130 [P] Measure CPU usage with 20 services (must be <1% per SC-003)

| Phase 2: Foundational | T006-T016 | 11 | Core models, interfaces, infrastructure |- [X] T131 [P] Optimize notification display time (must be <1 second per SC-002)

| Phase 3: User Story 1 (P1) | T017-T024 | 8 | Service monitoring & notification (MVP) |- [X] T132 [P] Test 24-hour continuous operation stability (SC-007)

| Phase 4: User Story 2 (P2) | T025-T033 | 9 | Service selection UI |

| Phase 5: User Story 3 (P3) | T034-T044 | 11 | Configuration file management |### UI Polish

| Phase 6: User Story 4 (P3) | T045-T057 | 13 | Internationalization (i18n) |

| Phase 7: Polish | T058-T068 | 11 | Cross-cutting concerns |- [X] T133 [P] Add application icon to MainForm and notification

| **TOTAL** | T001-T068 | **68** | |- [X] T134 [P] Implement proper application exit (stop monitoring, save config)

- [X] T135 [P] Add keyboard shortcuts (F5 for refresh, Ctrl+S for save config)

### Parallel Task Count by Phase- [X] T136 [P] Add status bar with monitoring status and service count

- [X] T137 [P] Implement window state persistence (remember size/position)

- Phase 1: 5 tasks (all can run in parallel)

- Phase 2: 11 tasks (5 models parallel, then 6 interfaces parallel)### Documentation

- Phase 3: 2 initial parallel tasks (ServiceMonitor + NotificationService)

- Phase 4: 2 initial parallel tasks (MainForm + ServiceListForm)- [X] T138 [P] Create README.md with installation instructions

- Phase 5: 2 initial parallel tasks (ConfigurationManager + SettingsForm)- [X] T139 [P] Add inline code comments for complex logic (monitoring loop, error handling)

- Phase 6: 3 initial parallel tasks (3 resource files)- [X] T140 [P] Create CHANGELOG.md documenting v1.0.0 features

- Phase 7: 4 parallel tasks (documentation, logging, IDisposable, icon)- [X] T141 [P] Add XML documentation comments to all public methods



### MVP Scope (Recommended Initial Release)### Deployment



**Minimum Viable Product**: Phases 1-3 only (T001-T024)- [X] T142 Create publish profile for self-contained .NET 8.0 deployment

- **Task Count**: 24 tasks- [X] T143 Test application on Windows 10, Windows 11, Windows Server 2016

- **Delivers**: Service monitoring with notifications for predefined services- [X] T144 Create zip package with executable and default config template

- **User Story**: US1 (P1 priority)- [X] T145 [P] Add version number to application (AssemblyInfo or project properties)

- **Validation**: quickstart.md Scenario 3

- **Constitution Compliance**: All 8 principles satisfied---



---## Task Summary



## Notes**Total Tasks**: 145  

**MVP Tasks (US1 only)**: T001-T058 (58 tasks)  

- [P] tasks = different files, no dependencies within same phase**Full Feature Tasks**: All 145 tasks

- [Story] label maps task to specific user story for traceability

- Each user story should be independently completable and testable### Task Count by Phase

- Commit after each task or logical group

- Stop at any checkpoint to validate story independently- Phase 1 (Setup): 10 tasks

- Tests are EXCLUDED per feature specification (no explicit test request)- Phase 2 (Foundational): 7 tasks

- Run quickstart.md scenarios manually for validation- Phase 3 (US1 - P1): 41 tasks

- Constitution requirements: <50MB memory, <1% CPU, <1s latency, 24-hour stability- Phase 4 (US2 - P2): 37 tasks

- Localization uses .NET standard .resx approach with ComponentResourceManager- Phase 5 (US3 - P3): 28 tasks

- Configuration stored in %LOCALAPPDATA%\ServiceWatcher\config.json- Phase 6 (Polish): 22 tasks


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
Phase 1 (Setup) ↁEPhase 2 (Foundational)
                      ↁE              Phase 3 (US1 - P1) ↁEMVP Milestone
                      ↁE              Phase 4 (US2 - P2)
                      ↁE              Phase 5 (US3 - P3)
                      ↁE              Phase 6 (Polish)
```

**Independent Stories**: US1, US2, US3 are designed to be independently testable. However, for best user experience, implement in priority order (P1 ↁEP2 ↁEP3).

### Task Dependencies Within Each Story

**US1 Dependencies**:
- T018-T020 (Models) ↁET021-T030 (ServiceMonitor) ↁET042-T048 (MainForm)
- T031-T036 (INotificationService) ↁET037-T041 (NotificationForm) ↁET045 (Event wiring)

**US2 Dependencies**:
- T059-T060 (Config model) ↁET061-T069 (ConfigurationManager) ↁET074-T080 (ServiceListForm)
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
- ✁EMonitor services and show popup notifications
- ✁EStart/Stop monitoring manually
- ✁ECore error handling (service not found, access denied)
- ✁EBasic logging
- ✁EUnit and integration tests for monitoring logic

**What's missing** (add with US2/US3):
- ❁EUI to add/remove services (must edit config.json manually)
- ❁EService list display and search
- ❁ESettings UI (must edit config.json manually)
- ❁EConfiguration validation UI

**MVP is production-ready** for users comfortable with manual config editing.

### Incremental Delivery

1. **Iteration 1** (MVP): Tasks T001-T058 ↁEDeploy as v0.1.0-alpha
2. **Iteration 2** (US2): Tasks T059-T095 ↁEDeploy as v0.2.0-beta
3. **Iteration 3** (US3): Tasks T096-T123 ↁEDeploy as v1.0.0-rc1
4. **Iteration 4** (Polish): Tasks T124-T145 ↁEDeploy as v1.0.0

### Constitution Compliance

All tasks align with the 7 constitution principles:
- ✁E**I. Windows-Native**: Uses ServiceController API
- ✁E**II. User Notification First**: US1 core focus
- ✁E**III. Minimal Resource**: Performance tasks T129-T132
- ✁E**IV. Configuration-Driven**: US3 entire focus
- ✁E**V. Testability**: 70 unit/integration tests planned
- ✁E**VI. Git Management**: Each task ↁEone commit
- ✁E**VII. Feature-Driven Design**: Organized by user story with clear class boundaries

---

## Next Steps

1. **Start with Phase 1** (Setup): Create project structure (T001-T010)
2. **Build Foundation** (Phase 2): Create utility classes (T011-T017)
3. **Implement MVP** (Phase 3): Complete US1 for first testable increment (T018-T058)
4. **Test MVP**: Run manual tests from `quickstart.md` scenarios 1, 3, 7, 8, 9, 10
5. **Commit**: After each task completion per constitution principle VI
6. **Iterate**: Add US2 and US3 incrementally

**Ready to implement**: All tasks have clear file paths and acceptance criteria. Begin with T001! 🚀








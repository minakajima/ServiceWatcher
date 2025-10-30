# Implementation Plan: Windowsサービス監視システム

**Branch**: `001-service-monitor` | **Date**: 2025-10-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-service-monitor/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Windowsサービスの状態を監視し、サービス停止時に即座にポップアップ通知を表示するデスクトップアプリケーション。ユーザーはシステムにインストールされているサービス一覧から監視対象を選択・登録でき、設定はJSON形式のファイルで管理される。憲章の要求に従い、Windows Native APIを使用し、軽量（メモリ50MB以下、CPU 1%未満）で、1秒以内の通知応答時間を実現する。

## Technical Context

**Language/Version**: C# 12 / .NET 8.0  
**Primary Dependencies**: System.ServiceProcess.ServiceController, Windows Forms, System.Text.Json  
**Storage**: JSON configuration file (file-based, no database)  
**Testing**: xUnit, Moq (for unit tests), manual integration testing for Windows API interactions  
**Target Platform**: Windows 10 (21H2+), Windows 11, Windows Server 2016+  
**Project Type**: single (desktop application)  
**Performance Goals**: <1 second notification latency, <1% CPU usage with 20 monitored services, <50MB memory  
**Constraints**: Must work without Administrator privileges (with graceful degradation), offline operation (no internet required), polling-based monitoring (5-second default interval)  
**Scale/Scope**: Monitor up to 50 services simultaneously, single-user desktop application

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Windows-Native Service Monitoring
✅ **PASS** - Design uses System.ServiceProcess.ServiceController for native Windows API access  
✅ **PASS** - Target platforms are Windows 10, 11, and Server 2016+  
✅ **PASS** - Service state detection designed for <1 second latency  
✅ **PASS** - Service restart scenarios handled in edge cases (FR-004, エッジケース)

### Principle II: User Notification First
✅ **PASS** - Immediate popup notification on service stop (FR-004, SC-002)  
✅ **PASS** - Notification includes service name, display name, timestamp, reason (FR-005)  
✅ **PASS** - Notification UI is dismissible and non-blocking (FR-011, ユーザーストーリー1)  
⚠️ **PARTIAL** - System tray notifications not explicitly in spec, but can be added as enhancement  
✅ **PASS** - Per-service notification configuration supported (設定情報エンティティ)

### Principle III: Minimal Resource Footprint
✅ **PASS** - Memory constraint: <50MB specified (SC-003)  
✅ **PASS** - CPU constraint: <1% with 20 services specified (SC-003)  
✅ **PASS** - Polling interval configurable (default 5 seconds) (FR-003)  
⚠️ **NOTE** - Event-driven monitoring via WMI not in initial scope, polling-based for simplicity  
✅ **PASS** - No interference with monitored services (read-only monitoring)

### Principle IV: Configuration-Driven
✅ **PASS** - Service list defined in JSON configuration file (FR-006)  
✅ **PASS** - Per-service settings supported (notification enabled/disabled) (設定情報エンティティ)  
✅ **PASS** - Configuration reload without restart (FR-010)  
✅ **PASS** - Configuration validation on load (ユーザーストーリー3, シナリオ4)  
✅ **PASS** - Default configuration template on first run (FR-009)

### Principle V: Testability and Reliability
✅ **PASS** - Unit tests required for service state detection  
✅ **PASS** - Integration tests required for Windows API interactions  
✅ **PASS** - Edge cases identified: permission errors, service not found, SCM unavailable (エッジケース)  
✅ **PASS** - Error logging required for troubleshooting (FR-012, 制約事項)  
✅ **PASS** - 24-hour continuous operation stability (SC-007)

### Technology Stack Requirements
✅ **PASS** - C# with .NET 8.0 (exceeds minimum .NET 6.0 requirement)  
✅ **PASS** - Windows Forms for UI  
✅ **PASS** - System.ServiceProcess.ServiceController for service interaction  
✅ **PASS** - System.Text.Json for configuration  
⚠️ **TODO** - Logging framework to be selected (Microsoft.Extensions.Logging or NLog)  
⚠️ **TODO** - Deployment packaging (MSI installer) not in initial MVP scope

### Overall Assessment
🟢 **CONSTITUTION COMPLIANT** - All core principles satisfied. Minor enhancements (system tray, WMI events, installer) can be addressed in future iterations.

## Project Structure

### Documentation (this feature)

```text
specs/001-service-monitor/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (internal API contracts, not REST)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ServiceWatcher/                      # Solution root
├── ServiceWatcher.sln               # Visual Studio solution file
├── ServiceWatcher.csproj            # Main project file
├── Program.cs                       # Application entry point
├── Models/                          # Data models
│   ├── MonitoredService.cs          # 監視対象サービス entity
│   ├── AppConfiguration.cs          # 設定情報 entity
│   └── NotificationEvent.cs         # 通知イベント entity
├── Services/                        # Business logic layer
│   ├── ServiceMonitor.cs            # Core monitoring logic
│   ├── ConfigurationManager.cs      # Configuration file management
│   └── NotificationService.cs       # Notification display logic
├── UI/                              # User interface layer
│   ├── MainForm.cs                  # Main application window
│   ├── MainForm.Designer.cs         # UI designer file
│   ├── ServiceListForm.cs           # Service selection UI
│   ├── NotificationForm.cs          # Popup notification window
│   └── Resources/                   # UI resources (icons, images)
├── Utils/                           # Utility classes
│   ├── Logger.cs                    # Logging utility
│   └── ServiceControllerExtensions.cs  # Helper methods
├── config.json                      # Default configuration template
└── README.md                        # Application documentation

tests/                               # Test project root
├── ServiceWatcher.Tests.csproj      # Test project file
├── Unit/                            # Unit tests
│   ├── ServiceMonitorTests.cs       # Monitor logic tests
│   ├── ConfigurationManagerTests.cs # Config management tests
│   └── NotificationServiceTests.cs  # Notification tests
└── Integration/                     # Integration tests
    └── WindowsServiceTests.cs       # Windows API integration tests

.specify/                            # Specification framework
.github/                             # GitHub workflows (future)
.gitignore                           # Git ignore patterns
```

**Structure Decision**: Single project structure selected. This is a standalone Windows desktop application with no separate frontend/backend or mobile components. The solution follows standard C# project organization with Models-Services-UI layering for clear separation of concerns. Tests are in a separate project following the same organizational pattern.

---

## Complexity Tracking

*No issues to track. All constitution principles are satisfied and no NEEDS CLARIFICATION markers exist in Technical Context.*

---

## Phase 0: Outline & Research

**Status**: ✅ COMPLETE

**Outputs**:
- ✅ `research.md` - Technical decisions and research findings

**Key Research Areas Resolved**:
1. **Windows Service Monitoring API**: Use System.ServiceProcess.ServiceController (native .NET API)
2. **UI Framework**: Windows Forms (lightweight, fast startup)
3. **Configuration Format**: JSON with System.Text.Json (no external dependencies)
4. **Monitoring Strategy**: Polling-based with 5-second interval (simple, reliable)
5. **Threading Model**: Task-based async/await for background monitoring
6. **Logging Framework**: Microsoft.Extensions.Logging (standard abstraction)
7. **Error Handling**: Try-catch around all Windows API calls with graceful degradation

**Research Document**: [research.md](./research.md)

---

## Phase 1: Design & Contracts

**Status**: ✅ COMPLETE

**Outputs**:
- ✅ `data-model.md` - Entity definitions, relationships, validation rules
- ✅ `contracts/` - Internal API contracts (IServiceMonitor, IConfigurationManager, INotificationService)
- ✅ `quickstart.md` - Manual testing scenarios

### Data Model Summary

**Entities**:
1. **MonitoredService** (監視対象サービス)
   - ServiceName, DisplayName, NotificationEnabled
   - LastKnownStatus, LastChecked, IsAvailable, ErrorMessage

2. **ApplicationConfiguration** (設定情報)
   - MonitoringIntervalSeconds, NotificationDisplayTimeSeconds
   - MonitoredServices (List), ConfigurationVersion, LastModified
   - StartMinimized, AutoStartMonitoring

3. **ServiceStatusChange** (通知イベント)
   - ServiceName, DisplayName, PreviousStatus, CurrentStatus
   - DetectedAt, NotificationShown, UserAcknowledged

**Validation Rules**:
- Monitoring interval: 1-3600 seconds
- Max monitored services: 50 (constitution requirement)
- No duplicate service names
- Service names max 256 characters

**Serialization**: JSON with camelCase naming, UTF-8 encoding

**File Location**: `%LOCALAPPDATA%\ServiceWatcher\config.json`

**Data Model Document**: [data-model.md](./data-model.md)

### Internal Contracts Summary

**Note**: These are in-process interfaces for a desktop application, not REST APIs.

1. **IServiceMonitor** - Core monitoring operations
   - StartMonitoringAsync, StopMonitoringAsync
   - AddServiceAsync, RemoveServiceAsync
   - Events: ServiceStatusChanged, MonitoringError

2. **IConfigurationManager** - Configuration persistence
   - LoadAsync, SaveAsync, ReloadAsync
   - Validate, RestoreFromBackupAsync
   - Event: ConfigurationChanged

3. **INotificationService** - User notifications
   - ShowNotification, ShowNotificationAsync
   - CloseAllNotifications, CloseNotification
   - Event: NotificationAcknowledged

**Dependency Injection**: Simple constructor injection (no DI container needed)

**Error Handling Pattern**: Result<T> type for explicit success/failure

**Contracts Directory**: [contracts/](./contracts/)

### Testing Strategy

**Manual Tests** (quickstart.md):
- 10 key scenarios including: first launch, add service, detect stop, multiple services, config persistence, performance test with 50 services

**Automated Tests**:
- Unit tests: xUnit + Moq for all service layer classes
- Integration tests: Real ServiceController API interactions
- Target: 80% coverage for Services, 60% for Models

**Quickstart Document**: [quickstart.md](./quickstart.md)

---

## Agent Context Update

**Status**: ⏳ PENDING

**Action Required**: Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`

**Purpose**: Update `.specify/memory/agent-context-copilot.md` with:
- Technology stack: C# 12, .NET 8.0, Windows Forms, System.ServiceProcess
- Architecture patterns: Models-Services-UI layering
- Key interfaces: IServiceMonitor, IConfigurationManager, INotificationService
- Performance constraints: <50MB memory, <1% CPU, <1s notification latency
- Testing approach: xUnit + Moq, manual integration tests

**After Update**: Agent will have full context for task generation and implementation guidance.

---

## Next Steps

**Phase 2: Task Breakdown**

Run the following command to generate tasks.md:

```powershell
# User must run this separately (not part of /speckit.plan)
/speckit.tasks
```

**Expected Output**: `tasks.md` with granular implementation tasks based on:
- Data model entities (Models/)
- Service layer implementations (Services/)
- UI components (UI/)
- Unit and integration tests (tests/)
- Configuration and deployment

**Implementation Readiness**: 🟢 **READY** - All planning artifacts complete, no blockers identified.

````

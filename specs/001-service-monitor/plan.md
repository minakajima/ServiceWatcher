# Implementation Plan: Windowsサービス監視システム (国際化対応含む)

**Branch**: `001-service-monitor` | **Date**: 2025-11-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-service-monitor/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Windowsサービスの状態をリアルタイムで監視し、サービス停止時に即座にポップアップ通知を表示するデスクトップアプリケーション。監視対象サービスはGUIから選択・登録可能で、設定はJSON形式で永続化。国際化対応により、日本語と英語のUIを設定画面から即時切り替え可能。軽量設計(CPU<1%, メモリ<50MB)で24時間連続稼働に対応。

## Technical Context

**Language/Version**: C# 12 / .NET 8.0  
**Primary Dependencies**: 
- System.ServiceProcess.ServiceController (Windows Service management)
- Windows Forms (UI framework)
- System.Text.Json (configuration serialization)
- System.Resources.ResourceManager (.resx for localization)

**Storage**: JSON configuration file (config.json) + .resx resource files for i18n  
**Testing**: xUnit + Moq (unit/integration tests), manual UI testing  
**Target Platform**: Windows 10 (21H2+), Windows 11, Windows Server 2016+  
**Project Type**: Single desktop application (Windows Forms)  
**Performance Goals**: 
- Service status check latency <1 second
- Notification display <1 second after service stop
- Language switch UI update <1 second
- CPU usage <1% (20 services monitored)

**Constraints**: 
- Memory footprint <50MB
- No internet connectivity required
- Administrator privileges optional (graceful degradation)
- Offline-capable (all features work without network)

**Scale/Scope**: 
- Up to 100 monitored services per configuration
- 2 supported languages (Japanese, English)
- 3-4 main UI screens
- 24-hour continuous operation stability

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance | Notes |
|-----------|------------|-------|
| **I. Windows-Native Service Monitoring** | ✅ PASS | Uses ServiceController API, targets Windows 10+, <1s latency |
| **II. User Notification First** | ✅ PASS | Immediate popup with service details, configurable per-service |
| **III. Minimal Resource Footprint** | ✅ PASS | <50MB memory, <1% CPU, configurable polling (default 5s) |
| **IV. Configuration-Driven** | ✅ PASS | JSON config, no restart required, validation on load, default template |
| **V. Testability and Reliability** | ✅ PASS | xUnit tests, permission/error handling, logging planned |
| **VI. Git Management** | ✅ PASS | Feature branch 001-service-monitor, incremental commits planned |
| **VII. Feature-Driven Class Design** | ✅ PASS | Organized by domain: Models/, Services/, UI/, Utils/ |
| **Localization** | ✅ PASS | Japanese spec.md, .resx for UI strings, config in Japanese/English |

**Overall**: ✅ ALL GATES PASSED

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ServiceWatcher/
├── Models/                          # Domain entities
│   ├── MonitoredService.cs
│   ├── ApplicationConfiguration.cs
│   ├── ServiceStatusChange.cs
│   └── ServiceStatus.cs (enum)
├── Services/                        # Business logic
│   ├── ServiceMonitor.cs           # Core monitoring engine
│   ├── NotificationService.cs      # Popup notification manager
│   └── ConfigurationManager.cs     # Config file I/O
├── UI/                              # Windows Forms
│   ├── MainForm.cs                 # Primary monitoring UI
│   ├── ServiceListForm.cs          # Service selection dialog
│   ├── SettingsForm.cs             # Settings (incl. language dropdown)
│   └── NotificationForm.cs         # Popup window
├── Utils/                           # Helpers
│   ├── Logger.cs
│   ├── ConfigurationValidator.cs
│   ├── ServiceControllerExtensions.cs
│   └── Result.cs (Result<T> pattern)
├── Resources/                       # Localization
│   ├── Strings.ja.resx             # Japanese UI strings
│   └── Strings.en.resx             # English UI strings
├── Program.cs                       # Entry point
└── config.json                      # Default configuration template

tests/
├── Unit/
│   ├── Models/
│   ├── Services/
│   └── Utils/
└── Integration/
    └── Services/
```

**Structure Decision**: Single desktop project structure. Feature-driven organization with clear separation: Models (data), Services (business logic), UI (presentation), Utils (cross-cutting). Localization resources in dedicated Resources/ folder using .NET's standard .resx approach.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations detected.** All constitution principles are satisfied by the current design.

---

## Implementation Phases

### Phase 0: Research & Clarification ✅ COMPLETED (2025-11-05)

**Objective**: Resolve all NEEDS CLARIFICATION items, document technology decisions, establish best practices.

**Deliverables**:
- ✅ `research.md` updated with i18n strategy (Section 7: Internationalization Strategy)
  - Decision: .NET Resource Files (.resx) + CultureInfo
  - Language detection: OS language auto-detection with fallback to English
  - Runtime switching: ComponentResourceManager.ApplyResources pattern
  - Performance: <1 second language switch (SC-008 compliance)

**Key Decisions**:
1. **Service Monitoring API**: System.ServiceProcess.ServiceController (polling-based)
2. **UI Framework**: Windows Forms (lightweight, fast startup)
3. **Configuration Format**: JSON with System.Text.Json
4. **Localization**: .resx files (Strings.ja.resx, Strings.en.resx) with ResourceManager
5. **Language Detection**: CultureInfo.CurrentUICulture (Japanese → "ja", else → "en")
6. **Error Handling**: Result<T> pattern + hierarchical exception handling

**Clarifications Resolved**:
- All technical unknowns from spec.md clarified
- i18n requirements added through `/speckit.clarify` workflow (US4, FR-013 to FR-018)
- No blocking issues remain

---

### Phase 1: Architecture & Design ✅ COMPLETED (2025-11-05)

**Objective**: Define data models, service contracts, project structure, developer onboarding.

**Deliverables**:
- ✅ `data-model.md` updated with `ApplicationConfiguration.UiLanguage` property
  - Added: `UiLanguage` ("ja" or "en") with OS detection default
  - Validation: Must be exactly "ja" or "en", invalid values fallback to "en"
  - C# implementation includes `DetectDefaultLanguage()` helper
  
- ✅ `contracts/ILocalizationService.md` created
  - Interface methods: DetectDefaultLanguage(), SetLanguage(), ApplyResourcesTo(), GetString(), GetFormattedString()
  - Usage examples for initialization, language switching, resource application
  - Unit test examples for language detection and validation
  - Performance requirements: <1s language switch, <10ms GetString()
  
- ✅ `contracts/README.md` updated
  - Added ILocalizationService to contract list
  
- ✅ `quickstart.md` updated with **Scenario 11: Language Switching (i18n)**
  - Test steps: Verify OS language detection, switch between ja/en, verify persistence, restart test
  - Expected results: <1s switch, all UI elements update, no restart required, config persists
  
- ✅ Agent context updated via `update-agent-context.ps1`
  - Added: JSON + .resx localization to `.github/copilot-instructions.md`
  - Added: C# 12 / .NET 8.0 to technology list

**Architecture Validation**:
- All entities, relationships, validation rules documented
- Service contracts defined with usage examples and test patterns
- Project structure finalized (Models/, Services/, UI/, Utils/, Resources/)
- No architecture complexity violations

---

### Phase 2: Task Decomposition ⏳ PENDING

**Objective**: Break down implementation into atomic, testable tasks.

**Command**: Execute `/speckit.tasks` to generate `tasks.md`

**Expected Workflow**:
1. AI reads spec.md, plan.md, data-model.md, contracts/
2. Generates task breakdown by architectural layer
3. Assigns task IDs, dependencies, acceptance criteria
4. Estimates effort (S/M/L complexity)
5. Outputs to `specs/001-service-monitor/tasks.md`

**Next Action**: User should invoke `/speckit.tasks` command

---

### Phase 3+: Implementation (Future)

**Not yet planned.** Will be defined after Phase 2 task decomposition.

**Expected workflow**:
- Phase 3: Core Models + Configuration (Tasks T001-T010)
- Phase 4: Service Monitoring Engine (Tasks T011-T020)
- Phase 5: UI Layer (Tasks T021-T030)
- Phase 6: Localization Implementation (Tasks T031-T035)
- Phase 7: Testing & Integration (Tasks T036-T040)
- Phase 8: Documentation & Release (Tasks T041-T045)

Use `/speckit.implement` command to execute tasks in dependency order.

---

## Completion Criteria

### Phase 0 ✅
- [x] All NEEDS CLARIFICATION resolved in research.md
- [x] Technology stack finalized and documented
- [x] i18n strategy documented with implementation pattern

### Phase 1 ✅
- [x] Data model includes UiLanguage field with validation
- [x] ILocalizationService contract defined with examples
- [x] Quickstart includes i18n testing scenario
- [x] Agent context updated with .resx + JSON technologies
- [x] All contracts documented with usage patterns

### Phase 2 ⏳
- [ ] tasks.md generated with task breakdown
- [ ] All tasks have IDs, descriptions, dependencies, acceptance criteria
- [ ] Tasks organized by architectural layer
- [ ] Effort estimates assigned (S/M/L)

### Phase 3+ ⏳
- [ ] To be defined after Phase 2 task decomposition

---

## Next Steps

1. **Execute `/speckit.tasks`** to generate task breakdown
2. Review and approve task decomposition
3. Execute `/speckit.implement` to begin implementation
4. Follow constitution principles during implementation
5. Run tests continuously (xUnit) + manual testing (quickstart.md scenarios)

---

## Change Log

| Date | Phase | Change |
|------|-------|--------|
| 2025-11-05 | 0 | Initial plan template created via setup-plan.ps1 |
| 2025-11-05 | 0 | Technical Context, Constitution Check, Project Structure filled |
| 2025-11-05 | 0 | Phase 0 completed: research.md updated with i18n strategy |
| 2025-11-05 | 1 | Phase 1 completed: data-model, contracts, quickstart updated |
| 2025-11-05 | 1 | Agent context updated with .resx localization technologies |

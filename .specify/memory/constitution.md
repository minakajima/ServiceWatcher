<!--
Sync Impact Report:
- Version change: 1.0.0 → 1.1.0 (Minor version bump)
- Modified principles: 
  * Governance section (Enhanced localization requirements)
  * Development Workflow (Added Git management requirements)
- Added sections: 
  * VI. Git Management and Version Control (New principle)
  * VII. Feature-Driven Class Design (New principle)
- Removed sections: N/A
- Templates requiring updates:
  ✅ plan-template.md (no changes needed - principles are additive)
  ✅ spec-template.md (already requires Japanese)
  ✅ tasks-template.md (compatible with new Git workflow)
- Follow-up TODOs: 
  * Ensure all developers understand incremental commit strategy
  * Update CI/CD workflows to validate commit granularity (future)
-->

# ServiceWatcher Constitution

## Core Principles

### I. Windows-Native Service Monitoring

ServiceWatcher MUST be designed specifically for Windows environments:
- Use native Windows APIs (ServiceController, System.ServiceProcess) for service monitoring
- MUST support Windows Service Control Manager (SCM) integration
- MUST run reliably on Windows 10, Windows 11, and Windows Server 2016+
- Service state detection MUST be real-time with minimal latency (<1 second)
- MUST handle service restart scenarios gracefully

**Rationale**: Windows service monitoring requires platform-specific APIs and behaviors that cannot be abstracted. Native integration ensures reliability and performance.

### II. User Notification First

When a monitored service stops, user notification is the primary responsibility:
- MUST display immediate popup/notification when service status changes to "Stopped"
- Notification MUST include: service name, display name, stop timestamp, and reason (if available)
- MUST support both foreground notifications (popup) and system tray notifications
- Users MUST be able to configure notification preferences per service
- Notification UI MUST be non-blocking and dismissible

**Rationale**: Immediate visibility of service failures enables rapid response. Users need clear, actionable information without manual polling.

### III. Minimal Resource Footprint

ServiceWatcher MUST be lightweight and efficient:
- Memory usage MUST NOT exceed 50MB under normal operation
- CPU usage MUST average <1% when monitoring up to 20 services
- Polling interval MUST be configurable (default: 5 seconds) with minimum 1 second
- MUST use event-driven monitoring where possible (WMI events, SCM notifications)
- MUST NOT interfere with monitored service performance

**Rationale**: Monitoring tools should not become a burden themselves. Efficiency ensures deployment on resource-constrained systems.

### IV. Configuration-Driven

All monitoring behavior MUST be configuration-driven:
- Service list MUST be defined in configuration file (JSON/XML)
- Each service entry MUST specify: service name, notification enabled/disabled, custom actions
- Configuration reload MUST NOT require application restart
- MUST validate configuration on load and provide clear error messages
- MUST provide default configuration template on first run

**Rationale**: Hardcoding service names limits flexibility. Configuration enables reuse across different environments.

### V. Testability and Reliability

ServiceWatcher MUST be thoroughly tested:
- Unit tests MUST cover service state detection logic
- Integration tests MUST verify Windows API interactions
- Manual test plan MUST exist for notification UI scenarios
- MUST handle edge cases: permission errors, service not found, SCM unavailable
- MUST log all errors with sufficient context for troubleshooting

**Rationale**: Service monitoring is critical infrastructure. Failures must be minimized through rigorous testing.

### VI. Git Management and Version Control

All code changes MUST follow incremental Git workflow:
- MUST commit to local Git repository after each completed task
- Commit messages MUST be clear and descriptive (use conventional commits format)
- Commit scope MUST be atomic: one logical change per commit
- MUST create separate branches for each feature or major change
- Branch names MUST follow pattern: `NNN-feature-name` (e.g., `001-service-monitor`)
- MUST NOT commit incomplete or broken functionality
- Configuration files and documentation MUST be versioned alongside code

**Rationale**: Incremental commits provide fine-grained history for debugging and rollback. Feature branches isolate changes and enable parallel development without conflicts.

### VII. Feature-Driven Class Design

Code organization MUST follow feature-oriented structure:
- Each feature MUST be implemented as cohesive set of classes
- Classes MUST be organized by functional domain (Models, Services, UI)
- MUST create separate classes for each distinct responsibility
- Class names MUST clearly indicate their purpose (e.g., `ServiceMonitor`, `NotificationService`)
- MUST avoid God objects: classes should have single, well-defined purpose
- Related classes MUST be grouped in same namespace/directory

**Rationale**: Feature-driven design improves code maintainability and testability. Clear class boundaries enable parallel development and reduce merge conflicts.

## Technology Stack Requirements

**Language**: C# (.NET 6.0 or later)
- Primary Framework: .NET Desktop Runtime (Windows Forms or WPF for UI)
- Service Interaction: System.ServiceProcess.ServiceController
- Configuration: System.Text.Json or System.Xml
- Logging: Microsoft.Extensions.Logging or NLog

**Deployment**:
- MUST provide standalone executable (.exe)
- SHOULD support Windows Service mode for background operation
- MUST include installer (MSI or setup.exe)
- MUST support both user-mode and elevated (Administrator) execution

**Dependencies**:
- Minimize external dependencies to reduce deployment complexity
- All dependencies MUST be included in deployment package
- MUST NOT require internet connectivity for core functionality

## Development Workflow

**Code Organization**:
- Separate concerns: Service monitoring logic, UI layer, configuration management
- Use dependency injection for testability
- Configuration models MUST be strongly-typed
- Each feature MUST be organized in dedicated namespace/directory structure
- Classes MUST be small and focused (prefer multiple small classes over large monoliths)
- MUST follow Single Responsibility Principle: one class, one purpose

**Error Handling**:
- All Windows API calls MUST be wrapped in try-catch with specific exception handling
- MUST distinguish between: access denied, service not found, SCM unreachable
- MUST provide user-friendly error messages in notifications
- Unhandled exceptions MUST be logged before application terminates

**Documentation**:
- README MUST include: installation steps, configuration format, troubleshooting guide
- Code comments MUST explain Windows-specific behavior (e.g., service state transitions)
- Configuration file MUST include inline comments/schema

## Governance

This constitution defines the non-negotiable principles for ServiceWatcher development. All feature specifications, implementation plans, and code reviews MUST verify compliance with these principles.

**Amendment Process**:
- Amendments require documentation of rationale and impact analysis
- Constitution version MUST be incremented per semantic versioning
- All dependent templates and documentation MUST be updated to reflect changes

**Compliance**:
- All pull requests MUST pass constitution check in implementation plan phase
- Deviations MUST be explicitly justified in Complexity Tracking section
- User stories and tasks MUST align with core principles

**Localization**:
- All specifications (spec.md) MUST be written in Japanese
- All user-facing documentation (README, quickstart guides) MUST be written in Japanese
- Code comments SHOULD use Japanese for business logic explanations
- UI strings and error messages MUST be in Japanese
- Constitution and implementation plans remain in English for technical consistency
- Technical API documentation (contracts, data models) remains in English

**Git Workflow**:
- Feature development MUST happen on dedicated feature branches
- Each completed task (from tasks.md) MUST result in a Git commit
- Commit messages MUST use format: `<type>(<scope>): <description>` (Japanese description allowed)
  - Types: feat, fix, docs, refactor, test, chore
  - Example: `feat(monitoring): サービス監視ロジックを実装`
- MUST push feature branch regularly to enable backup and collaboration
- Branch merges MUST use merge commits (preserve history)

**Version**: 1.1.0 | **Ratified**: 2025-10-30 | **Last Amended**: 2025-10-31

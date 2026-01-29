# ADR-001: Use CQRS Pattern for Application Layer

**Status**: Accepted  
**Date**: 2026-01-29  
**Context**: Need to separate read/write operations for flexibility and maintainability in a complex business intelligence application.  
**Decision**: Implement Command Query Responsibility Segregation (CQRS) using MediatR library.  
**Consequences**:
- **Positive**: Clearer separation of concerns, easier testing, better scalability for read-heavy operations
- **Positive**: Commands and queries can evolve independently
- **Positive**: Natural fit for domain-driven design principles
- **Negative**: More files per feature (command, query, handler, validator, DTOs)
- **Negative**: Slight learning curve for new developers

**Implementation**: 
- Commands located in `MIC.Core.Application/{Feature}/Commands/`
- Queries located in `MIC.Core.Application/{Feature}/Queries/`
- Handlers implement `ICommandHandler<TCommand, TResponse>` or `IQueryHandler<TQuery, TResponse>`
- Uses ErrorOr pattern for functional error handling
- MediatR pipeline handles cross-cutting concerns

**Example Structure**:
```
MIC.Core.Application/
├── Alerts/
│   ├── Commands/
│   │   ├── CreateAlert/
│   │   │   ├── CreateAlertCommand.cs
│   │   │   ├── CreateAlertCommandHandler.cs
│   │   │   └── CreateAlertCommandValidator.cs
│   │   └── ...
│   └── Queries/
│       ├── GetAllAlerts/
│       │   ├── GetAllAlertsQuery.cs
│       │   ├── GetAllAlertsQueryHandler.cs
│       │   └── AlertDto.cs
│       └── ...
└── ...
```

**Related Decisions**:
- ADR-002: Use ErrorOr Pattern for Functional Error Handling
- ADR-003: Use Clean Architecture with Dependency Inversion
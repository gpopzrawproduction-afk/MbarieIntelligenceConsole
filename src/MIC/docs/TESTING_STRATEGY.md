# Mbarie Intelligence Console - Testing Strategy

## Current Test Coverage Analysis

### Existing Unit Tests (MIC.Tests.Unit)
| Component | Tests | Status |
|-----------|-------|--------|
| `GetAllAlertsQueryHandler` | 2 tests | ? Implemented |
| `CreateAlertCommandHandler` | 3 tests | ? Implemented |
| `AuthenticationService` | 3+ tests | ? Implemented |

### Existing Integration Tests (MIC.Tests.Integration)
| Component | Tests | Status |
|-----------|-------|--------|
| `LoginIntegrationTests` | 1 test | ? Implemented (with TestContainers) |

### E2E Tests (MIC.Tests.E2E)
| Component | Tests | Status |
|-----------|-------|--------|
| None | 0 | ? Scaffold only |

---

## Priority Matrix - Components Requiring Tests

### ?? Critical Priority (Business Risk: High)

| Component | Type | Risk Level | Reason |
|-----------|------|------------|--------|
| `LoginCommandHandler` | Command | Critical | Authentication security |
| `RegisterUserCommandHandler` | Command | Critical | User account creation |
| `UpdateAlertCommandHandler` | Command | High | Data modification |
| `DeleteAlertCommandHandler` | Command | High | Data deletion |
| `GetAlertByIdQueryHandler` | Query | High | Single entity retrieval |

### ?? Medium Priority (Business Risk: Medium)

| Component | Type | Risk Level | Reason |
|-----------|------|------------|--------|
| `GetMetricsQueryHandler` | Query | Medium | Dashboard data |
| `GetMetricTrendQueryHandler` | Query | Medium | Analytics |
| `SaveChatInteractionCommandHandler` | Command | Medium | AI chat persistence |
| `ClearChatSessionCommandHandler` | Command | Medium | Session management |
| `GetChatHistoryQueryHandler` | Query | Medium | Chat retrieval |

### ?? Lower Priority (Business Risk: Lower)

| Component | Type | Risk Level | Reason |
|-----------|------|------------|--------|
| `GetEmailsQueryHandler` | Query | Lower | Email listing |
| `GetEmailByIdQueryHandler` | Query | Lower | Email retrieval |
| `SaveSettingsCommandHandler` | Command | Lower | Settings persistence |
| `GetSettingsQueryHandler` | Query | Lower | Settings retrieval |
| `UploadDocumentCommandHandler` | Command | Lower | Knowledge base |

---

## Test Implementation Guidelines

### Unit Test Naming Convention
```
[MethodName]_[Scenario]_[ExpectedResult]
```

Examples:
- `Handle_WithValidCommand_ReturnsAlertId`
- `Handle_WithNonexistentAlert_ReturnsNotFoundError`
- `Handle_WhenRepositoryThrows_ReturnsError`

### AAA Pattern (Arrange, Act, Assert)
```csharp
[Fact]
public async Task Handle_WithValidCommand_ReturnsSuccess()
{
    // Arrange
    var command = new UpdateAlertCommand(...);
    _repository.GetByIdAsync(...).Returns(existingAlert);

    // Act
    var result = await _sut.Handle(command, CancellationToken.None);

    // Assert
    result.IsError.Should().BeFalse();
    result.Value.Should().Be(expectedId);
}
```

### Mocking Strategy
- Use **NSubstitute** for all repository/service mocks
- Use **FluentAssertions** for readable assertions
- Mock only external dependencies (repositories, services)
- Never mock the System Under Test (SUT)

### Test Data Strategy
1. **Test Builders** - For complex entity creation
2. **Fixtures** - Shared setup for related tests
3. **Inline Data** - Simple, test-specific values

---

## Coverage Targets

| Layer | Target | Current Estimate |
|-------|--------|------------------|
| Application (Commands) | 80% | ~30% |
| Application (Queries) | 70% | ~20% |
| Application (Validators) | 90% | 0% |
| Domain (Entities) | 60% | 0% |
| Infrastructure (Services) | 50% | ~15% |

**Overall Target: 60% minimum before Code Freeze**

---

## CI/CD Pipeline Requirements

1. **On every PR:**
   - Run all unit tests
   - Run integration tests (with TestContainers)
   - Enforce minimum coverage threshold (60%)
   - Block merge if tests fail

2. **On merge to main:**
   - Run full test suite
   - Generate coverage report
   - Upload artifacts

3. **Nightly:**
   - Run E2E tests (when implemented)
   - Performance/load tests (future)

---

## Test Project Structure

```
MIC.Tests.Unit/
??? Features/
?   ??? Alerts/
?   ?   ??? CreateAlertCommandHandlerTests.cs ?
?   ?   ??? UpdateAlertCommandHandlerTests.cs ??
?   ?   ??? DeleteAlertCommandHandlerTests.cs ??
?   ?   ??? GetAllAlertsQueryHandlerTests.cs ?
?   ?   ??? GetAlertByIdQueryHandlerTests.cs ??
?   ??? Auth/
?   ?   ??? AuthenticationServiceTests.cs ?
?   ?   ??? LoginCommandHandlerTests.cs ??
?   ?   ??? RegisterUserCommandHandlerTests.cs ??
?   ??? Metrics/
?   ?   ??? GetMetricsQueryHandlerTests.cs ??
?   ?   ??? GetMetricTrendQueryHandlerTests.cs ??
?   ??? Chat/
?   ?   ??? SaveChatInteractionCommandHandlerTests.cs ??
?   ?   ??? GetChatHistoryQueryHandlerTests.cs ??
?   ??? Settings/
?       ??? SaveSettingsCommandHandlerTests.cs ??
??? Builders/
?   ??? AlertBuilder.cs ??
?   ??? UserBuilder.cs ??
?   ??? MetricBuilder.cs ??
??? Fixtures/
    ??? TestFixtures.cs ??

MIC.Tests.Integration/
??? Features/
?   ??? Auth/
?       ??? LoginIntegrationTests.cs ?
??? Database/
?   ??? AlertRepositoryTests.cs ??
??? Fixtures/
    ??? DatabaseFixture.cs ??
```

---

## Next Steps

1. ? Document created
2. ?? Implement critical priority unit tests
3. ? Create test builders and fixtures
4. ? Set up CI/CD pipeline
5. ? Achieve 60% coverage target

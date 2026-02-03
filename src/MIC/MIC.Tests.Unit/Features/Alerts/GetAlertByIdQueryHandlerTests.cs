using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Alerts.Queries.GetAlertById;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Alerts;

public class GetAlertByIdQueryHandlerTests
{
    private readonly GetAlertByIdQueryHandler _sut;
    private readonly IAlertRepository _alertRepository;

    public GetAlertByIdQueryHandlerTests()
    {
        _alertRepository = Substitute.For<IAlertRepository>();
        _sut = new GetAlertByIdQueryHandler(_alertRepository);
    }

    [Fact]
    public async Task Handle_WithExistingAlert_ReturnsAlertDto()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Test Alert", AlertSeverity.Warning, AlertStatus.Active);
        var query = new GetAlertByIdQuery(alertId);

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(alertId);
        result.Value.AlertName.Should().Be("Test Alert");
        result.Value.Severity.Should().Be(AlertSeverity.Warning);
        result.Value.Status.Should().Be(AlertStatus.Active);
    }

    [Fact]
    public async Task Handle_WithNonexistentAlert_ReturnsNotFoundError()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var query = new GetAlertByIdQuery(alertId);

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IntelligenceAlert?>(null));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Alert.NotFound");
        result.FirstError.Description.Should().Contain(alertId.ToString());
    }

    [Fact]
    public async Task Handle_WithExistingAlert_MapsAllPropertiesToDto()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Detailed Alert", AlertSeverity.Critical, AlertStatus.Acknowledged);
        var query = new GetAlertByIdQuery(alertId);

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var dto = result.Value;
        
        dto.Id.Should().Be(alertId);
        dto.AlertName.Should().Be("Detailed Alert");
        dto.Description.Should().Be("Description for Detailed Alert");
        dto.Severity.Should().Be(AlertSeverity.Critical);
        dto.Status.Should().Be(AlertStatus.Acknowledged);
        dto.Source.Should().Be("Test Source");
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var query = new GetAlertByIdQuery(alertId);
        var alert = CreateTestAlert(alertId, "Test", AlertSeverity.Info, AlertStatus.Active);

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _alertRepository.Received(1).GetByIdAsync(
            Arg.Is<Guid>(id => id == alertId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var query = new GetAlertByIdQuery(alertId);
        var alert = CreateTestAlert(alertId, "Test", AlertSeverity.Info, AlertStatus.Active);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);

        // Act
        await _sut.Handle(query, cancellationToken);

        // Assert
        await _alertRepository.Received(1).GetByIdAsync(
            Arg.Any<Guid>(),
            Arg.Is<CancellationToken>(ct => ct == cancellationToken));
    }

    private static IntelligenceAlert CreateTestAlert(
        Guid id,
        string name,
        AlertSeverity severity,
        AlertStatus status)
    {
        var alert = new IntelligenceAlert(name, $"Description for {name}", severity, "Test Source");
        
        // Set Id via reflection
        var idProperty = typeof(IntelligenceAlert).GetProperty("Id");
        idProperty?.SetValue(alert, id);
        
        // Set status if not Active (default)
        if (status == AlertStatus.Acknowledged)
        {
            alert.Acknowledge("test-user");
        }
        else if (status == AlertStatus.Resolved)
        {
            alert.Resolve("test-user", "Test resolution");
        }
        
        return alert;
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Alerts.Commands.UpdateAlert;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Alerts;

public class UpdateAlertCommandHandlerTests
{
    private readonly UpdateAlertCommandHandler _sut;
    private readonly IAlertRepository _alertRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAlertCommandHandlerTests()
    {
        _alertRepository = Substitute.For<IAlertRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new UpdateAlertCommandHandler(_alertRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithNonexistentAlert_ReturnsNotFoundError()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var command = new UpdateAlertCommand
        {
            AlertId = alertId,
            NewStatus = AlertStatus.Acknowledged,
            UpdatedBy = "test-user"
        };

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IntelligenceAlert?>(null));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Alert.NotFound");
    }

    [Fact]
    public async Task Handle_WithDeletedAlert_ReturnsConflictError()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Test Alert", AlertSeverity.Warning);
        alert.MarkAsDeleted("admin"); // Mark as deleted

        var command = new UpdateAlertCommand
        {
            AlertId = alertId,
            NewStatus = AlertStatus.Acknowledged,
            UpdatedBy = "test-user"
        };

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        result.FirstError.Code.Should().Be("Alert.Deleted");
    }

    [Fact]
    public async Task Handle_WithMetadataUpdateButNoUser_ReturnsValidationError()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Test Alert", AlertSeverity.Warning);

        var command = new UpdateAlertCommand
        {
            AlertId = alertId,
            AlertName = "Updated Alert Name",
            UpdatedBy = "" // Empty user
        };

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Alert.UpdateRequiresUser");
    }

    [Fact]
    public async Task Handle_WithValidStatusUpdate_ReturnsUpdatedAlert()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Test Alert", AlertSeverity.Warning);

        var command = new UpdateAlertCommand
        {
            AlertId = alertId,
            NewStatus = AlertStatus.Acknowledged,
            UpdatedBy = "test-user"
        };

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(alertId);
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMetadataUpdate_UpdatesAlertProperties()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Original Name", AlertSeverity.Info);

        var command = new UpdateAlertCommand
        {
            AlertId = alertId,
            AlertName = "Updated Name",
            Description = "Updated Description",
            Severity = AlertSeverity.Critical,
            Source = "Updated Source",
            UpdatedBy = "test-user"
        };

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        alert.AlertName.Should().Be("Updated Name");
        alert.Description.Should().Be("Updated Description");
        alert.Severity.Should().Be(AlertSeverity.Critical);
        alert.Source.Should().Be("Updated Source");
    }

    [Fact]
    public async Task Handle_WithNotes_AddsNotesToContext()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Test Alert", AlertSeverity.Warning);

        var command = new UpdateAlertCommand
        {
            AlertId = alertId,
            Notes = "This is a test note",
            UpdatedBy = "test-user"
        };

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        alert.Context.Should().ContainKey("Notes");
        alert.Context["Notes"].Should().NotBeNull();
    }

    private static IntelligenceAlert CreateTestAlert(
        Guid id,
        string name,
        AlertSeverity severity)
    {
        var alert = new IntelligenceAlert(name, $"Description for {name}", severity, "Test Source");
        // Use reflection to set the Id since it's typically set by EF Core
        var idProperty = typeof(IntelligenceAlert).GetProperty("Id");
        idProperty?.SetValue(alert, id);
        return alert;
    }
}

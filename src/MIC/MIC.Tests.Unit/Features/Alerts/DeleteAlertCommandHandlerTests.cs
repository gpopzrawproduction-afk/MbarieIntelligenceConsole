using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Alerts.Commands.DeleteAlert;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Alerts;

public class DeleteAlertCommandHandlerTests
{
    private readonly DeleteAlertCommandHandler _sut;
    private readonly IAlertRepository _alertRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAlertCommandHandlerTests()
    {
        _alertRepository = Substitute.For<IAlertRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new DeleteAlertCommandHandler(_alertRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithNonexistentAlert_ReturnsNotFoundError()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var command = new DeleteAlertCommand(alertId, "admin");

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IntelligenceAlert?>(null));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Alert.NotFound");
        result.FirstError.Description.Should().Contain(alertId.ToString());
    }

    [Fact]
    public async Task Handle_WithAlreadyDeletedAlert_ReturnsConflictError()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Deleted Alert", AlertSeverity.Info);
        alert.MarkAsDeleted("previous-admin"); // Already deleted

        var command = new DeleteAlertCommand(alertId, "admin");

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        result.FirstError.Code.Should().Be("Alert.AlreadyDeleted");
    }

    [Fact]
    public async Task Handle_WithValidAlert_SoftDeletesAndReturnsTrue()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Active Alert", AlertSeverity.Warning);
        var deletedBy = "admin-user";

        var command = new DeleteAlertCommand(alertId, deletedBy);

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        alert.IsDeleted.Should().BeTrue();
        
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidAlert_MarksAlertWithCorrectDeletedBy()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Test Alert", AlertSeverity.Critical);
        var deletedBy = "test-admin";

        var command = new DeleteAlertCommand(alertId, deletedBy);

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        alert.IsDeleted.Should().BeTrue();
        alert.LastModifiedBy.Should().Be(deletedBy);
        alert.ModifiedAt.Should().NotBeNull();
        alert.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_DoesNotCallSaveChanges_WhenAlertNotFound()
    {
        // Arrange
        var command = new DeleteAlertCommand(Guid.NewGuid(), "admin");

        _alertRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IntelligenceAlert?>(null));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotCallSaveChanges_WhenAlertAlreadyDeleted()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = CreateTestAlert(alertId, "Already Deleted", AlertSeverity.Info);
        alert.MarkAsDeleted("previous-user");

        var command = new DeleteAlertCommand(alertId, "admin");

        _alertRepository.GetByIdAsync(alertId, Arg.Any<CancellationToken>())
            .Returns(alert);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static IntelligenceAlert CreateTestAlert(
        Guid id,
        string name,
        AlertSeverity severity)
    {
        var alert = new IntelligenceAlert(name, $"Description for {name}", severity, "Test Source");
        var idProperty = typeof(IntelligenceAlert).GetProperty("Id");
        idProperty?.SetValue(alert, id);
        return alert;
    }
}

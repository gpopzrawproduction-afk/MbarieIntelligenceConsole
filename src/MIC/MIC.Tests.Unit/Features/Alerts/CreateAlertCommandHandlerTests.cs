using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Alerts.Commands.CreateAlert;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Alerts;

public class CreateAlertCommandHandlerTests
{
    private readonly CreateAlertCommandHandler _sut;
    private readonly IAlertRepository _alertRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAlertCommandHandlerTests()
    {
        _alertRepository = Substitute.For<IAlertRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new CreateAlertCommandHandler(_alertRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsAlertId()
    {
        // Arrange
        var command = new CreateAlertCommand(
            "Test Alert",
            "This is a test alert",
            AlertSeverity.Warning,
            "Test System");
        
        var alertId = Guid.NewGuid();
        _ = _alertRepository.AddAsync(Arg.Any<IntelligenceAlert>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Mock the alert that would be created
        var capturedAlert = new IntelligenceAlert("Placeholder", "Placeholder", AlertSeverity.Info, "Placeholder");
        _ = _alertRepository.AddAsync(Arg.Do<IntelligenceAlert>(a => capturedAlert = a), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(capturedAlert.Id);
        
        await _alertRepository.Received(1)
            .AddAsync(Arg.Any<IntelligenceAlert>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesAlertWithCorrectProperties()
    {
        // Arrange
        var command = new CreateAlertCommand(
            "Test Alert",
            "This is a test alert",
            AlertSeverity.Critical,
            "Monitoring System");
        
        var capturedAlert = new IntelligenceAlert("Placeholder", "Placeholder", AlertSeverity.Info, "Placeholder");
        _ = _alertRepository.AddAsync(Arg.Do<IntelligenceAlert>(a => capturedAlert = a), Arg.Any<CancellationToken>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedAlert.AlertName.Should().Be("Test Alert");
        capturedAlert.Description.Should().Be("This is a test alert");
        capturedAlert.Severity.Should().Be(AlertSeverity.Critical);
        capturedAlert.Source.Should().Be("Monitoring System");
        capturedAlert.Status.Should().Be(AlertStatus.Active);
        capturedAlert.TriggeredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ReturnsError()
    {
        // Arrange
        var command = new CreateAlertCommand(
            "Test Alert",
            "Description",
            AlertSeverity.Info,
            "Source");
        
        _alertRepository.AddAsync(Arg.Any<IntelligenceAlert>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Database error")));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Description.Contains("Database error"));
    }

    [Fact]
    public async Task Handle_WhenUnitOfWorkFails_ReturnsError()
    {
        // Arrange
        var command = new CreateAlertCommand(
            "Test Alert",
            "Description",
            AlertSeverity.Info,
            "Source");
        
        _alertRepository.AddAsync(Arg.Any<IntelligenceAlert>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new Exception("Save failed")));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Description.Contains("Save failed"));
    }

    [Fact]
    public async Task Handle_WhenSaveChangesReturnsZero_StillReturnsAlertId()
    {
        // Arrange
        var command = new CreateAlertCommand(
            "Test Alert",
            "Description",
            AlertSeverity.Info,
            "Source");
        
        var capturedAlert = new IntelligenceAlert("Placeholder", "Placeholder", AlertSeverity.Info, "Placeholder");
        _ = _alertRepository.AddAsync(Arg.Do<IntelligenceAlert>(a => capturedAlert = a), Arg.Any<CancellationToken>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(capturedAlert.Id);
    }
}
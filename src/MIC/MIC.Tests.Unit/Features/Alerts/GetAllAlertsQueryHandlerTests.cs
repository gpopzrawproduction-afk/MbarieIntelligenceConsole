using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Alerts.Queries.GetAllAlerts;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Alerts;

public class GetAllAlertsQueryHandlerTests
{
    private readonly GetAllAlertsQueryHandler _sut;
    private readonly IAlertRepository _alertRepository;

    public GetAllAlertsQueryHandlerTests()
    {
        _alertRepository = Substitute.For<IAlertRepository>();
        _sut = new GetAllAlertsQueryHandler(_alertRepository);
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllAlertsAsDtos()
    {
        // Arrange
        var alerts = new List<IntelligenceAlert>
        {
            CreateTestAlert(Guid.NewGuid(), "Alert 1", AlertSeverity.Info),
            CreateTestAlert(Guid.NewGuid(), "Alert 2", AlertSeverity.Warning),
            CreateTestAlert(Guid.NewGuid(), "Alert 3", AlertSeverity.Critical)
        };
        
        var query = new GetAllAlertsQuery();
        
        _alertRepository.GetFilteredAlertsAsync(
            severity: null,
            status: null,
            startDate: null,
            endDate: null,
            searchText: null,
            take: 100,
            skip: null,
            includeDeleted: false,
            Arg.Any<CancellationToken>())
            .Returns(alerts.ToList());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        result.Value.Select(dto => dto.AlertName).Should().Contain(new[] { "Alert 1", "Alert 2", "Alert 3" });
        
        await _alertRepository.Received(1).GetFilteredAlertsAsync(
            Arg.Is<AlertSeverity?>(s => s == null),
            Arg.Is<AlertStatus?>(s => s == null),
            Arg.Is<DateTime?>(d => d == null),
            Arg.Is<DateTime?>(d => d == null),
            Arg.Is<string?>(s => s == null),
            Arg.Is<int?>(t => t == 100),
            Arg.Is<int?>(s => s == null),
            Arg.Is<bool>(b => b == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSeverityFilter_ReturnsFilteredAlerts()
    {
        // Arrange
        var alerts = new List<IntelligenceAlert>
        {
            CreateTestAlert(Guid.NewGuid(), "Critical Alert", AlertSeverity.Critical),
            CreateTestAlert(Guid.NewGuid(), "Another Critical", AlertSeverity.Critical)
        };
        
        var query = new GetAllAlertsQuery { Severity = AlertSeverity.Critical };
        
        _alertRepository.GetFilteredAlertsAsync(
            severity: AlertSeverity.Critical,
            status: null,
            startDate: null,
            endDate: null,
            searchText: null,
            take: 100,
            skip: null,
            includeDeleted: false,
            Arg.Any<CancellationToken>())
            .Returns(alerts.ToList());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
        result.Value.All(dto => dto.Severity == AlertSeverity.Critical).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsFilteredAlerts()
    {
        // Arrange
        var alert1 = CreateTestAlert(Guid.NewGuid(), "Active Alert", AlertSeverity.Info);
        var alert2 = CreateTestAlert(Guid.NewGuid(), "Resolved Alert", AlertSeverity.Warning);
        alert2.Resolve("testuser", "Fixed");
        
        var alerts = new List<IntelligenceAlert> { alert2 };
        
        var query = new GetAllAlertsQuery { Status = AlertStatus.Resolved };
        
        _alertRepository.GetFilteredAlertsAsync(
            severity: null,
            status: AlertStatus.Resolved,
            startDate: null,
            endDate: null,
            searchText: null,
            take: 100,
            skip: null,
            includeDeleted: false,
            Arg.Any<CancellationToken>())
            .Returns(alerts.ToList());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value.First().Status.Should().Be(AlertStatus.Resolved);
    }

    [Fact]
    public async Task Handle_WithDateRangeFilter_ReturnsFilteredAlerts()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        
        var alerts = new List<IntelligenceAlert>
        {
            CreateTestAlert(Guid.NewGuid(), "Alert in Range", AlertSeverity.Info)
        };
        
        var query = new GetAllAlertsQuery { StartDate = startDate, EndDate = endDate };
        
        _alertRepository.GetFilteredAlertsAsync(
            severity: null,
            status: null,
            startDate: startDate,
            endDate: endDate,
            searchText: null,
            take: 100,
            skip: null,
            includeDeleted: false,
            Arg.Any<CancellationToken>())
            .Returns(alerts.ToList());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        
        await _alertRepository.Received(1).GetFilteredAlertsAsync(
            Arg.Is<AlertSeverity?>(s => s == null),
            Arg.Is<AlertStatus?>(s => s == null),
            Arg.Is<DateTime?>(d => d == startDate),
            Arg.Is<DateTime?>(d => d == endDate),
            Arg.Is<string?>(s => s == null),
            Arg.Is<int?>(t => t == 100),
            Arg.Is<int?>(s => s == null),
            Arg.Is<bool>(b => b == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSearchText_ReturnsFilteredAlerts()
    {
        // Arrange
        var searchText = "important";
        var alerts = new List<IntelligenceAlert>
        {
            CreateTestAlert(Guid.NewGuid(), "Important Alert", AlertSeverity.Critical)
        };
        
        var query = new GetAllAlertsQuery { SearchText = searchText };
        
        _alertRepository.GetFilteredAlertsAsync(
            severity: null,
            status: null,
            startDate: null,
            endDate: null,
            searchText: searchText,
            take: 100,
            skip: null,
            includeDeleted: false,
            Arg.Any<CancellationToken>())
            .Returns(alerts.ToList());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        
        await _alertRepository.Received(1).GetFilteredAlertsAsync(
            Arg.Is<AlertSeverity?>(s => s == null),
            Arg.Is<AlertStatus?>(s => s == null),
            Arg.Is<DateTime?>(d => d == null),
            Arg.Is<DateTime?>(d => d == null),
            Arg.Is<string?>(s => s == searchText),
            Arg.Is<int?>(t => t == 100),
            Arg.Is<int?>(s => s == null),
            Arg.Is<bool>(b => b == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var take = 10;
        var skip = 20;
        
        var alerts = new List<IntelligenceAlert>
        {
            CreateTestAlert(Guid.NewGuid(), "Alert 1", AlertSeverity.Info)
        };
        
        var query = new GetAllAlertsQuery { Take = take, Skip = skip };
        
        _alertRepository.GetFilteredAlertsAsync(
            severity: null,
            status: null,
            startDate: null,
            endDate: null,
            searchText: null,
            take: take,
            skip: skip,
            includeDeleted: false,
            Arg.Any<CancellationToken>())
            .Returns(alerts.ToList());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        
        await _alertRepository.Received(1).GetFilteredAlertsAsync(
            Arg.Is<AlertSeverity?>(s => s == null),
            Arg.Is<AlertStatus?>(s => s == null),
            Arg.Is<DateTime?>(d => d == null),
            Arg.Is<DateTime?>(d => d == null),
            Arg.Is<string?>(s => s == null),
            Arg.Is<int?>(t => t == take),
            Arg.Is<int?>(s => s == skip),
            Arg.Is<bool>(b => b == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ReturnsError()
    {
        // Arrange
        var query = new GetAllAlertsQuery();
        
        _alertRepository.GetFilteredAlertsAsync(
            Arg.Any<AlertSeverity?>(),
            Arg.Any<AlertStatus?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<string?>(),
            Arg.Any<int?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<IntelligenceAlert>>(new Exception("Database error")));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Description.Contains("Database error"));
    }

    [Fact]
    public async Task Handle_WhenNoAlertsFound_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAllAlertsQuery();
        
        _alertRepository.GetFilteredAlertsAsync(
            Arg.Any<AlertSeverity?>(),
            Arg.Any<AlertStatus?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<string?>(),
            Arg.Any<int?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<IntelligenceAlert>());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithIncludeDeletedFlag_ReturnsDeletedAlerts()
    {
        // Arrange
        var query = new GetAllAlertsQuery { IncludeDeleted = true };
        
        var alerts = new List<IntelligenceAlert>
        {
            CreateTestAlert(Guid.NewGuid(), "Deleted Alert", AlertSeverity.Info)
        };
        
        _alertRepository.GetFilteredAlertsAsync(
            severity: null,
            status: null,
            startDate: null,
            endDate: null,
            searchText: null,
            take: 100,
            skip: null,
            includeDeleted: true,
            Arg.Any<CancellationToken>())
            .Returns(alerts.ToList());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        
        await _alertRepository.Received(1).GetFilteredAlertsAsync(
            Arg.Any<AlertSeverity?>(),
            Arg.Any<AlertStatus?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<string?>(),
            Arg.Any<int?>(),
            Arg.Any<int?>(),
            Arg.Is<bool>(b => b == true),
            Arg.Any<CancellationToken>());
    }

    private static IntelligenceAlert CreateTestAlert(Guid id, string name, AlertSeverity severity)
    {
        var alert = new IntelligenceAlert(name, "Test description", severity, "Test System");
        // Set private Id via reflection or constructor? BaseEntity likely has protected set
        // For simplicity, we'll just use the alert as is
        return alert;
    }
}
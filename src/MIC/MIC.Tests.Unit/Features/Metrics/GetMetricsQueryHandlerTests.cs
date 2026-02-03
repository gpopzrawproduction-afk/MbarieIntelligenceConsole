using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Metrics.Queries.GetMetrics;
using MIC.Core.Domain.Entities;
using NSubstitute;
using Xunit;

namespace MIC.Tests.Unit.Features.Metrics;

public class GetMetricsQueryHandlerTests
{
    private readonly GetMetricsQueryHandler _sut;
    private readonly IMetricsRepository _metricsRepository;

    public GetMetricsQueryHandlerTests()
    {
        _metricsRepository = Substitute.For<IMetricsRepository>();
        _sut = new GetMetricsQueryHandler(_metricsRepository);
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllMetrics()
    {
        // Arrange
        var metrics = new List<OperationalMetric>
        {
            CreateTestMetric("CPU Usage", "Performance", 75.5),
            CreateTestMetric("Memory Usage", "Performance", 60.2),
            CreateTestMetric("Disk Space", "Storage", 45.0)
        };

        var query = new GetMetricsQuery();

        _metricsRepository.GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(Task.FromResult<IReadOnlyList<OperationalMetric>>(metrics));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Verify repository was called
        await _metricsRepository.Received(1).GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_PassesFilterToRepository()
    {
        // Arrange
        var metrics = new List<OperationalMetric>
        {
            CreateTestMetric("CPU Usage", "Performance", 75.5),
            CreateTestMetric("Memory Usage", "Performance", 60.2)
        };

        var query = new GetMetricsQuery { Category = "Performance" };

        _metricsRepository.GetFilteredMetricsAsync(
            category: "Performance",
            metricName: Arg.Any<string?>(),
            startDate: Arg.Any<DateTime?>(),
            endDate: Arg.Any<DateTime?>(),
            take: Arg.Any<int?>(),
            latestOnly: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OperationalMetric>>(metrics));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(m => m.Category == "Performance");
    }

    [Fact]
    public async Task Handle_WithDateRangeFilter_PassesDatesToRepository()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var metrics = new List<OperationalMetric>
        {
            CreateTestMetric("CPU Usage", "Performance", 75.5)
        };

        var query = new GetMetricsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        _metricsRepository.GetFilteredMetricsAsync(
            category: Arg.Any<string?>(),
            metricName: Arg.Any<string?>(),
            startDate: startDate,
            endDate: endDate,
            take: Arg.Any<int?>(),
            latestOnly: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OperationalMetric>>(metrics));

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _metricsRepository.Received(1).GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Is<DateTime?>(d => d == startDate),
            Arg.Is<DateTime?>(d => d == endDate),
            Arg.Any<int?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithLatestOnlyFlag_PassesFlagToRepository()
    {
        // Arrange
        var metrics = new List<OperationalMetric>
        {
            CreateTestMetric("CPU Usage", "Performance", 75.5)
        };

        var query = new GetMetricsQuery { LatestOnly = true };

        _metricsRepository.GetFilteredMetricsAsync(
            category: Arg.Any<string?>(),
            metricName: Arg.Any<string?>(),
            startDate: Arg.Any<DateTime?>(),
            endDate: Arg.Any<DateTime?>(),
            take: Arg.Any<int?>(),
            latestOnly: true,
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OperationalMetric>>(metrics));

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _metricsRepository.Received(1).GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int?>(),
            Arg.Is<bool>(l => l == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTakeLimit_PassesLimitToRepository()
    {
        // Arrange
        var metrics = new List<OperationalMetric>
        {
            CreateTestMetric("CPU Usage", "Performance", 75.5)
        };

        var query = new GetMetricsQuery { Take = 10 };

        _metricsRepository.GetFilteredMetricsAsync(
            category: Arg.Any<string?>(),
            metricName: Arg.Any<string?>(),
            startDate: Arg.Any<DateTime?>(),
            endDate: Arg.Any<DateTime?>(),
            take: 10,
            latestOnly: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OperationalMetric>>(metrics));

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _metricsRepository.Received(1).GetFilteredMetricsAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Is<int?>(t => t == 10),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var metrics = new List<OperationalMetric>();
        var query = new GetMetricsQuery { Category = "NonExistent" };

        _metricsRepository.GetFilteredMetricsAsync(
            category: Arg.Any<string?>(),
            metricName: Arg.Any<string?>(),
            startDate: Arg.Any<DateTime?>(),
            endDate: Arg.Any<DateTime?>(),
            take: Arg.Any<int?>(),
            latestOnly: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OperationalMetric>>(metrics));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ConvertsMetricsToDto_WithCorrectProperties()
    {
        // Arrange
        var metric = CreateTestMetric("CPU Usage", "Performance", 75.5);
        var metrics = new List<OperationalMetric> { metric };
        var query = new GetMetricsQuery();

        _metricsRepository.GetFilteredMetricsAsync(
            category: Arg.Any<string?>(),
            metricName: Arg.Any<string?>(),
            startDate: Arg.Any<DateTime?>(),
            endDate: Arg.Any<DateTime?>(),
            take: Arg.Any<int?>(),
            latestOnly: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OperationalMetric>>(metrics));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var dto = result.Value.First();
        dto.MetricName.Should().Be("CPU Usage");
        dto.Category.Should().Be("Performance");
        dto.Value.Should().Be(75.5);
    }

    private static OperationalMetric CreateTestMetric(
        string name,
        string category,
        double value)
    {
        return new OperationalMetric(
            metricName: name,
            category: category,
            source: "Test",
            value: value,
            unit: "%",
            severity: MetricSeverity.Normal);
    }
}

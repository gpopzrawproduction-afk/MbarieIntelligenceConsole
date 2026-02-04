using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Tests.Unit.Services;

public class UpdateServiceTests
{
    private readonly Mock<ILogger<UpdateService>> _loggerMock;

    public UpdateServiceTests()
    {
        _loggerMock = new Mock<ILogger<UpdateService>>();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WithNewerVersion_ReturnsUpdateInfo()
    {
        // Arrange
        var currentVersion = "1.0.0";
        var latestVersion = "1.1.0";

        var mockResponse = new
        {
            tag_name = $"v{latestVersion}",
            body = "Release notes",
            assets = new[]
            {
                new
                {
                    name = "MIC.Desktop.Avalonia.msix",
                    browser_download_url = "https://example.com/download.msix",
                    size = 1000000
                }
            }
        };

        var httpClient = new HttpClient(new MockHttpMessageHandler(mockResponse));
        var sut = new UpdateService(httpClient, _loggerMock.Object);

        // Act
        var result = await sut.CheckForUpdatesAsync(currentVersion);

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be(latestVersion);
        result.DownloadUrl.Should().Be("https://example.com/download.msix");
        result.ReleaseNotes.Should().Be("Release notes");
        result.Size.Should().Be(1000000);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WithSameVersion_ReturnsNull()
    {
        // Arrange
        var currentVersion = "1.0.0";

        var mockResponse = new
        {
            tag_name = "v1.0.0",
            body = "Release notes",
            assets = Array.Empty<object>()
        };

        var httpClient = new HttpClient(new MockHttpMessageHandler(mockResponse));
        var sut = new UpdateService(httpClient, _loggerMock.Object);

        // Act
        var result = await sut.CheckForUpdatesAsync(currentVersion);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WithException_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient(new ExceptionHttpMessageHandler());
        var sut = new UpdateService(httpClient, _loggerMock.Object);

        // Act
        var result = await sut.CheckForUpdatesAsync("1.0.0");

        // Assert
        result.Should().BeNull();
    }

    // Mock HTTP message handler for testing
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly object _response;

        public MockHttpMessageHandler(object response)
        {
            _response = response;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(_response)
            };
        }
    }

    private class ExceptionHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Network error");
        }
    }
}
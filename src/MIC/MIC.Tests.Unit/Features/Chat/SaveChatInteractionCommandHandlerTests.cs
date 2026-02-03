using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentAssertions;
using MIC.Core.Application.Chat.Commands.SaveChatInteraction;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MIC.Tests.Unit.Features.Chat;

public class SaveChatInteractionCommandHandlerTests
{
    private readonly SaveChatInteractionCommandHandler _sut;
    private readonly IChatHistoryRepository _chatHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveChatInteractionCommandHandlerTests()
    {
        _chatHistoryRepository = Substitute.For<IChatHistoryRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new SaveChatInteractionCommandHandler(_chatHistoryRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsChatHistoryId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid().ToString();
        var command = new SaveChatInteractionCommand(
            UserId: userId,
            SessionId: sessionId,
            Query: "What is the weather?",
            Response: "The weather is sunny.");

        ChatHistory? capturedEntry = null;
        _chatHistoryRepository.AddAsync(Arg.Do<ChatHistory>(e => capturedEntry = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
        capturedEntry.Should().NotBeNull();
        capturedEntry!.UserId.Should().Be(userId);
        capturedEntry.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SavesChatWithCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = "session-123";
        var timestamp = DateTimeOffset.UtcNow;
        var command = new SaveChatInteractionCommand(
            UserId: userId,
            SessionId: sessionId,
            Query: "Tell me about MIC",
            Response: "MIC is the Mbarie Intelligence Console.",
            Timestamp: timestamp,
            AiProvider: "OpenAI",
            ModelUsed: "gpt-4",
            TokenCount: 150,
            IsSuccessful: true,
            Context: "Dashboard context",
            Metadata: "{\"source\":\"dashboard\"}");

        ChatHistory? capturedEntry = null;
        _chatHistoryRepository.AddAsync(Arg.Do<ChatHistory>(e => capturedEntry = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.Query.Should().Be("Tell me about MIC");
        capturedEntry.Response.Should().Be("MIC is the Mbarie Intelligence Console.");
        capturedEntry.AIProvider.Should().Be("OpenAI");
        capturedEntry.ModelUsed.Should().Be("gpt-4");
        capturedEntry.TokenCount.Should().Be(150);
        capturedEntry.IsSuccessful.Should().BeTrue();
        capturedEntry.Context.Should().Be("Dashboard context");
        capturedEntry.Metadata.Should().Be("{\"source\":\"dashboard\"}");
    }

    [Fact]
    public async Task Handle_WithFailedInteraction_SavesErrorDetails()
    {
        // Arrange
        var command = new SaveChatInteractionCommand(
            UserId: Guid.NewGuid(),
            SessionId: "session-456",
            Query: "Invalid query",
            Response: "",
            IsSuccessful: false,
            ErrorMessage: "API rate limit exceeded");

        ChatHistory? capturedEntry = null;
        _chatHistoryRepository.AddAsync(Arg.Do<ChatHistory>(e => capturedEntry = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.IsSuccessful.Should().BeFalse();
        capturedEntry.ErrorMessage.Should().Be("API rate limit exceeded");
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ReturnsFailureError()
    {
        // Arrange
        var command = new SaveChatInteractionCommand(
            UserId: Guid.NewGuid(),
            SessionId: "session-789",
            Query: "Test query",
            Response: "Test response");

        _chatHistoryRepository.AddAsync(Arg.Any<ChatHistory>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("ChatHistory.SaveFailed");
        result.FirstError.Description.Should().Contain("Database connection failed");
    }

    [Fact]
    public async Task Handle_WithNullTimestamp_UsesCurrentUtcTime()
    {
        // Arrange
        var beforeTest = DateTimeOffset.UtcNow;
        var command = new SaveChatInteractionCommand(
            UserId: Guid.NewGuid(),
            SessionId: "session-test",
            Query: "Query",
            Response: "Response",
            Timestamp: null);

        ChatHistory? capturedEntry = null;
        _chatHistoryRepository.AddAsync(Arg.Do<ChatHistory>(e => capturedEntry = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.Timestamp.Should().BeOnOrAfter(beforeTest);
        capturedEntry.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_CallsRepositoryAndUnitOfWork_InCorrectOrder()
    {
        // Arrange
        var command = new SaveChatInteractionCommand(
            UserId: Guid.NewGuid(),
            SessionId: "session-order",
            Query: "Query",
            Response: "Response");

        _chatHistoryRepository.AddAsync(Arg.Any<ChatHistory>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            _chatHistoryRepository.AddAsync(Arg.Any<ChatHistory>(), Arg.Any<CancellationToken>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WithNullTokenCount_DefaultsToZero()
    {
        // Arrange
        var command = new SaveChatInteractionCommand(
            UserId: Guid.NewGuid(),
            SessionId: "session-tokens",
            Query: "Query",
            Response: "Response",
            TokenCount: null);

        ChatHistory? capturedEntry = null;
        _chatHistoryRepository.AddAsync(Arg.Do<ChatHistory>(e => capturedEntry = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedEntry.Should().NotBeNull();
        capturedEntry!.TokenCount.Should().Be(0);
    }
}

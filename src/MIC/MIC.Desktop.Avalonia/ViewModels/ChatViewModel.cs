using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MIC.Infrastructure.AI.Services;
using ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Intelligence;
using System.Linq;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using MediatR;
using MIC.Core.Application.Chat.Commands.SaveChatInteraction;
using MIC.Core.Application.Chat.Queries.GetChatHistory;
using MIC.Core.Application.Chat.Commands.ClearChatSession;
using Unit = System.Reactive.Unit;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Claude AI-style chat interface.
/// </summary>
public class ChatViewModel : ViewModelBase
{
    private readonly IChatService? _chatService;
    private readonly IKnowledgeBaseService? _knowledgeBaseService;
    private readonly PositionQuestionnaireService? _questionnaireService;
    private readonly IMediator? _mediator;
    private readonly string _conversationId;
    private string _userInput = string.Empty;
    private bool _isAITyping;
    private string _statusText = "Online";
    private bool _showSuggestions = true;
    private User? _currentUser;

    public ChatViewModel()
    {
        Console.WriteLine($"[ChatViewModel] Constructor called");
        
        _conversationId = Guid.NewGuid().ToString();
        Messages = new ObservableCollection<ChatMessageViewModel>();
        SuggestedQuestions = new ObservableCollection<string>
        {
            "Show critical alerts",
            "Revenue trends",
            "Performance summary",
            "Today's insights"
        };

        // Try to get services from DI
        _chatService = Program.ServiceProvider?.GetService(typeof(IChatService)) as IChatService;
        _knowledgeBaseService = Program.ServiceProvider?.GetService(typeof(IKnowledgeBaseService)) as IKnowledgeBaseService;
        _questionnaireService = Program.ServiceProvider?.GetService(typeof(PositionQuestionnaireService)) as PositionQuestionnaireService;
        _mediator = Program.ServiceProvider?.GetService(typeof(IMediator)) as IMediator;
        
        Console.WriteLine($"[ChatViewModel] ChatService injected: {_chatService != null}");
        Console.WriteLine($"[ChatViewModel] KnowledgeBaseService injected: {_knowledgeBaseService != null}");
        Console.WriteLine($"[ChatViewModel] QuestionnaireService injected: {_questionnaireService != null}");
        Console.WriteLine($"[ChatViewModel] Mediator injected: {_mediator != null}");

        // Get current user
        _currentUser = GetUserSession();
        
        // Load chat history from database
        _ = LoadChatHistoryAsync();

        // Set up commands
        SendMessageCommand = ReactiveCommand.CreateFromTask(
            async () => await SendMessageAsync(),
            this.WhenAnyValue(x => x.CanSend)
                .ObserveOn(RxApp.MainThreadScheduler));

        // InsertNewLineCommand for Shift+Enter
        InsertNewLineCommand = ReactiveCommand.Create(() =>
        {
            UserInput += Environment.NewLine;
        });

        ClearChatCommand = ReactiveCommand.CreateFromTask(async () => await ClearChatAsync());

        UseSuggestionCommand = ReactiveCommand.CreateFromTask<string>(async (s) => await UseSuggestionAsync(s));

        // Add welcome message
        AddWelcomeMessage();

        // Test API connection
        Task.Run(async () =>
        {
            try
            {
                if (_chatService != null)
                {
                    Console.WriteLine($"[ChatViewModel] Testing API connection...");
                    var testResponse = await _chatService.SendMessageAsync("Test");
                    Console.WriteLine($"[ChatViewModel] Test response success: {testResponse.Success}");
                    Console.WriteLine($"[ChatViewModel] Test response error: {testResponse.Error}");
                }
                else
                {
                    Console.WriteLine($"[ChatViewModel] ChatService is null - API not configured");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatViewModel] API test error: {ex.Message}");
            }
        });
    }

    public ChatViewModel(IChatService chatService) : this()
    {
        _chatService = chatService;
    }

    #region Properties

    public ObservableCollection<ChatMessageViewModel> Messages { get; }
    public ObservableCollection<string> SuggestedQuestions { get; }


    public string UserInput
    {
        get => _userInput;
        set
        {
            this.RaiseAndSetIfChanged(ref _userInput, value);
            this.RaisePropertyChanged(nameof(CanSend));
        }
    }

    public bool IsAITyping
    {
        get => _isAITyping;
        set => this.RaiseAndSetIfChanged(ref _isAITyping, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public bool ShowSuggestions
    {
        get => _showSuggestions && Messages.Count <= 1;
        set => this.RaiseAndSetIfChanged(ref _showSuggestions, value);
    }

    public bool CanSend => !string.IsNullOrWhiteSpace(UserInput) && !IsAITyping;

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearChatCommand { get; }
    public ReactiveCommand<string, Unit> UseSuggestionCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertNewLineCommand { get; }

    #endregion

    #region Methods

    private async Task LoadChatHistoryAsync()
    {
        try
        {
            if (_mediator == null || _currentUser == null || _currentUser.Id == Guid.Empty)
            {
                Console.WriteLine($"[ChatViewModel] Cannot load chat history: mediator={_mediator != null}, user={_currentUser != null}, userId={_currentUser?.Id}");
                return;
            }

            var session = UserSessionService.Instance.CurrentSession;
            if (session?.UserId == null)
            {
                Console.WriteLine($"[ChatViewModel] No user session available");
                return;
            }

            var userId = Guid.Parse(session.UserId);
            Console.WriteLine($"[ChatViewModel] Loading chat history for user {userId}, session {_conversationId}");
            
            var query = new GetChatHistoryQuery(userId, _conversationId, 100);
            var result = await _mediator.Send(query);
            
            if (!result.IsError && result.Value.Any())
            {
                // Clear existing messages (including welcome message)
                Messages.Clear();
                
                // Load history
                foreach (var msg in result.Value)
                {
                    Messages.Add(new ChatMessageViewModel
                    {
                        Content = $"User: {msg.Query}\n\nAssistant: {msg.Response}",
                        IsUser = false, // We'll show combined messages as assistant responses
                        Timestamp = msg.Timestamp.LocalDateTime
                    });
                }
                
                // Add welcome message if no messages loaded
                if (Messages.Count == 0)
                {
                    AddWelcomeMessage();
                }
                
                Console.WriteLine($"[ChatViewModel] Loaded {result.Value.Count} chat history items");
            }
            else
            {
                Console.WriteLine($"[ChatViewModel] No chat history found or error: {result.FirstError.Description}");
            }
        }
        catch (Exception ex)
        {
            // Don't crash on load failure, just log it
            Console.WriteLine($"[ChatViewModel] Error loading chat history: {ex.Message}");
        }
    }

    private async Task SaveMessageToDatabase(string query, string response, bool isSuccessful = true, string? errorMessage = null)
    {
        try
        {
            if (_mediator == null || _currentUser == null || _currentUser.Id == Guid.Empty)
            {
                Console.WriteLine($"[ChatViewModel] Cannot save message: mediator={_mediator != null}, user={_currentUser != null}");
                return;
            }

            var session = UserSessionService.Instance.CurrentSession;
            if (session?.UserId == null)
            {
                Console.WriteLine($"[ChatViewModel] No user session available for saving");
                return;
            }

            var userId = Guid.Parse(session.UserId);
            Console.WriteLine($"[ChatViewModel] Saving chat interaction for user {userId}, session {_conversationId}");
            
            var command = new SaveChatInteractionCommand(
                UserId: userId,
                SessionId: _conversationId,
                Query: query,
                Response: response,
                IsSuccessful: isSuccessful,
                ErrorMessage: errorMessage);
            
            var result = await _mediator.Send(command);
            
            if (result.IsError)
            {
                Console.WriteLine($"[ChatViewModel] Failed to save chat message: {result.FirstError.Description}");
            }
            else
            {
                Console.WriteLine($"[ChatViewModel] Chat message saved with ID: {result.Value}");
            }
        }
        catch (Exception ex)
        {
            // Don't crash on save failure, just log it
            Console.WriteLine($"[ChatViewModel] Error saving chat message: {ex.Message}");
        }
    }

    private User? GetUserSession()
    {
        // Get current user from session
        var session = UserSessionService.Instance.CurrentSession;
        if (session?.UserId != null)
        {
            // In a real implementation, you would fetch the user from the database
            // For now, we'll return a basic user object
            var user = new User
            {
                Username = session.Username,
                Email = session.Email,
                FullName = session.DisplayName,
                JobPosition = session.Position, // Assuming Position is added to session
                Department = session.Department // Assuming Department is added to session
            };
            // Note: Id is read-only and auto-generated by the base class
            return user;
        }
        return null;
    }

    private void AddWelcomeMessage()
    {
        var welcomeText = "Hello! I'm your AI assistant for Mbarie Intelligence Console. ";
        
        if (_currentUser != null)
        {
            welcomeText += $"I recognize you as {_currentUser.FullName ?? _currentUser.Username}";
            if (!string.IsNullOrEmpty(_currentUser.JobPosition))
            {
                welcomeText += $" in the position of {_currentUser.JobPosition}";
            }
            if (!string.IsNullOrEmpty(_currentUser.Department))
            {
                welcomeText += $" in the {_currentUser.Department} department.";
            }
            welcomeText += " ";
        }
        
        welcomeText += "I can help you analyze metrics, review alerts, search your email history, and provide business insights. What would you like to know?";
        
        Messages.Add(new ChatMessageViewModel
        {
            Content = welcomeText,
            IsUser = false,
            Timestamp = DateTime.Now
        });
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput)) return;

        var userMessage = UserInput.Trim();
        UserInput = string.Empty;

        // Add user message to chat
        var userMessageVm = new ChatMessageViewModel
        {
            Content = userMessage,
            IsUser = true,
            Timestamp = DateTime.Now
        };
        Messages.Add(userMessageVm);

        // Hide suggestions after first message
        this.RaisePropertyChanged(nameof(ShowSuggestions));

        // Show typing indicator
        IsAITyping = true;
        StatusText = "Thinking...";

        try
        {
            if (_chatService == null)
            {
                // Fallback response when AI is not configured
                await Task.Delay(500); // Simulate thinking
                var fallbackResponse = "AI services are not configured. Please add your OpenAI API key to the settings to enable AI chat functionality.";
                Messages.Add(new ChatMessageViewModel
                {
                    Content = fallbackResponse,
                    IsUser = false,
                    Timestamp = DateTime.Now
                });
                
                // Save fallback interaction to database
                await SaveMessageToDatabase(userMessage, fallbackResponse, isSuccessful: false, errorMessage: "AI not configured");
            }
            else
            {
                // Check if the query relates to user's emails or knowledge base
                var aiResponse = await ProcessUserQuery(userMessage);

                Messages.Add(new ChatMessageViewModel
                {
                    Content = aiResponse,
                    IsUser = false,
                    Timestamp = DateTime.Now
                });
                
                // Save successful interaction to database
                await SaveMessageToDatabase(userMessage, aiResponse, isSuccessful: true);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"An error occurred: {ex.Message}";
            Messages.Add(new ChatMessageViewModel
            {
                Content = errorMessage,
                IsUser = false,
                Timestamp = DateTime.Now
            });
            
            // Save failed interaction to database
            await SaveMessageToDatabase(userMessage, errorMessage, isSuccessful: false, errorMessage: ex.Message);
        }
        finally
        {
            IsAITyping = false;
            StatusText = "Online";
        }
    }

    private async Task<string> ProcessUserQuery(string userMessage)
    {
        // First, check if this is a knowledge-based query
        if (_knowledgeBaseService != null && _currentUser != null && _currentUser.Id != Guid.Empty)
{
            // Search knowledge base for relevant information
            var knowledgeResults = await _knowledgeBaseService.SearchAsync(userMessage, _currentUser.Id);
            
            if (knowledgeResults.Any())
            {
                // Format knowledge results to include in AI response
                var knowledgeContext = "Based on your email history and documents:\n";
                foreach (var knowledgeResult in knowledgeResults.Take(3)) // Limit to top 3 results
                {
                    knowledgeContext += $"- {knowledgeResult.Title}: {knowledgeResult.Content.Substring(0, Math.Min(knowledgeResult.Content.Length, 200))}...\n";
                }
                
                // Send message to AI with knowledge context
                var enrichedQuery = $"User query: {userMessage}\n\nRelevant information from user's knowledge base:\n{knowledgeContext}";
                if (_chatService != null)
                {
                    var aiResult = await _chatService.SendMessageAsync(enrichedQuery, _conversationId);
                    
                    if (aiResult.Success)
                    {
                        return aiResult.Response;
                    }
                    else
                    {
                        return $"Sorry, I encountered an error: {aiResult.Error}";
                    }
                }
                else
                {
                    return "AI services are not configured.";
                }
            }
        }
        
        // If no knowledge base results or service not available, proceed with regular AI query
        if (_chatService != null)
        {
            var result = await _chatService.SendMessageAsync(userMessage, _conversationId);

            if (result.Success)
            {
                return result.Response;
            }
            else
            {
                return $"Sorry, I encountered an error: {result.Error}";
            }
        }
        else
        {
            return "AI services are not configured.";
        }
    }

    private async Task ClearChatAsync()
    {
        try
        {
            // Clear database history
            if (_mediator != null)
            {
                var session = UserSessionService.Instance.CurrentSession;
                if (session?.UserId != null)
                {
                    var userId = Guid.Parse(session.UserId);
                    var command = new ClearChatSessionCommand(userId, _conversationId);
                    var result = await _mediator.Send(command);
                    
                    if (result.IsError)
                    {
                        Console.WriteLine($"[ChatViewModel] Failed to clear chat history from database: {result.FirstError.Description}");
                    }
                    else
                    {
                        Console.WriteLine($"[ChatViewModel] Chat history cleared from database");
                    }
                }
            }

            // Clear in-memory chat service history
            if (_chatService != null)
            {
                await _chatService.ClearConversationAsync(_conversationId);
            }

            // Clear UI messages
            Messages.Clear();

            // Add welcome message
            AddWelcomeMessage();
            this.RaisePropertyChanged(nameof(ShowSuggestions));
            
            Console.WriteLine($"[ChatViewModel] Chat cleared successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatViewModel] Error clearing chat: {ex.Message}");
        }
    }

    private async Task UseSuggestionAsync(string suggestion)
    {
        UserInput = suggestion;
        await SendMessageAsync();
    }

    #endregion
}

/// <summary>
/// ViewModel for individual chat messages.
/// </summary>
public class ChatMessageViewModel : ViewModelBase
{
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; }

    public string FormattedTime => Timestamp.ToString("HH:mm");
}

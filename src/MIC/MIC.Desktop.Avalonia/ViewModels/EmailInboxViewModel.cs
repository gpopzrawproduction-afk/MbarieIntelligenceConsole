using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Emails.Common;
using MIC.Core.Application.Emails.Queries.GetEmails;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Email Inbox view with AI-powered intelligence.
/// </summary>
public class EmailInboxViewModel : ViewModelBase
{
    private readonly IMediator? _mediator;
    
    private bool _isLoading;
    private EmailDto? _selectedEmail;
    private string _searchText = string.Empty;
    private EmailFolder _selectedFolder = EmailFolder.Inbox;
    private EmailCategory? _selectedCategory;
    private EmailPriority? _selectedPriority;
    private bool _showUnreadOnly;
    private bool _showRequiresResponseOnly;
    private int _totalEmails;
    private int _unreadCount;
    private int _requiresResponseCount;

    public EmailInboxViewModel()
    {
        _mediator = Program.ServiceProvider?.GetService<IMediator>();
        
        // Initialize commands
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadEmailsAsync);
        MarkAsReadCommand = ReactiveCommand.CreateFromTask<EmailDto>(MarkAsReadAsync);
        ToggleFlagCommand = ReactiveCommand.CreateFromTask<EmailDto>(ToggleFlagAsync);
        ArchiveCommand = ReactiveCommand.CreateFromTask<EmailDto>(ArchiveEmailAsync);
        DeleteCommand = ReactiveCommand.CreateFromTask<EmailDto>(DeleteEmailAsync);
        ConnectAccountCommand = ReactiveCommand.Create(ConnectAccount);
        ExportCommand = ReactiveCommand.CreateFromTask(ExportEmailsAsync);

        // Auto-refresh when filters change
        this.WhenAnyValue(
            x => x.SelectedFolder,
            x => x.SelectedCategory,
            x => x.SelectedPriority,
            x => x.ShowUnreadOnly,
            x => x.ShowRequiresResponseOnly)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(_ => Unit.Default)
            .InvokeCommand(RefreshCommand);

        // Search with debounce
        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(_ => Unit.Default)
            .InvokeCommand(RefreshCommand);

        // Load initial data
        _ = LoadEmailsAsync();
    }

    #region Properties

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ObservableCollection<EmailDto> Emails { get; } = new();

    public EmailDto? SelectedEmail
    {
        get => _selectedEmail;
        set => this.RaiseAndSetIfChanged(ref _selectedEmail, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public EmailFolder SelectedFolder
    {
        get => _selectedFolder;
        set => this.RaiseAndSetIfChanged(ref _selectedFolder, value);
    }

    public EmailCategory? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    public EmailPriority? SelectedPriority
    {
        get => _selectedPriority;
        set => this.RaiseAndSetIfChanged(ref _selectedPriority, value);
    }

    public bool ShowUnreadOnly
    {
        get => _showUnreadOnly;
        set => this.RaiseAndSetIfChanged(ref _showUnreadOnly, value);
    }

    public bool ShowRequiresResponseOnly
    {
        get => _showRequiresResponseOnly;
        set => this.RaiseAndSetIfChanged(ref _showRequiresResponseOnly, value);
    }

    public int TotalEmails
    {
        get => _totalEmails;
        set => this.RaiseAndSetIfChanged(ref _totalEmails, value);
    }

    public int UnreadCount
    {
        get => _unreadCount;
        set => this.RaiseAndSetIfChanged(ref _unreadCount, value);
    }

    public int RequiresResponseCount
    {
        get => _requiresResponseCount;
        set => this.RaiseAndSetIfChanged(ref _requiresResponseCount, value);
    }

    // Folder options
    public ObservableCollection<FolderOption> Folders { get; } = new()
    {
        new FolderOption("Inbox", EmailFolder.Inbox, "??"),
        new FolderOption("Sent", EmailFolder.Sent, "??"),
        new FolderOption("Drafts", EmailFolder.Drafts, "??"),
        new FolderOption("Archive", EmailFolder.Archive, "??"),
        new FolderOption("Junk", EmailFolder.Junk, "??")
    };

    // Category filter options
    public ObservableCollection<CategoryOption> Categories { get; } = new()
    {
        new CategoryOption("All Categories", null),
        new CategoryOption("?? Meeting", EmailCategory.Meeting),
        new CategoryOption("?? Project", EmailCategory.Project),
        new CategoryOption("? Decision", EmailCategory.Decision),
        new CategoryOption("? Action", EmailCategory.Action),
        new CategoryOption("?? Report", EmailCategory.Report),
        new CategoryOption("? FYI", EmailCategory.FYI)
    };

    // Priority filter options
    public ObservableCollection<PriorityOption> Priorities { get; } = new()
    {
        new PriorityOption("All Priorities", null),
        new PriorityOption("?? Urgent", EmailPriority.Urgent),
        new PriorityOption("?? High", EmailPriority.High),
        new PriorityOption("?? Normal", EmailPriority.Normal),
        new PriorityOption("? Low", EmailPriority.Low)
    };

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<EmailDto, Unit> MarkAsReadCommand { get; }
    public ReactiveCommand<EmailDto, Unit> ToggleFlagCommand { get; }
    public ReactiveCommand<EmailDto, Unit> ArchiveCommand { get; }
    public ReactiveCommand<EmailDto, Unit> DeleteCommand { get; }
    public ReactiveCommand<Unit, Unit> ConnectAccountCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }

    #endregion

    #region Methods

    private async Task LoadEmailsAsync()
    {
        if (_mediator == null)
        {
            // Load demo data when no mediator available
            LoadDemoData();
            return;
        }

        try
        {
            IsLoading = true;

            var userId = Guid.Parse(UserSessionService.Instance.CurrentSession?.UserId ?? Guid.Empty.ToString());
            
            var query = new GetEmailsQuery
            {
                UserId = userId,
                Folder = SelectedFolder,
                Category = SelectedCategory,
                Priority = SelectedPriority,
                IsUnread = ShowUnreadOnly ? true : null,
                RequiresResponse = ShowRequiresResponseOnly ? true : null,
                SearchText = SearchText,
                Take = 100
            };

            var result = await _mediator.Send(query);

            if (!result.IsError)
            {
                Emails.Clear();
                foreach (var email in result.Value)
                {
                    Emails.Add(email);
                }
                TotalEmails = result.Value.Count;
                UnreadCount = result.Value.Count(e => !e.IsRead);
                RequiresResponseCount = result.Value.Count(e => e.RequiresResponse);
            }
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Load Emails");
            LoadDemoData();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadDemoData()
    {
        Emails.Clear();

        var demoEmails = new[]
        {
            new EmailDto
            {
                Id = Guid.NewGuid(),
                Subject = "Q4 Budget Review - Action Required",
                FromAddress = "cfo@company.com",
                FromName = "John Smith (CFO)",
                ToRecipients = "team@company.com",
                ReceivedDate = DateTime.UtcNow.AddHours(-2),
                BodyPreview = "Please review the attached Q4 budget projections and provide your feedback by EOD Friday...",
                IsRead = false,
                HasAttachments = true,
                AIPriority = EmailPriority.Urgent,
                AICategory = EmailCategory.Decision,
                Sentiment = SentimentType.Neutral,
                RequiresResponse = true,
                ContainsActionItems = true,
                AISummary = "CFO requests budget review feedback by Friday. Contains action items for department heads.",
                Folder = EmailFolder.Inbox
            },
            new EmailDto
            {
                Id = Guid.NewGuid(),
                Subject = "Project Alpha - Weekly Status Update",
                FromAddress = "pm@company.com",
                FromName = "Sarah Johnson",
                ToRecipients = "stakeholders@company.com",
                ReceivedDate = DateTime.UtcNow.AddHours(-5),
                BodyPreview = "Hi team, here's our weekly status update for Project Alpha. We're on track for the milestone...",
                IsRead = true,
                HasAttachments = false,
                AIPriority = EmailPriority.Normal,
                AICategory = EmailCategory.Report,
                Sentiment = SentimentType.Positive,
                RequiresResponse = false,
                ContainsActionItems = false,
                AISummary = "Weekly project status - on track for milestone delivery.",
                Folder = EmailFolder.Inbox
            },
            new EmailDto
            {
                Id = Guid.NewGuid(),
                Subject = "Meeting: Technical Architecture Review",
                FromAddress = "calendar@company.com",
                FromName = "Calendar",
                ToRecipients = "engineering@company.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-1),
                BodyPreview = "You have been invited to a meeting: Technical Architecture Review. Date: Tomorrow 2:00 PM...",
                IsRead = false,
                HasAttachments = true,
                AIPriority = EmailPriority.High,
                AICategory = EmailCategory.Meeting,
                Sentiment = SentimentType.Neutral,
                RequiresResponse = true,
                ContainsActionItems = true,
                AISummary = "Meeting invitation for architecture review tomorrow. RSVP required.",
                Folder = EmailFolder.Inbox
            },
            new EmailDto
            {
                Id = Guid.NewGuid(),
                Subject = "Server Alert: High CPU Usage Detected",
                FromAddress = "alerts@monitoring.company.com",
                FromName = "System Monitoring",
                ToRecipients = "ops@company.com",
                ReceivedDate = DateTime.UtcNow.AddMinutes(-30),
                BodyPreview = "CRITICAL: Server PROD-WEB-01 has exceeded 95% CPU usage for the past 15 minutes...",
                IsRead = false,
                HasAttachments = false,
                AIPriority = EmailPriority.Urgent,
                AICategory = EmailCategory.Action,
                Sentiment = SentimentType.Negative,
                RequiresResponse = true,
                ContainsActionItems = true,
                AISummary = "Critical server alert - immediate action required for PROD-WEB-01.",
                Folder = EmailFolder.Inbox
            },
            new EmailDto
            {
                Id = Guid.NewGuid(),
                Subject = "Company Newsletter - January Edition",
                FromAddress = "hr@company.com",
                FromName = "HR Department",
                ToRecipients = "all@company.com",
                ReceivedDate = DateTime.UtcNow.AddDays(-2),
                BodyPreview = "Welcome to our January newsletter! This month we celebrate several milestones...",
                IsRead = true,
                HasAttachments = false,
                AIPriority = EmailPriority.Low,
                AICategory = EmailCategory.FYI,
                Sentiment = SentimentType.Positive,
                RequiresResponse = false,
                ContainsActionItems = false,
                AISummary = "Monthly company newsletter with updates and announcements.",
                Folder = EmailFolder.Inbox
            }
        };

        foreach (var email in demoEmails)
        {
            Emails.Add(email);
        }

        TotalEmails = demoEmails.Length;
        UnreadCount = demoEmails.Count(e => !e.IsRead);
        RequiresResponseCount = demoEmails.Count(e => e.RequiresResponse);
    }

    private async Task MarkAsReadAsync(EmailDto email)
    {
        if (email == null) return;
        NotificationService.Instance.ShowInfo($"Marked as read: {email.Subject}");
        await LoadEmailsAsync();
    }

    private async Task ToggleFlagAsync(EmailDto email)
    {
        if (email == null) return;
        NotificationService.Instance.ShowInfo($"Flag toggled: {email.Subject}");
        await Task.CompletedTask;
    }

    private async Task ArchiveEmailAsync(EmailDto email)
    {
        if (email == null) return;
        NotificationService.Instance.ShowSuccess($"Archived: {email.Subject}");
        Emails.Remove(email);
        await Task.CompletedTask;
    }

    private async Task DeleteEmailAsync(EmailDto email)
    {
        if (email == null) return;
        NotificationService.Instance.ShowInfo($"Deleted: {email.Subject}");
        Emails.Remove(email);
        await Task.CompletedTask;
    }

    private void ConnectAccount()
    {
        NotificationService.Instance.ShowInfo("Email account connection will open OAuth flow. Coming soon!");
    }

    private async Task ExportEmailsAsync()
    {
        NotificationService.Instance.ShowInfo("Exporting emails...");
        await Task.CompletedTask;
    }

    #endregion
}

#region Supporting Types

public record FolderOption(string Name, EmailFolder Folder, string Icon);
public record CategoryOption(string Name, EmailCategory? Category);
public record PriorityOption(string Name, EmailPriority? Priority);

#endregion

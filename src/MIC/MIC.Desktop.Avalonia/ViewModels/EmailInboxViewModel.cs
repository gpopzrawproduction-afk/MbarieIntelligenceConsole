using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Emails.Common;
using MIC.Core.Application.Emails.Queries.GetEmails;
using MIC.Core.Application.Email.Commands.AddEmailAccount;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.Services;
using MIC.Desktop.Avalonia.Views.Dialogs;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Email Inbox view with AI-powered intelligence.
/// </summary>
public class EmailInboxViewModel : ViewModelBase
{
    private readonly IMediator? _mediator;
    private readonly IEmailSyncService? _emailSyncService;
    private readonly IEmailAccountRepository? _emailAccountRepository;
    private readonly IEmailRepository? _emailRepository;
    private readonly IEmailOAuth2Service? _gmailOAuthService;
    private readonly IEmailOAuth2Service? _outlookOAuthService;
    
    private bool _isLoading;
    private bool _isSyncing;
    private string _syncStatus = string.Empty;
    private string _statusText = "Ready";
    private EmailDto? _selectedEmail;
    private string _searchText = string.Empty;
    private EmailFolder _selectedFolder = EmailFolder.Inbox;
    private CategoryOption? _selectedCategoryOption;
    private PriorityOption? _selectedPriorityOption;
    private bool _showUnreadOnly;
    private bool _showRequiresResponseOnly;
    private int _totalEmails;
    private int _unreadCount;
    private int _requiresResponseCount;

    public EmailInboxViewModel()
    {
        _mediator = Program.ServiceProvider?.GetService<IMediator>();
        _emailSyncService = Program.ServiceProvider?.GetService<IEmailSyncService>();
        _emailAccountRepository = Program.ServiceProvider?.GetService<IEmailAccountRepository>();
        _emailRepository = Program.ServiceProvider?.GetService<IEmailRepository>();
        _gmailOAuthService = Program.ServiceProvider?.GetKeyedService<IEmailOAuth2Service>("Gmail");
        _outlookOAuthService = Program.ServiceProvider?.GetKeyedService<IEmailOAuth2Service>("Outlook");
        
        // Initialize commands
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadEmailsAsync);
        SyncCommand = ReactiveCommand.CreateFromTask(SyncEmailsAsync, this.WhenAnyValue(x => x.IsSyncing).Select(syncing => !syncing));
        MarkAsReadCommand = ReactiveCommand.CreateFromTask<EmailDto>(MarkAsReadAsync);
        ToggleFlagCommand = ReactiveCommand.CreateFromTask<EmailDto>(ToggleFlagAsync);
        ArchiveCommand = ReactiveCommand.CreateFromTask<EmailDto>(ArchiveEmailAsync);
        DeleteCommand = ReactiveCommand.CreateFromTask<EmailDto>(DeleteEmailAsync);
        AddGmailAccountCommand = ReactiveCommand.CreateFromTask(AddGmailAccountAsync, 
            this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));
        AddOutlookAccountCommand = ReactiveCommand.CreateFromTask(AddOutlookAccountAsync, 
            this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));
        AddDirectEmailAccountCommand = ReactiveCommand.CreateFromTask(
            AddDirectEmailAccountAsync,
            this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading)
        );
        ExportCommand = ReactiveCommand.CreateFromTask(ExportEmailsAsync);
        ComposeCommand = ReactiveCommand.CreateFromTask(ComposeEmailAsync,
            this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));
        ReplyCommand = ReactiveCommand.CreateFromTask<EmailDto>(ReplyToEmailAsync,
            this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));
        ForwardCommand = ReactiveCommand.CreateFromTask<EmailDto>(ForwardEmailAsync,
            this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));

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

    public bool IsSyncing
    {
        get => _isSyncing;
        set => this.RaiseAndSetIfChanged(ref _isSyncing, value);
    }

    public string SyncStatus
    {
        get => _syncStatus;
        set => this.RaiseAndSetIfChanged(ref _syncStatus, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
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

    public CategoryOption? SelectedCategory
    {
        get => _selectedCategoryOption ?? Categories.FirstOrDefault(c => c.Category == null);
        set => this.RaiseAndSetIfChanged(ref _selectedCategoryOption, value);
    }

    public PriorityOption? SelectedPriority
    {
        get => _selectedPriorityOption ?? Priorities.FirstOrDefault(p => p.Priority == null);
        set => this.RaiseAndSetIfChanged(ref _selectedPriorityOption, value);
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
        new FolderOption("Inbox", EmailFolder.Inbox, "📥"),
        new FolderOption("Sent", EmailFolder.Sent, "📤"),
        new FolderOption("Drafts", EmailFolder.Drafts, "📝"),
        new FolderOption("Archive", EmailFolder.Archive, "🗄️"),
        new FolderOption("Junk", EmailFolder.Junk, "🗑️")
    };

    // Category filter options
    public ObservableCollection<CategoryOption> Categories { get; } = new()
    {
        new CategoryOption("All Categories", null),
        new CategoryOption("📅 Meeting", EmailCategory.Meeting),
        new CategoryOption("📁 Project", EmailCategory.Project),
        new CategoryOption("⚖️ Decision", EmailCategory.Decision),
        new CategoryOption("✅ Action", EmailCategory.Action),
        new CategoryOption("📊 Report", EmailCategory.Report),
        new CategoryOption("ℹ️ FYI", EmailCategory.FYI)
    };

    // Priority filter options
    public ObservableCollection<PriorityOption> Priorities { get; } = new()
    {
        new PriorityOption("All Priorities", null),
        new PriorityOption("🚨 Urgent", EmailPriority.Urgent),
        new PriorityOption("⚠️ High", EmailPriority.High),
        new PriorityOption("🟢 Normal", EmailPriority.Normal),
        new PriorityOption("⬇️ Low", EmailPriority.Low)
    };

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> SyncCommand { get; }
    public ReactiveCommand<EmailDto, Unit> MarkAsReadCommand { get; }
    public ReactiveCommand<EmailDto, Unit> ToggleFlagCommand { get; }
    public ReactiveCommand<EmailDto, Unit> ArchiveCommand { get; }
    public ReactiveCommand<EmailDto, Unit> DeleteCommand { get; }
    public ReactiveCommand<Unit, Unit> AddGmailAccountCommand { get; }
    public ReactiveCommand<Unit, Unit> AddOutlookAccountCommand { get; }
    public ReactiveCommand<Unit, Unit> AddDirectEmailAccountCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }
    public ReactiveCommand<Unit, Unit> ComposeCommand { get; }
    public ReactiveCommand<EmailDto, Unit> ReplyCommand { get; }
    public ReactiveCommand<EmailDto, Unit> ForwardCommand { get; }

    #endregion

    #region Methods

    private async Task LoadEmailsAsync()
    {
        if (_mediator == null)
        {
            StatusText = "Email data service is not available.";
            Emails.Clear();
            TotalEmails = 0;
            UnreadCount = 0;
            RequiresResponseCount = 0;
            return;
        }

        try
        {
            IsLoading = true;

            var userId = Guid.Parse(UserSessionService.Instance.CurrentSession?.UserId ?? Guid.Empty.ToString());
            if (userId == Guid.Empty)
            {
                StatusText = "Please sign in to view emails.";
                Emails.Clear();
                TotalEmails = 0;
                UnreadCount = 0;
                RequiresResponseCount = 0;
                return;
            }
            
            var query = new GetEmailsQuery
            {
                UserId = userId,
                Folder = SelectedFolder,
                Category = SelectedCategory?.Category,
                Priority = SelectedPriority?.Priority,
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
            StatusText = "Failed to load emails.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SyncEmailsAsync()
    {
        if (_emailSyncService == null)
        {
            SyncStatus = "Sync service not available";
            NotificationService.Instance.ShowError("Email sync service is not available.");
            return;
        }

        if (_emailAccountRepository == null)
        {
            SyncStatus = "Email account repository not available";
            NotificationService.Instance.ShowError("Email account repository is not available.");
            return;
        }

        try
        {
            IsSyncing = true;
            SyncStatus = "Syncing emails...";
            
            var userId = Guid.Parse(UserSessionService.Instance.CurrentSession?.UserId ?? Guid.Empty.ToString());
            if (userId == Guid.Empty)
            {
                NotificationService.Instance.ShowError("User not logged in");
                SyncStatus = "Sync failed";
                return;
            }

            var accounts = await _emailAccountRepository.GetByUserIdAsync(userId);
            if (accounts.Count == 0)
            {
                SyncStatus = "No email accounts connected";
                NotificationService.Instance.ShowInfo("No email accounts connected.");
                return;
            }

            var totalNew = 0;
            foreach (var account in accounts)
            {
                var result = await _emailSyncService.SyncAccountAsync(account);
                if (!result.Success)
                {
                    NotificationService.Instance.ShowError(result.ErrorMessage ?? "Email sync failed.");
                    SyncStatus = "Sync failed";
                    return;
                }
                totalNew += result.NewEmailsCount;
            }

            SyncStatus = totalNew > 0
                ? $"Sync complete: {totalNew} new emails"
                : "Sync complete: no new emails";
            NotificationService.Instance.ShowSuccess(SyncStatus);
            
            // Refresh the inbox to show any new emails
            await LoadEmailsAsync();
            
            SyncStatus = "Ready";
        }
        catch (Exception ex)
        {
            SyncStatus = "Sync failed";
            NotificationService.Instance.ShowError($"Email sync failed: {ex.Message}");
            ErrorHandlingService.Instance.HandleException(ex, "Email Sync");
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async Task MarkAsReadAsync(EmailDto email)
    {
        if (email == null) return;
        if (_emailRepository == null)
        {
            NotificationService.Instance.ShowError("Email repository is not available.");
            return;
        }

        var entity = await _emailRepository.GetByIdAsync(email.Id);
        if (entity == null)
        {
            NotificationService.Instance.ShowError("Email not found.");
            return;
        }

        entity.MarkAsRead();
        await _emailRepository.UpdateAsync(entity);

        NotificationService.Instance.ShowInfo($"Marked as read: {email.Subject}");
        await LoadEmailsAsync();
    }

    private async Task ToggleFlagAsync(EmailDto email)
    {
        if (email == null) return;
        if (_emailRepository == null)
        {
            NotificationService.Instance.ShowError("Email repository is not available.");
            return;
        }

        var entity = await _emailRepository.GetByIdAsync(email.Id);
        if (entity == null)
        {
            NotificationService.Instance.ShowError("Email not found.");
            return;
        }

        entity.ToggleFlag();
        await _emailRepository.UpdateAsync(entity);

        NotificationService.Instance.ShowInfo($"Flag toggled: {email.Subject}");
        await LoadEmailsAsync();
    }

    private async Task ArchiveEmailAsync(EmailDto email)
    {
        if (email == null) return;
        if (_emailRepository == null)
        {
            NotificationService.Instance.ShowError("Email repository is not available.");
            return;
        }

        var entity = await _emailRepository.GetByIdAsync(email.Id);
        if (entity == null)
        {
            NotificationService.Instance.ShowError("Email not found.");
            return;
        }

        entity.MoveToFolder(EmailFolder.Archive);
        await _emailRepository.UpdateAsync(entity);

        NotificationService.Instance.ShowSuccess($"Archived: {email.Subject}");
        await LoadEmailsAsync();
    }

    private async Task DeleteEmailAsync(EmailDto email)
    {
        if (email == null) return;
        if (_emailRepository == null)
        {
            NotificationService.Instance.ShowError("Email repository is not available.");
            return;
        }

        var entity = await _emailRepository.GetByIdAsync(email.Id);
        if (entity == null)
        {
            NotificationService.Instance.ShowError("Email not found.");
            return;
        }

        entity.MoveToFolder(EmailFolder.Trash);
        await _emailRepository.UpdateAsync(entity);

        NotificationService.Instance.ShowInfo($"Deleted: {email.Subject}");
        await LoadEmailsAsync();
    }

    private async Task AddGmailAccountAsync()
    {
        try
        {
            IsLoading = true;
            StatusText = "Connecting to Gmail...";
            
            if (_gmailOAuthService == null)
            {
                NotificationService.Instance.ShowError("Gmail OAuth service is not available.");
                return;
            }

            // Start OAuth flow
            var authResult = await _gmailOAuthService.AuthenticateGmailAsync();
            
            if (!authResult.Success)
            {
                NotificationService.Instance.ShowError(
                    $"Gmail authentication failed: {authResult.ErrorMessage}");
                return;
            }

            // Save account to database
            var userId = UserSessionService.Instance.CurrentSession?.UserId;
            if (userId == null)
            {
                NotificationService.Instance.ShowError("User not logged in");
                return;
            }

            var command = new AddEmailAccountCommand
            {
                UserId = Guid.Parse(userId),
                EmailAddress = authResult.EmailAddress!,
                AccessToken = authResult.AccessToken!,
                RefreshToken = authResult.RefreshToken,
                ExpiresAt = authResult.ExpiresAt,
                Provider = "Gmail"
            };

            if (_mediator == null)
            {
                NotificationService.Instance.ShowError("Mediator service is not available.");
                return;
            }

            var result = await _mediator.Send(command);
            
            if (!result.IsError)
            {
                NotificationService.Instance.ShowSuccess(
                    $"Connected Gmail account: {authResult.EmailAddress}");
                
                // Trigger initial sync
                StatusText = "Performing initial sync...";
                await SyncEmailsAsync();
            }
            else
            {
                NotificationService.Instance.ShowError(
                    $"Failed to save account: {result.FirstError.Description}");
            }
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Add Gmail Account");
            NotificationService.Instance.ShowError("Failed to connect Gmail account");
        }
        finally
        {
            IsLoading = false;
            StatusText = "Ready";
        }
    }

    private async Task AddOutlookAccountAsync()
    {
        try
        {
            IsLoading = true;
            StatusText = "Connecting to Outlook...";
            
            if (_outlookOAuthService == null)
            {
                NotificationService.Instance.ShowError("Outlook OAuth service is not available.");
                return;
            }

            // Start OAuth flow
            var authResult = await _outlookOAuthService.AuthenticateOutlookAsync();
            
            if (!authResult.Success)
            {
                NotificationService.Instance.ShowError(
                    $"Outlook authentication failed: {authResult.ErrorMessage}");
                return;
            }

            // Save account to database
            var userId = UserSessionService.Instance.CurrentSession?.UserId;
            if (userId == null)
            {
                NotificationService.Instance.ShowError("User not logged in");
                return;
            }

            var command = new AddEmailAccountCommand
            {
                UserId = Guid.Parse(userId),
                EmailAddress = authResult.EmailAddress!,
                AccessToken = authResult.AccessToken!,
                RefreshToken = authResult.RefreshToken,
                ExpiresAt = authResult.ExpiresAt,
                Provider = "Outlook"
            };

            if (_mediator == null)
            {
                NotificationService.Instance.ShowError("Mediator service is not available.");
                return;
            }

            var result = await _mediator.Send(command);
            
            if (!result.IsError)
            {
                NotificationService.Instance.ShowSuccess(
                    $"Connected Outlook account: {authResult.EmailAddress}");
                
                // Trigger initial sync
                StatusText = "Performing initial sync...";
                await SyncEmailsAsync();
            }
            else
            {
                NotificationService.Instance.ShowError(
                    $"Failed to save account: {result.FirstError.Description}");
            }
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Add Outlook Account");
            NotificationService.Instance.ShowError("Failed to connect Outlook account");
        }
        finally
        {
            IsLoading = false;
            StatusText = "Ready";
        }
    }

    private async Task AddDirectEmailAccountAsync()
    {
        try
        {
            var dialog = new AddEmailAccountDialog();
            
            // CRITICAL: Set owner window properly
            var mainWindow = GetMainWindow();
            
            if (mainWindow == null)
            {
                NotificationService.Instance.ShowError("Cannot show dialog: Main window is null");
                return;
            }
            
            var result = await dialog.ShowDialog<EmailAccountSettings?>(mainWindow);
            
            if (result == null) return; // User cancelled
            
            IsLoading = true;
            StatusText = "Adding email account...";
            
            var userId = UserSessionService.Instance.CurrentSession?.UserId;
            if (userId == null)
            {
                NotificationService.Instance.ShowError("User not logged in");
                return;
            }
            
            // Validate inputs
            if (string.IsNullOrWhiteSpace(result.EmailAddress))
            {
                NotificationService.Instance.ShowError("Email address is required");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(result.Password))
            {
                NotificationService.Instance.ShowError("Password is required");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(result.ImapServer))
            {
                NotificationService.Instance.ShowError("IMAP server is required");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(result.SmtpServer))
            {
                NotificationService.Instance.ShowError("SMTP server is required");
                return;
            }
            
            // Additional validation for port numbers
            if (result.ImapPort < 1 || result.ImapPort > 65535)
            {
                NotificationService.Instance.ShowError("Invalid IMAP port (must be 1-65535)");
                return;
            }
            
            if (result.SmtpPort < 1 || result.SmtpPort > 65535)
            {
                NotificationService.Instance.ShowError("Invalid SMTP port (must be 1-65535)");
                return;
            }
            
            // Save account to database
            var command = new AddEmailAccountCommand
            {
                UserId = Guid.Parse(userId),
                EmailAddress = result.EmailAddress.Trim(),
                AccountName = !string.IsNullOrWhiteSpace(result.AccountName) ? result.AccountName.Trim() : result.EmailAddress.Trim(),
                ImapServer = result.ImapServer.Trim(),
                ImapPort = result.ImapPort,
                SmtpServer = result.SmtpServer.Trim(),
                SmtpPort = result.SmtpPort,
                UseSsl = result.UseSsl,
                Password = result.Password,
                Provider = "IMAP"
            };
            
            if (_mediator == null)
            {
                NotificationService.Instance.ShowError("Mediator service is not available.");
                return;
            }
            
            var saveResult = await _mediator.Send(command);
            
            if (!saveResult.IsError)
            {
                NotificationService.Instance.ShowSuccess(
                    $"Added email account: {result.EmailAddress}");
                
                // Trigger initial sync
                StatusText = "Performing initial sync...";
                await SyncEmailsAsync();
            }
            else
            {
                NotificationService.Instance.ShowError(
                    $"Failed to add account: {saveResult.FirstError.Description}");
            }
        }
        catch (FormatException ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Add Email Account - Format Error");
            NotificationService.Instance.ShowError($"Invalid input format: {ex.Message}. Please check all fields.");
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Add Email Account");
            NotificationService.Instance.ShowError($"Failed to add email account: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            StatusText = "Ready";
        }
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

    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var active = desktop.Windows.FirstOrDefault(window => window.IsActive);
            return active ?? desktop.MainWindow;
        }

        return null;
    }

    private async Task ComposeEmailAsync()
    {
        try
        {
            var viewModel = new ComposeEmailViewModel();
            var dialog = new ComposeEmailDialog(viewModel);
            
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;
            
            var result = await dialog.ShowDialog<bool?>(mainWindow);
            if (result == true)
            {
                NotificationService.Instance.ShowSuccess("Email sent successfully!");
            }
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Compose Email");
        }
    }

    private async Task ReplyToEmailAsync(EmailDto email)
    {
        if (email == null) return;
        
        try
        {
            var viewModel = new ComposeEmailViewModel
            {
                ReplyToEmailId = email.Id,
                Mode = "reply"
            };
            
            var dialog = new ComposeEmailDialog(viewModel);
            
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;
            
            var result = await dialog.ShowDialog<bool?>(mainWindow);
            if (result == true)
            {
                NotificationService.Instance.ShowSuccess("Reply sent successfully!");
            }
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Reply to Email");
        }
    }

    private async Task ForwardEmailAsync(EmailDto email)
    {
        if (email == null) return;
        
        try
        {
            var viewModel = new ComposeEmailViewModel
            {
                ForwardEmailId = email.Id,
                Mode = "forward"
            };
            
            var dialog = new ComposeEmailDialog(viewModel);
            
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;
            
            var result = await dialog.ShowDialog<bool?>(mainWindow);
            if (result == true)
            {
                NotificationService.Instance.ShowSuccess("Email forwarded successfully!");
            }
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Forward Email");
        }
    }

    #endregion
}

#region Supporting Types

public record FolderOption(string Name, EmailFolder Folder, string Icon);
public record CategoryOption(string Name, EmailCategory? Category);
public record PriorityOption(string Name, EmailPriority? Priority);

#endregion

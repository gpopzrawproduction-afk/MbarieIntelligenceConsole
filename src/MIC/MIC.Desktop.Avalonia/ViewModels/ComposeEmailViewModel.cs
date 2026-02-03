using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using EmailAttachmentDto = MIC.Core.Application.Common.Interfaces.EmailAttachment;
using MIC.Desktop.Avalonia.Services;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// ViewModel for composing, replying to, and forwarding emails.
/// </summary>
public class ComposeEmailViewModel : ViewModelBase
{
    private readonly IEmailSenderService? _emailSenderService;
    private readonly IEmailAccountRepository? _emailAccountRepository;
    private readonly IEmailRepository? _emailRepository;
    
    private string _to = string.Empty;
    private string _cc = string.Empty;
    private string _bcc = string.Empty;
    private string _subject = string.Empty;
    private string _body = string.Empty;
    private bool _isHtml;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private EmailAccount? _selectedAccount;
    private Guid? _replyToEmailId;
    private Guid? _forwardEmailId;
    private bool _replyToAll;
    private string _additionalMessage = string.Empty;
    private string _mode = "compose"; // compose, reply, forward

    public ComposeEmailViewModel()
    {
        _emailSenderService = Program.ServiceProvider?.GetService<IEmailSenderService>();
        _emailAccountRepository = Program.ServiceProvider?.GetService<IEmailAccountRepository>();
        _emailRepository = Program.ServiceProvider?.GetService<IEmailRepository>();
        
        // Initialize email accounts
        _ = LoadEmailAccountsAsync();
        
        // Commands
        var canSend = this.WhenAnyValue(
            x => x.To,
            x => x.Subject,
            x => x.Body,
            x => x.SelectedAccount,
            x => x.IsLoading,
            (to, subject, body, account, loading) => 
                !string.IsNullOrWhiteSpace(to) && 
                !string.IsNullOrWhiteSpace(subject) && 
                !string.IsNullOrWhiteSpace(body) && 
                account != null && 
                !loading);
        
        SendCommand = ReactiveCommand.CreateFromTask(SendEmailAsync, canSend);
        CancelCommand = ReactiveCommand.Create(() => OnCancel?.Invoke());
        AddAttachmentCommand = ReactiveCommand.CreateFromTask(AddAttachmentAsync);
        RemoveAttachmentCommand = ReactiveCommand.Create<EmailAttachmentDto>(RemoveAttachment);
        
        // Auto-load email for reply/forward
        this.WhenAnyValue(x => x.ReplyToEmailId)
            .Where(id => id.HasValue)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(id => Observable.FromAsync(() => LoadEmailForReplyAsync(id!.Value)))
            .Subscribe();
        
        this.WhenAnyValue(x => x.ForwardEmailId)
            .Where(id => id.HasValue)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(id => Observable.FromAsync(() => LoadEmailForForwardAsync(id!.Value)))
            .Subscribe();
    }

    #region Properties

    public string To
    {
        get => _to;
        set => this.RaiseAndSetIfChanged(ref _to, value);
    }

    public string Cc
    {
        get => _cc;
        set => this.RaiseAndSetIfChanged(ref _cc, value);
    }

    public string Bcc
    {
        get => _bcc;
        set => this.RaiseAndSetIfChanged(ref _bcc, value);
    }

    public string Subject
    {
        get => _subject;
        set => this.RaiseAndSetIfChanged(ref _subject, value);
    }

    public string Body
    {
        get => _body;
        set => this.RaiseAndSetIfChanged(ref _body, value);
    }

    public bool IsHtml
    {
        get => _isHtml;
        set => this.RaiseAndSetIfChanged(ref _isHtml, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public EmailAccount? SelectedAccount
    {
        get => _selectedAccount;
        set => this.RaiseAndSetIfChanged(ref _selectedAccount, value);
    }

    public Guid? ReplyToEmailId
    {
        get => _replyToEmailId;
        set => this.RaiseAndSetIfChanged(ref _replyToEmailId, value);
    }

    public Guid? ForwardEmailId
    {
        get => _forwardEmailId;
        set => this.RaiseAndSetIfChanged(ref _forwardEmailId, value);
    }

    public bool ReplyToAll
    {
        get => _replyToAll;
        set => this.RaiseAndSetIfChanged(ref _replyToAll, value);
    }

    public string AdditionalMessage
    {
        get => _additionalMessage;
        set => this.RaiseAndSetIfChanged(ref _additionalMessage, value);
    }

    public string Mode
    {
        get => _mode;
        set => this.RaiseAndSetIfChanged(ref _mode, value);
    }

    public ObservableCollection<EmailAccount> EmailAccounts { get; } = new();
    public ObservableCollection<EmailAttachmentDto> Attachments { get; } = new();

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> SendCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> AddAttachmentCommand { get; }
    public ReactiveCommand<EmailAttachmentDto, Unit> RemoveAttachmentCommand { get; }

    public event Action? OnSent;
    public event Action? OnCancel;

    #endregion

    #region Methods

    private async Task LoadEmailAccountsAsync()
    {
        if (_emailAccountRepository == null) return;

        try
        {
            var userId = UserSessionService.Instance.CurrentSession?.UserId;
            if (userId == null) return;

            var accounts = await _emailAccountRepository.GetByUserIdAsync(Guid.Parse(userId));
            
            EmailAccounts.Clear();
            foreach (var account in accounts)
            {
                if (account.IsActive)
                    EmailAccounts.Add(account);
            }

            // Select the first active account
            SelectedAccount = EmailAccounts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Load Email Accounts");
        }
    }

    private async Task LoadEmailForReplyAsync(Guid emailId)
    {
        if (_emailRepository == null) return;

        try
        {
            var email = await _emailRepository.GetByIdAsync(emailId);
            if (email == null) return;

            Mode = "reply";
            Subject = email.Subject.StartsWith("Re: ", StringComparison.OrdinalIgnoreCase)
                ? email.Subject
                : $"Re: {email.Subject}";
            
            To = ReplyToAll ? GetAllRecipients(email) : email.FromAddress;
            
            Body = $"\n\n--- Original Message ---\n" +
                  $"From: {email.FromName} <{email.FromAddress}>\n" +
                  $"Date: {email.SentDate:yyyy-MM-dd HH:mm}\n" +
                  $"Subject: {email.Subject}\n\n" +
                  $"{email.BodyText}";
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Load Email for Reply");
        }
    }

    private async Task LoadEmailForForwardAsync(Guid emailId)
    {
        if (_emailRepository == null) return;

        try
        {
            var email = await _emailRepository.GetByIdAsync(emailId);
            if (email == null) return;

            Mode = "forward";
            Subject = email.Subject.StartsWith("Fwd: ", StringComparison.OrdinalIgnoreCase) ||
                      email.Subject.StartsWith("FW: ", StringComparison.OrdinalIgnoreCase)
                ? email.Subject
                : $"Fwd: {email.Subject}";
            
            Body = $"{AdditionalMessage}\n\n--- Forwarded Message ---\n" +
                  $"From: {email.FromName} <{email.FromAddress}>\n" +
                  $"Date: {email.SentDate:yyyy-MM-dd HH:mm}\n" +
                  $"Subject: {email.Subject}\n\n" +
                  $"{email.BodyText}";
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Load Email for Forward");
        }
    }

    private string GetAllRecipients(EmailMessage email)
    {
        var recipients = new System.Collections.Generic.List<string> { email.FromAddress };
        
        if (!string.IsNullOrEmpty(email.ToRecipients))
            recipients.AddRange(email.ToRecipients.Split(';', ',').Select(r => r.Trim()));
        
        if (!string.IsNullOrEmpty(email.CcRecipients))
            recipients.AddRange(email.CcRecipients.Split(';', ',').Select(r => r.Trim()));
        
        // Remove duplicates and the current user's email
        var currentEmail = SelectedAccount?.EmailAddress;
        return string.Join(",", recipients.Distinct().Where(r => r != currentEmail));
    }

    private async Task SendEmailAsync()
    {
        if (_emailSenderService == null || SelectedAccount == null)
        {
            ErrorMessage = "Email service or account not available";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            EmailSendResult result;
            
            if (Mode == "reply" && ReplyToEmailId.HasValue)
            {
                result = await _emailSenderService.ReplyToEmailAsync(
                    SelectedAccount.Id,
                    ReplyToEmailId.Value,
                    Body,
                    ReplyToAll,
                    IsHtml);
            }
            else if (Mode == "forward" && ForwardEmailId.HasValue)
            {
                result = await _emailSenderService.ForwardEmailAsync(
                    SelectedAccount.Id,
                    ForwardEmailId.Value,
                    To,
                    Cc,
                    Bcc,
                    AdditionalMessage,
                    IsHtml);
            }
            else
            {
                // Regular email send
                if (Attachments.Any())
                {
                    result = await _emailSenderService.SendEmailWithAttachmentsAsync(
                        SelectedAccount.Id,
                        To,
                        Subject,
                        Body,
                        Attachments,
                        Cc,
                        Bcc,
                        IsHtml);
                }
                else
                {
                    result = await _emailSenderService.SendEmailAsync(
                        SelectedAccount.Id,
                        To,
                        Subject,
                        Body,
                        Cc,
                        Bcc,
                        IsHtml);
                }
            }

            if (result.Success)
            {
                NotificationService.Instance.ShowSuccess("Email sent successfully!");
                OnSent?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to send email";
                NotificationService.Instance.ShowError($"Failed to send email: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ErrorHandlingService.Instance.HandleException(ex, "Send Email");
            NotificationService.Instance.ShowError($"Failed to send email: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddAttachmentAsync()
    {
        try
        {
            var mainWindow = global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow == null) return;

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(
                new global::Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Select Attachment",
                    AllowMultiple = true
                });

            if (files == null || files.Count == 0) return;

            foreach (var file in files)
            {
                var filePath = file.Path.LocalPath;
                var fileName = Path.GetFileName(filePath);
                await using var stream = await file.OpenReadAsync();
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);
                var content = memory.ToArray();
                var contentType = GetContentType(filePath);

                Attachments.Add(new EmailAttachmentDto
                {
                    FileName = fileName,
                    Content = content,
                    ContentType = contentType
                });
            }

            NotificationService.Instance.ShowInfo($"Added {files.Count} attachment(s)");
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Add Attachment");
        }
    }

    private void RemoveAttachment(EmailAttachmentDto attachment)
    {
        if (attachment == null) return;
        Attachments.Remove(attachment);
    }

    private string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".doc" or ".docx" => "application/msword",
            ".xls" or ".xlsx" => "application/vnd.ms-excel",
            ".ppt" or ".pptx" => "application/vnd.ms-powerpoint",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    #endregion
}
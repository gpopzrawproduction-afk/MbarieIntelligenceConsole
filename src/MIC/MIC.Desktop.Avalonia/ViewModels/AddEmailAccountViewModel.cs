using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Identity.Services;
using MIC.Infrastructure.Data.Services;

namespace MIC.Desktop.Avalonia.ViewModels;

public class AddEmailAccountViewModel : ViewModelBase
{
    private string _emailAddress;
    private string _displayName;
    private EmailProvider _selectedProvider;
    private string _statusMessage;
    private bool _hasStatusMessage;
    private bool _canAuthorize;

    public ObservableCollection<EmailProvider> Providers { get; }
    public ReactiveCommand<Unit, Unit> AuthorizeCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public AddEmailAccountViewModel()
    {
        _emailAddress = string.Empty;
        _displayName = string.Empty;
        _selectedProvider = EmailProvider.Gmail;
        _statusMessage = string.Empty;
        _hasStatusMessage = false;
        _canAuthorize = false;

        Providers = new ObservableCollection<EmailProvider>
        {
            EmailProvider.Gmail,
            EmailProvider.Outlook
        };

        AuthorizeCommand = ReactiveCommand.CreateFromTask(AuthorizeAsync);
        CancelCommand = ReactiveCommand.Create(Cancel);
        
        this.WhenAnyValue(x => x.EmailAddress)
            .Subscribe(email => ValidateInput());
    }

    public string EmailAddress
    {
        get => _emailAddress;
        set => this.RaiseAndSetIfChanged(ref _emailAddress, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => this.RaiseAndSetIfChanged(ref _displayName, value);
    }

    public EmailProvider SelectedProvider
    {
        get => _selectedProvider;
        set => this.RaiseAndSetIfChanged(ref _selectedProvider, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool HasStatusMessage
    {
        get => _hasStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _hasStatusMessage, value);
    }

    public bool CanAuthorize
    {
        get => _canAuthorize;
        set => this.RaiseAndSetIfChanged(ref _canAuthorize, value);
    }

    private void ValidateInput()
    {
        CanAuthorize = !string.IsNullOrWhiteSpace(EmailAddress) && 
                      !string.IsNullOrWhiteSpace(DisplayName) &&
                      IsValidEmail(EmailAddress);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task AuthorizeAsync()
    {
        try
        {
            StatusMessage = "Starting authorization process...";
            HasStatusMessage = true;

            // Determine which OAuth2 service to use based on provider
            if (_selectedProvider == EmailProvider.Gmail)
            {
                // This would be injected in the actual implementation
                // For now, we'll simulate the process
                StatusMessage = "Opening Gmail authorization window...";
                
                // In a real implementation, you would call the OAuth2 service here
                // await _emailOAuth2Service.AuthorizeGmailAccountAsync();
            }
            else if (_selectedProvider == EmailProvider.Outlook)
            {
                StatusMessage = "Opening Outlook authorization window...";
                
                // In a real implementation, you would call the OAuth2 service here
                // await _emailOAuth2Service.AuthorizeOutlookAccountAsync();
            }

            StatusMessage = "Authorization completed successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Authorization failed: {ex.Message}";
        }
    }

    private void Cancel()
    {
        // Close the window - this would typically be handled by the view
        StatusMessage = "Operation cancelled.";
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MIC.Desktop.Avalonia.Views.Dialogs;

public partial class AddEmailAccountDialog : Window
{
    public EmailAccountSettings? Result { get; private set; }

    // Control references - found via FindControl
    private TextBox? _emailAddressTextBox;
    private TextBox? _passwordTextBox;
    private TextBox? _accountNameTextBox;
    private TextBox? _imapServerTextBox;
    private TextBox? _imapPortTextBox;
    private TextBox? _smtpServerTextBox;
    private TextBox? _smtpPortTextBox;
    private CheckBox? _useSslCheckBox;
    private ProgressBar? _progressBar;
    private TextBlock? _statusMessage;
    private TextBlock? _errorMessage;

    public AddEmailAccountDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        // Safely find all controls after XAML is loaded
        _emailAddressTextBox = this.FindControl<TextBox>("EmailAddressTextBox");
        _passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
        _accountNameTextBox = this.FindControl<TextBox>("AccountNameTextBox");
        _imapServerTextBox = this.FindControl<TextBox>("ImapServerTextBox");
        _imapPortTextBox = this.FindControl<TextBox>("ImapPortTextBox");
        _smtpServerTextBox = this.FindControl<TextBox>("SmtpServerTextBox");
        _smtpPortTextBox = this.FindControl<TextBox>("SmtpPortTextBox");
        _useSslCheckBox = this.FindControl<CheckBox>("UseSslCheckBox");
        _progressBar = this.FindControl<ProgressBar>("ProgressBar");
        _statusMessage = this.FindControl<TextBlock>("StatusMessage");
        _errorMessage = this.FindControl<TextBlock>("ErrorMessage");
    }

    private async void OnAutoDetectClick(object? sender, RoutedEventArgs e)
    {
        var email = _emailAddressTextBox?.Text?.Trim();

        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            ShowError("Please enter a valid email address");
            return;
        }

        try
        {
            SetProgress(true, "Auto-detecting server settings...");

            var domain = email.Split('@')[1];

            var altPatterns = new[]
            {
                ($"imap.{domain}", $"smtp.{domain}"),
                ($"mail.{domain}", $"mail.{domain}"),
                (domain, domain)
            };

            foreach (var (imap, smtp) in altPatterns)
            {
                if (_imapServerTextBox != null) _imapServerTextBox.Text = imap;
                if (_smtpServerTextBox != null) _smtpServerTextBox.Text = smtp;

                SetStatus($"Trying {imap}...");
                await Task.Delay(500);
            }

            SetStatus("Settings detected. Click 'Test Connection' to verify.");
        }
        catch (Exception ex)
        {
            ShowError($"Auto-detection failed: {ex.Message}");
        }
        finally
        {
            SetProgress(false);
        }
    }

    private async void OnTestConnectionClick(object? sender, RoutedEventArgs e)
    {
        if (!ValidateInputs())
            return;

        try
        {
            SetProgress(true, "Preparing connection test...");

            var email = _emailAddressTextBox!.Text!.Trim();
            var password = _passwordTextBox!.Text!;
            var imapServer = _imapServerTextBox!.Text!.Trim();
            var smtpServer = _smtpServerTextBox!.Text!.Trim();
            var imapPort = int.Parse(_imapPortTextBox?.Text ?? "993");
            var smtpPort = int.Parse(_smtpPortTextBox?.Text ?? "465");
            var useSsl = _useSslCheckBox?.IsChecked ?? true;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Test IMAP
            SetStatus("Testing IMAP connection...");
            using (var imapClient = new MailKit.Net.Imap.ImapClient())
            {
                imapClient.Timeout = 15000;
                await imapClient.ConnectAsync(
                    imapServer, imapPort,
                    useSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect
                           : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable,
                    cts.Token);
                await imapClient.AuthenticateAsync(email, password, cts.Token);
                await imapClient.DisconnectAsync(true, cts.Token);
            }

            // Test SMTP
            SetStatus("Testing SMTP connection...");
            using (var smtpClient = new MailKit.Net.Smtp.SmtpClient())
            {
                smtpClient.Timeout = 15000;
                await smtpClient.ConnectAsync(
                    smtpServer, smtpPort,
                    useSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect
                           : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable,
                    cts.Token);
                await smtpClient.AuthenticateAsync(email, password, cts.Token);
                await smtpClient.DisconnectAsync(true, cts.Token);
            }

            SetStatus("✓ Connection successful! Click 'Add Account' to save.");
            if (_statusMessage != null)
                _statusMessage.Foreground = new SolidColorBrush(Color.Parse("#4CAF50"));
        }
        catch (OperationCanceledException)
        {
            ShowError("Connection timed out. Please check server settings.");
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            ShowError($"Authentication failed: {ex.Message}\n\nIf using Gmail, you need an App Password.");
        }
        catch (Exception ex)
        {
            ShowError($"Connection failed: {ex.Message}");
        }
        finally
        {
            SetProgress(false);
        }
    }

    private void OnAddAccountClick(object? sender, RoutedEventArgs e)
    {
        if (!ValidateInputs())
            return;

        try
        {
            Result = new EmailAccountSettings
            {
                EmailAddress = _emailAddressTextBox!.Text!.Trim(),
                Password = _passwordTextBox!.Text!,
                AccountName = string.IsNullOrWhiteSpace(_accountNameTextBox?.Text)
                    ? _emailAddressTextBox!.Text!.Trim()
                    : _accountNameTextBox.Text.Trim(),
                ImapServer = _imapServerTextBox!.Text!.Trim(),
                ImapPort = int.Parse(_imapPortTextBox?.Text ?? "993"),
                SmtpServer = _smtpServerTextBox!.Text!.Trim(),
                SmtpPort = int.Parse(_smtpPortTextBox?.Text ?? "465"),
                UseSsl = _useSslCheckBox?.IsChecked ?? true
            };

            Close(Result);
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(_emailAddressTextBox?.Text) ||
            !_emailAddressTextBox.Text.Contains("@"))
        {
            ShowError("Please enter a valid email address");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_passwordTextBox?.Text))
        {
            ShowError("Password is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_imapServerTextBox?.Text))
        {
            ShowError("IMAP server is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_smtpServerTextBox?.Text))
        {
            ShowError("SMTP server is required");
            return false;
        }

        if (!int.TryParse(_imapPortTextBox?.Text, out var imapPort) ||
            imapPort < 1 || imapPort > 65535)
        {
            ShowError("Invalid IMAP port");
            return false;
        }

        if (!int.TryParse(_smtpPortTextBox?.Text, out var smtpPort) ||
            smtpPort < 1 || smtpPort > 65535)
        {
            ShowError("Invalid SMTP port");
            return false;
        }

        return true;
    }

    private void SetProgress(bool isVisible, string? message = null)
    {
        if (_progressBar != null) _progressBar.IsVisible = isVisible;
        if (_errorMessage != null) _errorMessage.IsVisible = false;
        if (message != null) SetStatus(message);
    }

    private void SetStatus(string message)
    {
        if (_statusMessage != null)
        {
            _statusMessage.Text = message;
            _statusMessage.IsVisible = true;
            _statusMessage.Foreground = new SolidColorBrush(Colors.White);
        }
    }

    private void ShowError(string message)
    {
        if (_errorMessage != null)
        {
            _errorMessage.Text = message;
            _errorMessage.IsVisible = true;
        }
        if (_statusMessage != null) _statusMessage.IsVisible = false;
        if (_progressBar != null) _progressBar.IsVisible = false;
    }
}

public record EmailAccountSettings
{
    public string EmailAddress { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string ImapServer { get; init; } = string.Empty;
    public int ImapPort { get; init; }
    public string SmtpServer { get; init; } = string.Empty;
    public int SmtpPort { get; init; }
    public bool UseSsl { get; init; }
}
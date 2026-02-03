# Command Wiring Implementation Guide

## Overview
This guide provides detailed implementation instructions for wiring every button/command in the Mbarie Intelligence Console application to real actions. The core infrastructure is solid with MediatR CQRS pattern, ViewModels, and services already in place. This document outlines the specific changes needed to connect UI commands to database/API operations.

## Current State Analysis

### ✅ Already Implemented:
1. **Alert Commands**: Most MediatR handlers exist (CreateAlert, DeleteAlert, UpdateAlert, GetAllAlerts)
2. **Email Queries**: GetEmailsQueryHandler exists
3. **Chat Services**: IChatService and ChatService implemented
4. **Dashboard**: RefreshCommand partially implemented with mock data
5. **AlertListViewModel**: Good implementation with proper loading states

### ⚠️ Needs Enhancement:
1. **Dashboard Commands**: Navigation commands need real implementation
2. **Email Commands**: Missing SyncCommand, ComposeCommand handlers
3. **Predictions**: Missing actual prediction service integration
4. **Metrics**: Missing metrics calculation services
5. **Settings**: Missing persistence and validation
6. **Error Handling**: Needs consistent error handling patterns
7. **Loading States**: Some ViewModels missing proper IsBusy patterns

## Implementation Plan

### 1. DASHBOARD COMMANDS

#### Current State (DashboardViewModel.cs):
- `RefreshCommand`: Partially implemented - loads alerts/metrics but uses mock email/prediction data
- `CheckInboxCommand`, `ViewUrgentItemsCommand`, `AIChatCommand`: Console.WriteLine placeholder only

#### Required Changes:

**1.1 RefreshCommand Enhancement:**
```csharp
private async Task LoadDashboardDataAsync()
{
    try
    {
        IsLoading = true;
        
        // Load real email data (replace LoadMockEmailDataAsync)
        await LoadRealEmailDataAsync();
        
        // Load real predictions (replace LoadMockPredictionsAsync)
        await LoadRealPredictionsAsync();
        
        // Update with real AI insights
        await GenerateRealAIInsightsAsync();
    }
    catch (Exception ex)
    {
        // Use proper error handling service
        ErrorHandlingService.Instance.HandleException(ex, "Load Dashboard Data");
        NotificationService.Instance.ShowError("Failed to load dashboard data");
    }
    finally
    {
        IsLoading = false;
    }
}

private async Task LoadRealEmailDataAsync()
{
    var userId = UserSessionService.Instance.CurrentSession?.UserId;
    if (userId == null) return;
    
    var query = new GetEmailSummaryQuery 
    { 
        UserId = Guid.Parse(userId),
        TimeRange = TimeRange.Today 
    };
    var result = await _mediator.Send(query);
    
    if (!result.IsError)
    {
        TotalEmails = result.Value.TotalCount;
        UnreadCount = result.Value.UnreadCount;
        HighPriorityCount = result.Value.HighPriorityCount;
        RequiresResponseCount = result.Value.RequiresResponseCount;
        
        // Update RecentEmails collection
        UpdateRecentEmails(result.Value.RecentEmails);
    }
}
```

**1.2 Navigation Commands Implementation:**
```csharp
private readonly INavigationService _navigationService;

public DashboardViewModel(IMediator mediator, INavigationService navigationService)
{
    _navigationService = navigationService;
    // ... existing constructor code
}

private void CheckInbox()
{
    _navigationService.NavigateTo("Email");
    
    // Trigger email sync if not synced recently
    var lastSync = SettingsService.Instance.GetLastEmailSync();
    if (DateTime.UtcNow - lastSync > TimeSpan.FromHours(1))
    {
        Task.Run(async () => await TriggerEmailSyncAsync());
    }
}

private async Task TriggerEmailSyncAsync()
{
    try
    {
        var syncService = Program.ServiceProvider.GetRequiredService<IEmailSyncService>();
        await syncService.SyncEmailsAsync(CancellationToken.None);
        NotificationService.Instance.ShowSuccess("Email sync completed");
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Email Sync");
    }
}

private void ViewUrgentItems()
{
    _navigationService.NavigateTo("Alerts");
    
    // Set filter to show only Critical/Urgent alerts
    var alertViewModel = _navigationService.GetCurrentViewModel<AlertListViewModel>();
    if (alertViewModel != null)
    {
        alertViewModel.SelectedSeverity = AlertSeverity.Critical;
        alertViewModel.SelectedStatus = AlertStatus.Active;
    }
}

private void AIChat()
{
    _navigationService.NavigateTo("AI Chat");
}
```

#### MediatR Handlers to Create:
1. **GetEmailSummaryQuery** + **GetEmailSummaryQueryHandler** - Returns email counts and recent emails
2. **GetPredictionSummaryQuery** + **GetPredictionSummaryQueryHandler** - Returns recent predictions

#### Service Method Implementations:
1. **EmailSummaryService** - Aggregates email statistics
2. **PredictionSummaryService** - Aggregates prediction data

### 2. ALERTS COMMANDS

#### Current State (AlertListViewModel.cs):
- ✅ **RefreshCommand**: Implemented with proper loading states
- ⚠️ **CreateAlertRequested**: Event-based, needs dialog integration
- ⚠️ **EditAlertCommand**: Event-based, needs dialog integration  
- ✅ **DeleteAlertCommand**: Implemented with confirmation
- ✅ **FilterCommand**: Implemented via property change observables

#### Required Changes:

**2.1 CreateAlertRequested Integration:**
```csharp
// In MainWindow.xaml.cs or appropriate dialog service
private async void OnCreateAlertRequested(object sender, EventArgs e)
{
    var dialog = new CreateAlertDialog();
    var result = await dialog.ShowDialog<AlertDto>(this);
    
    if (result != null)
    {
        var command = new CreateAlertCommand
        {
            AlertName = result.AlertName,
            Description = result.Description,
            Severity = result.Severity,
            Source = result.Source,
            CreatedBy = UserSessionService.Instance.CurrentUserName
        };
        
        try
        {
            IsBusy = true;
            var createResult = await _mediator.Send(command);
            
            if (!createResult.IsError)
            {
                NotificationService.Instance.ShowSuccess("Alert created successfully");
                await LoadAlertsAsync(); // Refresh list
            }
            else
            {
                NotificationService.Instance.ShowError($"Failed to create alert: {createResult.FirstError.Description}");
            }
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Create Alert");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

**2.2 EditAlertCommand Enhancement:**
```csharp
private async void EditAlert(AlertDto alert)
{
    var dialog = new EditAlertDialog(alert);
    var result = await dialog.ShowDialog<AlertDto>(this);
    
    if (result != null)
    {
        var command = new UpdateAlertCommand
        {
            AlertId = alert.Id,
            AlertName = result.AlertName,
            Description = result.Description,
            Severity = result.Severity,
            Status = result.Status,
            UpdatedBy = UserSessionService.Instance.CurrentUserName
        };
        
        try
        {
            IsBusy = true;
            var updateResult = await _mediator.Send(command);
            
            if (!updateResult.IsError)
            {
                NotificationService.Instance.ShowSuccess("Alert updated successfully");
                await LoadAlertsAsync(); // Refresh list
            }
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Update Alert");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

#### XAML Binding Verification Checklist:
- [ ] `CreateAlertCommand` bound to "New Alert" button
- [ ] `EditAlertCommand` bound to edit button with CommandParameter
- [ ] `DeleteAlertCommand` bound to delete button with CommandParameter  
- [ ] `RefreshCommand` bound to refresh button
- [ ] `IsLoading` bound to progress indicator visibility
- [ ] `SelectedAlert` two-way binding to list selection

### 3. EMAIL COMMANDS

#### Current State (EmailInboxViewModel.cs):
- ✅ **RefreshCommand**: Implemented with real GetEmailsQuery
- ⚠️ **AddAccountCommand**: Placeholder only
- ⚠️ **SyncCommand**: Missing implementation
- ⚠️ **ComposeCommand**: Missing implementation
- ⚠️ **MarkAsRead/ToggleFlag/Archive/Delete**: Notification placeholders only

#### Required Changes:

**3.1 AddAccountCommand Implementation:**
```csharp
public ReactiveCommand<Unit, Unit> AddAccountCommand { get; }

// In constructor:
AddAccountCommand = ReactiveCommand.CreateFromTask(AddEmailAccountAsync);

private async Task AddEmailAccountAsync()
{
    try
    {
        IsLoading = true;
        
        // Show OAuth flow window
        var oauthService = Program.ServiceProvider.GetRequiredService<IEmailOAuth2Service>();
        var authResult = await oauthService.AuthenticateAsync();
        
        if (authResult.Success)
        {
            // Save email account to database
            var command = new AddEmailAccountCommand
            {
                EmailAddress = authResult.EmailAddress,
                AccessToken = authResult.AccessToken,
                RefreshToken = authResult.RefreshToken,
                Provider = authResult.Provider,
                UserId = UserSessionService.Instance.CurrentSession.UserId
            };
            
            var saveResult = await _mediator.Send(command);
            
            if (!saveResult.IsError)
            {
                NotificationService.Instance.ShowSuccess($"Connected {authResult.EmailAddress}");
                
                // Trigger initial sync
                await SyncEmailsAsync();
            }
        }
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Add Email Account");
    }
    finally
    {
        IsLoading = false;
    }
}
```

**3.2 SyncCommand Implementation:**
```csharp
public ReactiveCommand<Unit, Unit> SyncCommand { get; }

// In constructor:
SyncCommand = ReactiveCommand.CreateFromTask(SyncEmailsAsync, 
    this.WhenAnyValue(x => x.IsLoading).Select(isLoading => !isLoading));

private async Task SyncEmailsAsync()
{
    try
    {
        IsLoading = true;
        StatusText = "Syncing emails...";
        
        var syncService = Program.ServiceProvider.GetRequiredService<IEmailSyncService>();
        var syncResult = await syncService.SyncAllAccountsAsync(CancellationToken.None);
        
        if (syncResult.Success)
        {
            NotificationService.Instance.ShowSuccess($"Synced {syncResult.NewEmails} new emails");
            
            // Refresh email list
            await LoadEmailsAsync();
        }
        else
        {
            NotificationService.Instance.ShowError($"Sync failed: {syncResult.ErrorMessage}");
        }
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Email Sync");
    }
    finally
    {
        IsLoading = false;
        StatusText = "Ready";
    }
}
```

**3.3 ComposeCommand Implementation:**
```csharp
public ReactiveCommand<Unit, Unit> ComposeCommand { get; }

// In constructor:
ComposeCommand = ReactiveCommand.Create(ComposeEmail);

private void ComposeEmail()
{
    var composeWindow = new ComposeEmailWindow();
    composeWindow.Show();
    
    // When sent, handle the email
    composeWindow.EmailSent += async (sender, email) =>
    {
        try
        {
            var sendCommand = new SendEmailCommand
            {
                To = email.To,
                Subject = email.Subject,
                Body = email.Body,
                Attachments = email.Attachments,
                SentBy = UserSessionService.Instance.CurrentUserName
            };
            
            var result = await _mediator.Send(sendCommand);
            
            if (!result.IsError)
            {
                NotificationService.Instance.ShowSuccess("Email sent successfully");
            }
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Send Email");
        }
    };
}
```

#### MediatR Handlers to Create:
1. **AddEmailAccountCommand** + **AddEmailAccountCommandHandler** - Saves OAuth tokens
2. **SendEmailCommand** + **SendEmailCommandHandler** - Sends emails via SMTP
3. **MarkEmailAsReadCommand** + **MarkEmailAsReadCommandHandler** - Updates email read status
4. **UpdateEmailCommand** + **UpdateEmailCommandHandler** - Updates email flags/folder

#### Service Method Implementations:
1. **EmailSyncService.SyncAllAccountsAsync** - Syncs all connected email accounts
2. **EmailOAuth2Service.AuthenticateAsync** - Handles OAuth flow

### 4. PREDICTIONS COMMANDS

#### Current State (PredictionsViewModel.cs):
- ⚠️ **GenerateCommand**: Mock implementation only
- ⚠️ **ExportCommand**: Placeholder only  
- ⚠️ **RefreshCommand**: Mock data only

#### Required Changes:

**4.1 GenerateCommand Implementation:**
```csharp
public ReactiveCommand<Unit, Unit> GenerateCommand { get; }

// In constructor:
GenerateCommand = ReactiveCommand.CreateFromTask(GeneratePredictionAsync,
    this.WhenAnyValue(x => x.IsLoading).Select(isLoading => !isLoading));

private async Task GeneratePredictionAsync()
{
    try
    {
        IsLoading = true;
        PredictionSummary = "Analyzing data and generating prediction...";
        
        var command = new GeneratePredictionCommand
        {
            Metric = SelectedMetric,
            ForecastDays = ForecastDays,
            ConfidenceLevel = ConfidenceLevel,
            UserId = UserSessionService.Instance.CurrentSession.UserId
        };
        
        var result = await _mediator.Send(command);
        
        if (!result.IsError)
        {
            var prediction = result.Value;
            
            // Update predictions collection
            UpdatePredictions(prediction);
            
            // Update chart data
            UpdateChartData(prediction);
            
            // Set summary
            PredictionSummary = GeneratePredictionSummary(prediction);
            
            NotificationService.Instance.ShowSuccess("Prediction generated successfully");
        }
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Generate Prediction");
        PredictionSummary = "Failed to generate prediction. Please try again.";
    }
    finally
    {
        IsLoading = false;
    }
}
```

**4.2 ExportCommand Implementation:**
```csharp
private async Task ExportPredictionsAsync()
{
    try
    {
        IsLoading = true;
        
        var exportService = Program.ServiceProvider.GetRequiredService<IPredictionExportService>();
        
        var options = new ExportOptions
        {
            Format = ExportFormat.Csv,
            IncludeHistoricalData = true,
            IncludeConfidenceIntervals = true
        };
        
        var filePath = await exportService.ExportAsync(Predictions.ToList(), ChartData.ToList(), options);
        
        if (!string.IsNullOrEmpty(filePath))
        {
            NotificationService.Instance.ShowSuccess($"Exported to {filePath}");
            
            // Open the exported file
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Export Predictions");
    }
    finally
    {
        IsLoading = false;
    }
}
```

#### MediatR Handlers to Create:
1. **GeneratePredictionCommand** + **GeneratePredictionCommandHandler** - Calls AI prediction service
2. **GetPredictionsQuery** + **GetPredictionsQueryHandler** - Retrieves saved predictions

#### Service Method Implementations:
1. **PredictionService.GenerateAsync** - Calls AI models for predictions
2. **PredictionExportService.ExportAsync** - Exports to CSV/PDF formats

### 5. METRICS COMMANDS

#### Current State (MetricsDashboardViewModel.cs - needs inspection):
Assuming similar pattern to other ViewModels.

#### Required Changes:

**5.1 RefreshCommand Implementation:**
```csharp
private async Task RefreshMetricsAsync()
{
    try
    {
        IsLoading = true;
        
        var query = new GetMetricsQuery
        {
            TimeRange = SelectedTimeRange,
            Category = SelectedCategory,
            IncludeCalculations = true
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.IsError)
        {
            UpdateMetricsDisplay(result.Value);
            
            // Recalculate derived metrics
            await RecalculateDerivedMetricsAsync();
        }
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Refresh Metrics");
    }
    finally
    {
        IsLoading = false;
    }
}
```

**5.2 ExportCommand Implementation:**
```csharp
private async Task ExportMetricsAsync()
{
    try
    {
        IsLoading = true;
        
        var exportService = Program.ServiceProvider.GetRequiredService<IMetricsExportService>();
        
        var report = new MetricsReport
        {
            Metrics = Metrics.ToList(),
            TimeRange = SelectedTimeRange,
            GeneratedBy = UserSessionService.Instance.CurrentUserName,
            GeneratedAt = DateTime.UtcNow
        };
        
        var filePath = await exportService.ExportReportAsync(report, ExportFormat.Pdf);
        
        if (!string.IsNullOrEmpty(filePath))
        {
            NotificationService.Instance.ShowSuccess($"Report exported to {filePath}");
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Export Metrics");
    }
    finally
    {
        IsLoading = false;
    }
}
```

#### MediatR Handlers to Create:
1. **GetMetricsQuery** + **GetMetricsQueryHandler** - Retrieves operational metrics
2. **CalculateMetricsCommand** + **CalculateMetricsCommandHandler** - Triggers metric calculations

#### Service Method Implementations:
1. **MetricsCalculationService.RecalculateAllAsync** - Recalculates all derived metrics
2. **MetricsExportService.ExportReportAsync** - Creates PDF/Excel reports

### 6. AI CHAT COMMANDS

#### Current State (ChatViewModel.cs):
- ✅ **SendMessageCommand**: Implemented with IChatService
- ✅ **ClearHistoryCommand**: Implemented
- ⚠️ **Enter key binding**: Needs implementation in XAML

#### Required Changes:

**6.1 SendMessageCommand Enhancement:**
```csharp
private async Task SendMessageAsync()
{
    // Existing code is good, but add:
    // 1. Message persistence to database
    // 2. Conversation context management
    // 3. Better error handling
    
    try
    {
        // Save user message to database
        await SaveChatMessageAsync(userMessage, true);
        
        // Get AI response
        var aiResponse = await ProcessUserQuery(userMessage);
        
        // Save AI response to database
        await SaveChatMessageAsync(aiResponse, false);
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Chat Message");
        
        // Fallback response
        Messages.Add(new ChatMessageViewModel
        {
            Content = "I'm having trouble processing your request. Please try again.",
            IsUser = false,
            Timestamp = DateTime.Now
        });
    }
}

private async Task SaveChatMessageAsync(string content, bool isUser)
{
    var command = new SaveChatMessageCommand
    {
        ConversationId = _conversationId,
        Content = content,
        IsUserMessage = isUser,
        UserId = UserSessionService.Instance.CurrentSession?.UserId,
        Timestamp = DateTime.UtcNow
    };
    
    await _mediator.Send(command);
}
```

**6.2 ClearHistoryCommand Enhancement:**
```csharp
private async Task ClearChatAsync()
{
    try
    {
        // Clear from database
        var command = new ClearChatHistoryCommand
        {
            ConversationId = _conversationId,
            UserId = UserSessionService.Instance.CurrentSession?.UserId
        };
        
        await _mediator.Send(command);
        
        // Clear UI
        Messages.Clear();
        AddWelcomeMessage();
        
        NotificationService.Instance.ShowInfo("Chat history cleared");
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Clear Chat");
    }
}
```

**6.3 Enter Key Binding:**
```xml
<!-- In ChatView.axaml -->
<TextBox Text="{Binding UserInput, UpdateSourceTrigger=PropertyChanged}"
         AcceptsReturn="False">
    <TextBox.InputBindings>
        <KeyBinding Command="{Binding SendMessageCommand}" 
                    Key="Enter"
                    Gesture="Enter"/>
    </TextBox.InputBindings>
</TextBox>
```

#### MediatR Handlers to Create:
1. **SaveChatMessageCommand** + **SaveChatMessageCommandHandler** - Saves messages to database
2. **ClearChatHistoryCommand** + **ClearChatHistoryCommandHandler** - Clears conversation history
3. **GetChatHistoryQuery** + **GetChatHistoryQueryHandler** - Retrieves chat history

### 7. SETTINGS COMMANDS

#### Required Implementation (SettingsViewModel.cs needs to be created/inspected):

**7.1 SaveCommand Implementation:**
```csharp
public ReactiveCommand<Unit, Unit> SaveCommand { get; }

// In constructor:
SaveCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync,
    this.WhenAnyValue(x => x.HasChanges).Select(hasChanges => hasChanges && !IsLoading));

private async Task SaveSettingsAsync()
{
    try
    {
        IsLoading = true;
        
        // Validate settings
        var validationResult = ValidateSettings();
        if (!validationResult.IsValid)
        {
            NotificationService.Instance.ShowError($"Validation failed: {validationResult.ErrorMessage}");
            return;
        }
        
        var command = new SaveSettingsCommand
        {
            Settings = CurrentSettings,
            UserId = UserSessionService.Instance.CurrentSession.UserId
        };
        
        var result = await _mediator.Send(command);
        
        if (!result.IsError)
        {
            HasChanges = false;
            NotificationService.Instance.ShowSuccess("Settings saved successfully");
        }
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Save Settings");
    }
    finally
    {
        IsLoading = false;
    }
}
```

**7.2 TestConnectionCommand Implementation:**
```csharp
private async Task TestConnectionAsync()
{
    try
    {
        IsLoading = true;
        
        var testService = Program.ServiceProvider.GetRequiredService<IConnectionTestService>();
        
        var tests = new List<ConnectionTest>
        {
            new() { Service = "Database", Test = () => testService.TestDatabaseConnection() },
            new() { Service = "Email API", Test = () => testService.TestEmailApiConnection() },
            new() { Service = "AI Service", Test = () => testService.TestAIServiceConnection() }
        };
        
        var results = new List<TestResult>();
        foreach (var test in tests)
        {
            var result = await test.Test();
            results.Add(result);
        }
        
        UpdateConnectionStatus(results);
    }
    catch (Exception ex)
    {
        ErrorHandlingService.Instance.HandleException(ex, "Test Connection");
    }
    finally
    {
        IsLoading = false;
    }
}
```

**7.3 ResetCommand Implementation:**
```csharp
private async Task ResetSettingsAsync()
{
    var dialog = new ConfirmationDialog
    {
        Title = "Reset Settings",
        Message = "Are you sure you want to reset all settings to defaults? This cannot be undone.",
        ConfirmText = "Reset",
        CancelText = "Cancel"
    };
    
    var result = await dialog.ShowDialog<bool>();
    
    if (result)
    {
        try
        {
            IsLoading = true;
            
            var command = new ResetSettingsCommand
            {
                UserId = UserSessionService.Instance.CurrentSession.UserId
            };
            
            await _mediator.Send(command);
            
            // Reload default settings
            await LoadSettingsAsync();
            
            NotificationService.Instance.ShowSuccess("Settings reset to defaults");
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Reset Settings");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

#### MediatR Handlers to Create:
1. **SaveSettingsCommand** + **SaveSettingsCommandHandler** - Persists settings
2. **ResetSettingsCommand** + **ResetSettingsCommandHandler** - Restores defaults
3. **GetSettingsQuery** + **GetSettingsQueryHandler** - Retrieves user settings

## Implementation Priority

### Phase 1 - Core Navigation & Basic Operations (Week 1)
1. Dashboard navigation commands
2. Alert CRUD operations completion
3. Email RefreshCommand enhancement

### Phase 2 - Data Integration (Week 2)
1. Real email data integration
2. Prediction service integration
3. Metrics calculation services

### Phase 3 - Advanced Features (Week 3)
1. Email OAuth and sync
2. Settings persistence
3. Export functionality

### Phase 4 - Polish & Testing (Week 4)
1. Error handling consistency
2. Loading state improvements
3. XAML binding verification

## Error Handling Patterns

### Standard Pattern for All Async Operations:
```csharp
try
{
    IsBusy = true;
    
    // Perform operation
    var result = await _mediator.Send(command);
    
    if (!result.IsError)
    {
        // Success handling
        NotificationService.Instance.ShowSuccess("Operation completed");
    }
    else
    {
        // MediatR error handling
        NotificationService.Instance.ShowError($"Failed: {result.FirstError.Description}");
    }
}
catch (Exception ex)
{
    // Unhandled exception
    ErrorHandlingService.Instance.HandleException(ex, "Operation Name");
    NotificationService.Instance.ShowError("An unexpected error occurred");
}
finally
{
    IsBusy = false;
}
```

### Validation Pattern:
```csharp
private ValidationResult ValidateInput()
{
    var errors = new List<string>();
    
    if (string.IsNullOrWhiteSpace(RequiredField))
        errors.Add("Required field is missing");
    
    if (NumericValue < 0)
        errors.Add("Value must be positive");
    
    return new ValidationResult
    {
        IsValid = errors.Count == 0,
        ErrorMessage = string.Join(", ", errors)
    };
}
```

## Loading State Pattern

### ViewModel Implementation:
```csharp
private bool _isBusy;
public bool IsBusy
{
    get => _isBusy;
    set => this.RaiseAndSetIfChanged(ref _isBusy, value);
}

// In commands:
RefreshCommand = ReactiveCommand.CreateFromTask(LoadDataAsync,
    this.WhenAnyValue(x => x.IsBusy).Select(isBusy => !isBusy));
```

### XAML Binding:
```xml
<Button Command="{Binding RefreshCommand}" 
        Content="Refresh"
        IsEnabled="{Binding IsBusy, Converter={x:Static converters:InverseBooleanConverter.Instance}}"/>
        
<ProgressRing IsActive="{Binding IsBusy}"
              Visibility="{Binding IsBusy, Converter={x:Static converters:BooleanToVisibilityConverter.Instance}}"/>
```

## Testing Checklist

### Unit Tests Needed:
1. Command validation tests
2. MediatR handler tests
3. Service method tests
4. ViewModel command execution tests

### Integration Tests:
1. Database operation tests
2. API integration tests
3. End-to-end command flow tests

### Manual Verification:
1. Each button performs real operation
2. Loading states work correctly
3. Error messages are user-friendly
4. Data persists correctly

## Conclusion

This implementation guide provides a comprehensive roadmap for wiring all UI commands to real actions. By following these instructions, the application will transform from having placeholder implementations to fully functional business intelligence software with proper database/API integration, error handling, and user feedback.
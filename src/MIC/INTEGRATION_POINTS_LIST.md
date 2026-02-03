# Integration Points List - Mbarie Intelligence Console (MIC)

## Overview
This document catalogues all integration points between the Mbarie Intelligence Console application and external systems, internal services, and data sources. Each integration point includes technical specifications, authentication requirements, and implementation status.

## 1. External API Integrations

### 1.1 OpenAI / AI Provider APIs

#### OpenAI Chat Completions API
- **Endpoint**: `POST https://api.openai.com/v1/chat/completions`
- **Authentication**: Bearer token (API Key)
- **Required Scopes**: `chat.completions`
- **Implementation Status**: Partially implemented (`ChatService.cs`)
- **Usage**:
  - AI Chat functionality
  - Email analysis and prioritization
  - Predictive analytics
  - Natural language processing

**Request Format:**
```json
{
  "model": "gpt-4-turbo",
  "messages": [
    {"role": "system", "content": "Respond in {user_language} language."},
    {"role": "user", "content": "{message}"}
  ],
  "temperature": 0.7,
  "max_tokens": 2000
}
```

**Multilingual Support Enhancement:**
- System prompt must include language context
- Response language matches user preference
- Fallback to English if language not supported

#### OpenAI Embeddings API (Future)
- **Endpoint**: `POST https://api.openai.com/v1/embeddings`
- **Purpose**: Email content vectorization for semantic search
- **Status**: Not implemented

### 1.2 Gmail API (OAuth2 + REST)

#### OAuth2 Authorization
- **Authorization Endpoint**: `https://accounts.google.com/o/oauth2/v2/auth`
- **Token Endpoint**: `https://oauth2.googleapis.com/token`
- **Required Scopes**:
  - `https://www.googleapis.com/auth/gmail.readonly`
  - `https://www.googleapis.com/auth/gmail.modify`
  - `https://www.googleapis.com/auth/gmail.labels`
- **Implementation**: `EmailOAuth2Service.cs`

#### Gmail REST API Endpoints
- **List Messages**: `GET https://gmail.googleapis.com/gmail/v1/users/{userId}/messages`
- **Get Message**: `GET https://gmail.googleapis.com/gmail/v1/users/{userId}/messages/{messageId}`
- **Modify Labels**: `POST https://gmail.googleapis.com/gmail/v1/users/{userId}/messages/{messageId}/modify`
- **Implementation**: `RealEmailSyncService.cs`

### 1.3 Microsoft Graph API (Outlook/Office 365)

#### OAuth2 Authorization
- **Authorization Endpoint**: `https://login.microsoftonline.com/common/oauth2/v2.0/authorize`
- **Token Endpoint**: `https://login.microsoftonline.com/common/oauth2/v2.0/token`
- **Required Scopes**:
  - `Mail.Read`
  - `Mail.ReadWrite`
  - `Mail.Send` (future)
  - `User.Read`
- **Implementation**: `EmailOAuth2Service.cs`

#### Graph API Endpoints
- **List Messages**: `GET https://graph.microsoft.com/v1.0/me/messages`
- **Get Message**: `GET https://graph.microsoft.com/v1.0/me/messages/{messageId}`
- **Mail Folders**: `GET https://graph.microsoft.com/v1.0/me/mailFolders`
- **Implementation**: `RealEmailSyncService.cs`

### 1.4 IMAP/POP3 Email Protocols (Fallback)

#### IMAP Configuration
- **Gmail IMAP**: `imap.gmail.com:993` (SSL)
- **Outlook IMAP**: `outlook.office365.com:993` (SSL)
- **Protocol**: IMAP4 with SSL/TLS
- **Authentication**: OAuth2 (preferred) or username/password
- **Implementation**: MailKit library in `RealEmailSyncService.cs`

#### SMTP Configuration (Future - Send Email)
- **Gmail SMTP**: `smtp.gmail.com:587` (STARTTLS)
- **Outlook SMTP**: `smtp.office365.com:587` (STARTTLS)
- **Status**: Not implemented (receive-only currently)

## 2. Internal Service Interfaces

### 2.1 Application Layer Services (CQRS/MediatR)

#### Authentication Services
```csharp
// Implemented in MIC.Core.Application
public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(string username, string password);
    Task<RegistrationResult> RegisterAsync(UserRegistrationDto registration);
    Task<bool> LogoutAsync();
    Task<UserDto> GetCurrentUserAsync();
}

public interface IJwtTokenService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
    ClaimsPrincipal GetPrincipalFromToken(string token);
}
```

#### Email Services
```csharp
// Implemented in MIC.Core.Application
public interface IEmailSyncService
{
    Task<EmailSyncResult> SyncAccountAsync(EmailAccount account, CancellationToken ct = default);
    Task<EmailSyncResult> SyncAllAccountsAsync(string userId, CancellationToken ct = default);
    Task<EmailSyncResult> IncrementalSyncAsync(EmailAccount account, CancellationToken ct = default);
}

public interface IEmailAnalysisService
{
    Task<EmailAnalysisResult> AnalyzeEmailAsync(EmailMessage email, CancellationToken ct = default);
    Task<BatchAnalysisResult> AnalyzeEmailsAsync(IEnumerable<EmailMessage> emails, CancellationToken ct = default);
}
```

#### AI Services
```csharp
// Implemented in MIC.Infrastructure.AI
public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(string message, string conversationId, string userLanguage);
    Task<Conversation> GetConversationAsync(string conversationId);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId);
    Task DeleteConversationAsync(string conversationId);
}

public interface IKnowledgeBaseService
{
    Task IndexEmailAsync(EmailMessage email, CancellationToken ct = default);
    Task<IEnumerable<KnowledgeEntry>> SearchAsync(string query, int limit = 10, CancellationToken ct = default);
}
```

#### Localization Services (NEW - Required)
```csharp
// To be implemented
public interface ILocalizationService
{
    string GetString(string key);
    string GetString(string key, params object[] args);
    void SetLanguage(string languageCode);
    string CurrentLanguage { get; }
    event EventHandler LanguageChanged;
    
    // Resource management
    Task LoadResourcesAsync(string languageCode);
    bool HasKey(string key);
    IEnumerable<string> GetAvailableLanguages();
}
```

### 2.2 Infrastructure Layer Services

#### Repository Interfaces
```csharp
// Implemented in MIC.Core.Application.Common.Interfaces
public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(string id);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}

public interface IEmailRepository
{
    Task<EmailMessage?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<EmailMessage>> GetByAccountAsync(string accountId, EmailFolder? folder = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(string messageId, CancellationToken ct = default);
    Task AddAsync(EmailMessage email, CancellationToken ct = default);
    Task UpdateAsync(EmailMessage email, CancellationToken ct = default);
}

public interface IAlertRepository
{
    Task<IntelligenceAlert?> GetByIdAsync(string id);
    Task<IEnumerable<IntelligenceAlert>> GetAllAsync(AlertFilter? filter = null);
    Task AddAsync(IntelligenceAlert alert);
    Task UpdateAsync(IntelligenceAlert alert);
    Task DeleteAsync(IntelligenceAlert alert);
}

public interface IMetricsRepository
{
    Task<IEnumerable<OperationalMetric>> GetMetricsAsync(DateTime? startDate = null, DateTime? endDate = null, string? category = null);
    Task AddAsync(OperationalMetric metric);
    Task AddRangeAsync(IEnumerable<OperationalMetric> metrics);
}

// NEW: Localization Repository
public interface ILocalizationRepository
{
    Task<LocalizedString?> GetByKeyAsync(string key);
    Task<IEnumerable<LocalizedString>> GetByCategoryAsync(string category);
    Task UpdateTranslationAsync(string key, string languageCode, string translation);
    Task<IEnumerable<string>> GetAvailableKeysAsync();
}
```

#### Security Services
```csharp
public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool VerifyPassword(string password, string hash, string salt);
}

public interface IEncryptionService
{
    string Encrypt(string plainText, string key);
    string Decrypt(string cipherText, string key);
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string cipherText);
}
```

## 3. Database Integration Points

### 3.1 Entity Framework Core Context

#### Primary Database Context
- **Context Class**: `MicDbContext` (`MIC.Infrastructure.Data.Persistence`)
- **Database Provider**: SQLite (development), PostgreSQL (production)
- **Connection String**: Configuration-based with environment overrides
- **Migration Strategy**: Code-first with automatic migrations

#### Entity Configurations
```csharp
// Configured in MIC.Infrastructure.Data.Persistence.Configurations
public class UserConfiguration : IEntityTypeConfiguration<User>
public class EmailAccountConfiguration : IEntityTypeConfiguration<EmailAccount>
public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
public class IntelligenceAlertConfiguration : IEntityTypeConfiguration<IntelligenceAlert>
public class OperationalMetricConfiguration : IEntityTypeConfiguration<OperationalMetric>
// NEW: Localization configuration
public class LocalizedStringConfiguration : IEntityTypeConfiguration<LocalizedString>
```

#### Database Tables Schema
| Table | Primary Key | Foreign Keys | Indexes | Purpose |
|-------|-------------|--------------|---------|---------|
| `Users` | `Id` (string) | - | `Username`, `Email`, `Language` | User authentication and preferences |
| `EmailAccounts` | `Id` (string) | `UserId` → `Users` | `EmailAddress`, `Provider`, `UserId` | Email account configurations |
| `EmailMessages` | `Id` (string) | `EmailAccountId`, `UserId` | `MessageId` (unique), `ReceivedDate`, `Folder`, `Priority` | Email storage and metadata |
| `EmailAttachments` | `Id` (string) | `EmailMessageId` → `EmailMessages` | `EmailMessageId`, `FileName` | Email attachment storage |
| `IntelligenceAlerts` | `Id` (string) | `AcknowledgedBy`, `ResolvedBy` → `Users` | `Severity`, `Status`, `TriggeredAt` | Alert management |
| `OperationalMetrics` | `Id` (string) | `UserId` → `Users` | `Timestamp`, `Category`, `Source` | Metrics data storage |
| `ChatConversations` | `Id` (string) | `UserId` → `Users` | `UserId`, `CreatedAt` | AI chat conversations |
| `ChatMessages` | `Id` (string) | `ConversationId` → `ChatConversations` | `ConversationId`, `Timestamp`, `Role` | Individual chat messages |
| `LocalizedStrings` | `Id` (string) | - | `Key` (unique), `Category` | Multilingual resource storage |

### 3.2 Database Migration Points

#### Current Migrations
1. `20260123112624_InitialCreate` - Base schema
2. `20260124101147_PendingModelChanges` - Email model updates
3. `20260126083123_AddUserEntity` - User authentication

#### Required New Migrations
1. **Add Language Column to Users** - Support multilingual preferences
2. **Create LocalizedStrings Table** - Resource storage
3. **Add Chat Tables** - If not already present
4. **Add Predictions Table** - For prediction storage

### 3.3 Data Seeding Integration

#### Seed Data Sources
- **Initial Admin User**: Hardcoded in `DbInitializer` (must be removed/secure)
- **Demo Data**: Generated for development/demo purposes
- **Localization Resources**: Loaded from resource files into database

#### Seed Process Integration Points
1. `DbInitializer.InitializeAsync()` - Main entry point
2. `SeedDemoDataAsync()` - Demo data generation
3. `SeedAdminUserAsync()` - Admin user creation
4. `SeedLocalizationResourcesAsync()` - NEW: Load translation resources

## 4. File System Integration Points

### 4.1 Configuration Files

#### Application Settings
- **Primary**: `appsettings.json` (base configuration)
- **Development**: `appsettings.Development.json` (overrides)
- **Production**: `appsettings.Production.json` (environment-specific)
- **User Settings**: `%AppData%/MIC/settings.json` (user preferences)

#### Configuration Structure
```json
{
  "Database": {
    "ConnectionString": "DataSource=mic_dev.db",
    "RunMigrationsOnStartup": true,
    "SeedDataOnStartup": true
  },
  "Jwt": {
    "Secret": "development-secret-change-in-production",
    "Issuer": "MIC",
    "Audience": "MIC-Users",
    "ExpiryMinutes": 120
  },
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "", // Securely stored
    "Model": "gpt-4-turbo",
    "Temperature": 0.7
  },
  "EmailSync": {
    "InitialSyncMonths": 3,
    "SyncIntervalMinutes": 15,
    "BatchSize": 100
  },
  "Localization": {
    "DefaultLanguage": "en",
    "ResourcePath": "Resources",
    "FallbackLanguage": "en"
  }
}
```

### 4.2 Resource Files (Localization)

#### Resource File Structure
```
MIC.Desktop.Avalonia/
├── Resources/
│   ├── Strings.resx            (Default/English)
│   ├── Strings.fr.resx         (French)
│   ├── Strings.es.resx         (Spanish)
│   ├── Strings.ar.resx         (Arabic)
│   ├── Strings.zh.resx         (Chinese)
│   └── Strings.zh-Hans.resx    (Chinese Simplified)
```

#### Resource Key Categories
- `UI.*` - User interface labels, buttons, menus
- `Messages.*` - System messages, notifications
- `Errors.*` - Error messages and descriptions
- `Email.*` - Email-related text
- `Alerts.*` - Alert system text
- `Settings.*` - Settings interface text

### 4.3 Secure Storage

#### Windows DPAPI (Data Protection API)
- **Purpose**: Encrypt sensitive data (API keys, OAuth tokens)
- **Implementation**: `ProtectedData` class in `System.Security.Cryptography`
- **Scope**: `DataProtectionScope.CurrentUser`

#### Credential Manager (Windows)
- **Alternative**: Store OAuth refresh tokens in Windows Credential Manager
- **Advantage**: Integrated with Windows security
- **Implementation**: `CredentialManagement` NuGet package

#### File-Based Secure Storage
```csharp
public class SecureStorageService : ISecureStorageService
{
    private readonly string _encryptionKey; // Derived from user context
    
    public Task<string> EncryptAndStoreAsync(string key, string value);
    public Task<string> RetrieveAndDecryptAsync(string key);
    public Task DeleteSecureValueAsync(string key);
}
```

## 5. Operating System Integration Points

### 5.1 Windows-Specific Integrations

#### System Tray Integration
- **Purpose**: Background operation when minimized
- **Implementation**: `NotifyIcon` in Avalonia (platform-specific)
- **Features**: 
  - Minimize to tray
  - System notifications
  - Quick actions from tray menu

#### File System Watchers
- **Purpose**: Monitor configuration changes
- **Implementation**: `FileSystemWatcher` for settings files
- **Use Cases**: 
  - Hot-reload configuration
  - Detect new localization resources

#### Registry Integration (Optional)
- **Purpose**: Store machine-specific settings
- **Implementation**: `Microsoft.Win32.Registry` class
- **Use Cases**: 
  - Installation path
  - Machine ID for licensing
  - System-wide defaults

### 5.2 Cross-Platform Considerations

#### Platform Abstraction Layer
```csharp
public interface IPlatformService
{
    // File system
    string GetAppDataDirectory();
    string GetTempDirectory();
    
    // Security
    Task<string> SecureStoreAsync(string key, string value);
    Task<string> SecureRetrieveAsync(string key);
    
    // Notifications
    void ShowNotification(string title, string message, string type);
    
    // System integration
    void OpenBrowser(string url);
    void OpenFileLocation(string path);
}
```

## 6. Real-time Communication Points

### 6.1 SignalR/WebSocket Integration (Future)

#### Real-time Dashboard Updates
- **Endpoint**: `/hubs/dashboard` (SignalR hub)
- **Events**: 
  - `AlertCreated`
  - `MetricUpdated`
  - `EmailSynced`
  - `ChatMessageReceived`

#### Implementation Points
```csharp
public class DashboardHub : Hub
{
    public async Task SubscribeToAlerts();
    public async Task UnsubscribeFromAlerts();
    public async Task SendMetricUpdate(MetricUpdate update);
}
```

### 6.2 Background Service Integration

#### Email Sync Service
- **Implementation**: `BackgroundService` base class
- **Interval**: Configurable (default: 15 minutes)
- **Features**:
  - Incremental sync
  - Error recovery
  - Progress reporting

#### Metrics Collection Service
- **Purpose**: Periodically collect system metrics
- **Sources**: 
  - Database queries
  - External APIs
  - System performance counters

## 7. Third-Party Library Integration Points

### 7.1 MailKit (Email Processing)
- **Version**: 4.3.0+
- **Purpose**: IMAP/SMTP client
- **Integration Points**:
  - `RealEmailSyncService.cs` - Email retrieval
  - `ImapClient` - Connection management
  - `MimeMessage` - Email parsing

### 7.2 Entity Framework Core
- **Version**: 7.0.0+
- **Purpose**: Database ORM
- **Integration Points**:
  - `MicDbContext` - Database context
  - Repository implementations
  - Migration system

### 7.3 ReactiveUI (MVVM Framework)
- **Version**: 19.4.1+
- **Purpose**: Reactive MVVM pattern
- **Integration Points**:
  - All ViewModel classes
  - Command implementations
  - Property change notifications

### 7.4 MediatR (CQRS Pattern)
- **Version**: 12.2.0+
- **Purpose**: Command/Query separation
- **Integration Points**:
  - Command/Query handlers
  - Pipeline behaviors
  - Event publishing

### 7.5 LiveCharts (Data Visualization)
- **Version**: 2.0.0+
- **Purpose**: Charts and graphs
- **Integration Points**:
  - Dashboard metrics charts
  - Trend visualizations
  - Real-time updates

## 8. Authentication & Authorization Integration

### 8.1 JWT Token Flow

#### Token Generation
```csharp
// Integration point: JwtTokenService.GenerateToken()
var tokenHandler = new JwtSecurityTokenHandler();
var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("language", user.Language) // NEW
    }),
    Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
    SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(key), 
        SecurityAlgorithms.HmacSha256Signature)
};
```

#### Token Validation
- **Middleware**: JWT bearer authentication
- **Validation Parameters**: Issuer, audience, signing key
- **Claims Extraction**: User identity and language preference

### 8.2 OAuth2 Integration Points

#### Authorization Code Flow
1. User initiates OAuth in UI
2. Redirect to provider authorization page
3. User grants permissions
4. Redirect back with authorization code
5. Exchange code for access/refresh tokens
6. Store tokens securely

#### Token Refresh Flow
1. Detect token expiration
2. Use refresh token to get new access token
3. Update stored tokens
4. Continue operation

## 9. Error Handling & Monitoring Integration

### 9.1 Logging Integration

#### Serilog Configuration
- **Sinks**: Console, File, Seq (optional)
- **Enrichment**: User context, request ID
- **Structured Logging**: JSON format for analysis

#### Integration Points
```csharp
// Program.cs - Logger configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MIC")
    .CreateLogger();
```

### 9.2 Error Reporting Services

#### Application Insights (Azure)
- **Instrumentation Key**: From configuration
- **Telemetry**: Exceptions, metrics, dependencies
- **Integration**: `Microsoft.ApplicationInsights` package

#### Custom Error Handling
```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public Task HandleExceptionAsync(Exception exception, string context)
    {
        // Log to multiple destinations
        _logger.LogError(exception, "Unhandled exception in {Context}", context);
        _telemetryClient.TrackException(exception);
        
        // User-friendly error display
        ShowUserFriendlyError(exception);
        
        return Task.CompletedTask;
    }
}
```

## 10. Deployment & Update Integration

### 10.1 Installer Integration

#### Inno Setup (Windows)
- **Script**: `Create-Installer.ps1`
- **Output**: `MIC-Setup.exe`
- **Components**: 
  - Application files
  - Runtime dependencies
  - Start menu shortcuts
  - File associations

#### MSI Installer (Enterprise)
- **Requirements**: WiX Toolset
- **Features**: 
  - Silent installation
  - Administrative deployment
  - Upgrade management

### 10.2 Auto-Update Integration

#### Squirrel.Windows
- **Purpose**: Automatic updates
- **Integration Points**:
  - Update checking on startup
  - Download and apply updates
  - Version rollback capability

#### Custom Update Service
```csharp
public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync();
    Task DownloadUpdateAsync(UpdateInfo update);
    Task ApplyUpdateAsync();
    Version CurrentVersion { get; }
}
```

## 11. Integration Status & Priority

### 11.1 High Priority (Phase 1 - Must Wire)
| Integration Point | Status | Owner | Target Completion |
|-------------------|--------|-------|-------------------|
| OpenAI Chat API | Partial | AI Team | Week 1 |
| Gmail OAuth | Not wired | Email Team | Week 1 |
| Localization Service | Not implemented | UI Team | Week 1 |
| Real Authentication | Bypassed | Auth Team | Week 1 |
| Email Sync Service | Mock data | Email Team | Week 2 |

### 11.2 Medium Priority (Phase 2)
| Integration Point | Status | Purpose | Target |
|-------------------|--------|---------|--------|
| Microsoft Graph API | Not wired | Outlook integration | Week 3 |
| Predictive Analytics | Mock | Metrics prediction | Week 4 |
| Real-time Updates | Basic | Dashboard refresh | Week 4 |
| Advanced Search | Not implemented | Email/knowledge search | Week 5 |

### 11.3 Low Priority (Future)
| Integration Point | Purpose | Complexity | Notes |
|-------------------|---------|------------|-------|
| Mobile App Sync | Cross-device | High | Requires API server |
| Voice Commands | Hands-free | Medium | Platform-dependent |
| BI Export | Reporting | Low | PDF/Excel export |
| Plugin System | Extensibility | High | API design needed |

## 12. Testing Integration Points

### 12.1 Unit Test Integration
- **Framework**: xUnit
- **Mocking**: Moq
- **Coverage**: Repository layer, service layer

### 12.2 Integration Test Points
1. **Database Integration Tests**: EF Core context testing
2. **API Integration Tests**: External API mocking
3. **UI Integration Tests**: Avalonia UI testing
4. **End-to-End Tests**: Complete user flows

### 12.3 Performance Test Points
1. **Email Sync Performance**: Large mailbox simulation
2. **AI Response Time**: Concurrent chat requests
3. **Database Query Performance**: Index optimization
4. **Memory Usage**: Long-running session monitoring

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-30  
**Author:** Cline (AI Assistant)  
**Status:** Complete - Ready for Implementation Planning
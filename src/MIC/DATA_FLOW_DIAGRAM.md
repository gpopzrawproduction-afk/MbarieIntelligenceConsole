# Data Flow Diagram - Mbarie Intelligence Console (MIC)

## System Architecture Overview

```mermaid
flowchart TD
    subgraph "User Interface Layer"
        UI[Desktop UI<br/>Avalonia Views/ViewModels]
        LANG[Language Selector<br/>Multilingual UI]
        NAV[Navigation Controller]
    end
    
    subgraph "Application Layer"
        MED[MediatR Pipeline<br/>CQRS Pattern]
        CMD[Command Handlers]
        QRY[Query Handlers]
        VAL[Validation]
        AUTH[Authentication Service]
        LOC[Localization Service]
    end
    
    subgraph "Domain Layer"
        DOM[Domain Entities<br/>Business Rules]
        EVT[Domain Events]
    end
    
    subgraph "Infrastructure Layer"
        subgraph "Data Persistence"
            REPO[Repositories<br/>EF Core]
            DB[(SQLite/PostgreSQL<br/>Database)]
            MIG[Migrations]
        end
        
        subgraph "External Integrations"
            OAUTH[OAuth2 Services]
            IMAP[IMAP/SMTP Clients]
            AI_API[AI Provider APIs]
            EMAIL_API[Email Provider APIs]
        end
        
        subgraph "Service Layer"
            SYNC[Email Sync Service]
            CHAT[AI Chat Service]
            ANALYZE[Email Analysis Service]
            NOTIFY[Notification Service]
        end
    end
    
    subgraph "External Systems"
        GMAIL[Gmail API]
        OUTLOOK[Microsoft Graph]
        OPENAI[OpenAI API]
        OTHER_AI[Other AI Providers]
    end
    
    %% Data Flows
    UI -->|User Actions| NAV
    UI -->|Language Selection| LANG
    LANG -->|Culture Settings| LOC
    
    NAV -->|Navigation Events| MED
    UI -->|Commands| CMD
    UI -->|Queries| QRY
    
    MED -->|Process Commands| CMD
    MED -->|Process Queries| QRY
    CMD -->|Validate| VAL
    VAL -->|Domain Rules| DOM
    
    CMD -->|Save Data| REPO
    QRY -->|Read Data| REPO
    REPO -->|CRUD Operations| DB
    
    CMD -->|Raise Events| EVT
    EVT -->|Trigger| SYNC
    EVT -->|Trigger| NOTIFY
    
    SYNC -->|Authenticate| OAUTH
    OAUTH -->|Gmail OAuth| GMAIL
    OAUTH -->|Outlook OAuth| OUTLOOK
    
    SYNC -->|Fetch Emails| IMAP
    IMAP -->|Connect to| GMAIL
    IMAP -->|Connect to| OUTLOOK
    
    SYNC -->|Email Data| ANALYZE
    ANALYZE -->|AI Analysis| AI_API
    AI_API -->|OpenAI| OPENAI
    AI_API -->|Other Providers| OTHER_AI
    
    CHAT -->|Chat Requests| AI_API
    AI_API -->|Responses| CHAT
    
    NOTIFY -->|User Notifications| UI
```

## Detailed Data Flow Descriptions

### 1. Authentication Flow
```mermaid
sequenceDiagram
    participant User
    participant LoginView
    participant LoginVM
    participant MediatR
    participant AuthService
    participant UserRepo
    participant DB
    participant SessionService
    
    User->>LoginView: Enter credentials
    LoginView->>LoginVM: Submit login
    LoginVM->>MediatR: Send LoginCommand
    MediatR->>AuthService: Handle authentication
    AuthService->>UserRepo: Get user by username
    UserRepo->>DB: Query Users table
    DB-->>UserRepo: Return user data
    UserRepo-->>AuthService: Return user entity
    AuthService->>AuthService: Verify password hash
    AuthService->>AuthService: Generate JWT token
    AuthService->>UserRepo: Update last login
    AuthService-->>MediatR: Return LoginResult
    MediatR-->>LoginVM: Return result
    LoginVM->>SessionService: Set user session
    LoginVM->>LoginView: Navigate to MainWindow
```

### 2. Email Synchronization Flow
```mermaid
sequenceDiagram
    participant User
    participant EmailView
    participant EmailVM
    participant MediatR
    participant EmailSyncService
    participant OAuthService
    participant IMAPClient
    participant EmailRepo
    participant AIAnalysis
    participant DB
    
    User->>EmailView: Click "Sync"
    EmailView->>EmailVM: Execute sync command
    EmailVM->>MediatR: Request sync
    MediatR->>EmailSyncService: SyncAccountAsync
    EmailSyncService->>OAuthService: Get access token
    OAuthService->>EmailProvider: OAuth2 flow
    EmailProvider-->>OAuthService: Return token
    OAuthService-->>EmailSyncService: Return token
    EmailSyncService->>IMAPClient: Connect to server
    IMAPClient->>EmailProvider: IMAP connection
    EmailProvider-->>IMAPClient: Return emails
    IMAPClient-->>EmailSyncService: Email messages
    EmailSyncService->>EmailSyncService: Convert to entities
    EmailSyncService->>AIAnalysis: Analyze email content
    AIAnalysis->>OpenAI: Send for analysis
    OpenAI-->>AIAnalysis: Return analysis
    AIAnalysis-->>EmailSyncService: Apply AI flags
    EmailSyncService->>EmailRepo: Save to database
    EmailRepo->>DB: Insert/Update emails
    EmailSyncService-->>MediatR: Return sync result
    MediatR-->>EmailVM: Update UI
    EmailVM->>EmailView: Refresh email list
```

### 3. AI Chat Flow
```mermaid
sequenceDiagram
    participant User
    participant ChatView
    participant ChatVM
    participant ChatService
    participant AIProvider
    participant ConversationRepo
    participant DB
    
    User->>ChatView: Type message + Enter
    ChatView->>ChatVM: SendMessageCommand
    ChatVM->>ChatService: SendMessageAsync
    ChatService->>ChatService: Build context with language
    ChatService->>AIProvider: Send chat request
    AIProvider->>AIProvider: Process with language context
    AIProvider-->>ChatService: Return response
    ChatService->>ConversationRepo: Save user message
    ConversationRepo->>DB: Insert message
    ChatService->>ConversationRepo: Save AI response
    ConversationRepo->>DB: Insert response
    ChatService-->>ChatVM: Return conversation
    ChatVM->>ChatView: Update chat UI
```

### 4. Localization Flow
```mermaid
sequenceDiagram
    participant User
    participant LoginView
    participant SettingsView
    participant LocalizationService
    participant ResourceFiles
    participant UserRepo
    participant DB
    
    User->>LoginView: Select language
    LoginView->>LocalizationService: SetCurrentCulture
    LocalizationService->>ResourceFiles: Load translations
    ResourceFiles-->>LocalizationService: Return strings
    LocalizationService->>LoginView: Update UI text
    
    User->>SettingsView: Change language
    SettingsView->>LocalizationService: UpdateLanguage
    LocalizationService->>UserRepo: Save preference
    UserRepo->>DB: Update User.Language
    LocalizationService->>AllViews: Refresh all UI
```

## Data Storage Schema

### Core Tables
```mermaid
erDiagram
    User ||--o{ EmailAccount : "has"
    User ||--o{ IntelligenceAlert : "creates"
    User ||--o{ OperationalMetric : "generates"
    User ||--o{ ChatConversation : "participates"
    
    EmailAccount ||--o{ EmailMessage : "contains"
    EmailMessage ||--o{ EmailAttachment : "has"
    
    User {
        string Id PK
        string Username
        string Email
        string PasswordHash
        string Salt
        string FullName
        string Role
        string Language "English|French|Spanish|Arabic|Chinese"
        datetime CreatedAt
        datetime UpdatedAt
        datetime LastLoginAt
        boolean IsActive
    }
    
    EmailAccount {
        string Id PK
        string EmailAddress
        string Provider "Gmail|Outlook"
        string UserId FK
        string DisplayName
        string EncryptedTokens
        string SyncStatus
        datetime LastSyncAt
        integer SyncCount
    }
    
    EmailMessage {
        string Id PK
        string MessageId
        string Subject
        string FromAddress
        string FromName
        string ToRecipients
        string CcRecipients
        string BccRecipients
        datetime SentDate
        datetime ReceivedDate
        string BodyText
        string BodyHtml
        string EmailAccountId FK
        string UserId FK
        string Folder "Inbox|Sent|Drafts|Trash|Archive"
        string Priority "High|Normal|Low"
        boolean IsRead
        boolean IsFlagged
        boolean IsUrgent
        boolean RequiresResponse
        boolean HasActionItems
        string Category "General|Work|Personal|News|Notifications"
        string Sentiment "Positive|Neutral|Negative"
    }
    
    IntelligenceAlert {
        string Id PK
        string AlertName
        string Description
        string Severity "Critical|Warning|Info"
        string Status "Active|Acknowledged|Resolved"
        string Source
        datetime TriggeredAt
        datetime AcknowledgedAt
        datetime ResolvedAt
        string AcknowledgedBy FK
        string ResolvedBy FK
        boolean IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    OperationalMetric {
        string Id PK
        string Name
        string Category
        string Source
        decimal Value
        string Unit
        string Severity "Normal|Warning|Critical"
        datetime Timestamp
        string UserId FK
    }
    
    ChatConversation {
        string Id PK
        string UserId FK
        string Title
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    ChatMessage {
        string Id PK
        string ConversationId FK
        string Role "User|Assistant|System"
        string Content
        datetime Timestamp
        json Metadata
    }
    
    LocalizedString {
        string Id PK
        string Key
        string English
        string French
        string Spanish
        string Arabic
        string Chinese
        string Category "UI|Messages|Errors|Notifications"
    }
```

## API Integration Points

### External API Endpoints

#### Gmail API (OAuth2 + REST)
```
POST https://accounts.google.com/o/oauth2/v2/auth
POST https://oauth2.googleapis.com/token
GET  https://gmail.googleapis.com/gmail/v1/users/{userId}/messages
GET  https://gmail.googleapis.com/gmail/v1/users/{userId}/messages/{messageId}
```

#### Microsoft Graph API (OAuth2 + REST)
```
POST https://login.microsoftonline.com/common/oauth2/v2.0/authorize
POST https://login.microsoftonline.com/common/oauth2/v2.0/token
GET  https://graph.microsoft.com/v1.0/me/messages
GET  https://graph.microsoft.com/v1.0/me/messages/{messageId}
```

#### OpenAI API (Chat Completions)
```
POST https://api.openai.com/v1/chat/completions
Headers:
  Authorization: Bearer {api_key}
  Content-Type: application/json
Body:
  {
    "model": "gpt-4",
    "messages": [
      {"role": "system", "content": "Respond in {user_language} language."},
      {"role": "user", "content": "{user_message}"}
    ],
    "temperature": 0.7
  }
```

### Internal Service Interfaces

#### IEmailSyncService
```csharp
public interface IEmailSyncService
{
    Task<EmailSyncResult> SyncAccountAsync(EmailAccount account, CancellationToken ct = default);
    Task<EmailSyncResult> SyncAllAccountsAsync(string userId, CancellationToken ct = default);
    Task<EmailSyncResult> IncrementalSyncAsync(EmailAccount account, CancellationToken ct = default);
}
```

#### IChatService
```csharp
public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(string message, string conversationId, string userLanguage);
    Task<Conversation> GetConversationAsync(string conversationId);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId);
    Task DeleteConversationAsync(string conversationId);
}
```

#### ILocalizationService
```csharp
public interface ILocalizationService
{
    string GetString(string key);
    string GetString(string key, params object[] args);
    void SetLanguage(string languageCode);
    string CurrentLanguage { get; }
    event EventHandler LanguageChanged;
}
```

#### IAuthenticationService
```csharp
public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(string username, string password);
    Task<RegistrationResult> RegisterAsync(UserRegistrationDto registration);
    Task<bool> LogoutAsync();
    Task<UserDto> GetCurrentUserAsync();
}
```

## Data Transformation Points

### 1. Email → Entity Conversion
- **Input**: MimeMessage (MailKit)
- **Transformation**: Extract headers, body, attachments
- **Output**: EmailMessage entity
- **Location**: `RealEmailSyncService.ConvertToEntity()`

### 2. Alert Severity Mapping
- **Input**: Raw alert data (numeric/string)
- **Transformation**: Map to enum (Critical=0, Warning=1, Info=2)
- **Output**: AlertSeverity enum
- **Location**: `AlertConverters.Convert()`

### 3. Metric Normalization
- **Input**: Raw metric values from various sources
- **Transformation**: Scale, unit conversion, outlier detection
- **Output**: Normalized OperationalMetric
- **Location**: `MetricsRepository.NormalizeMetric()`

### 4. Localization Lookup
- **Input**: Resource key + language code
- **Transformation**: Database/Resource file lookup
- **Output**: Translated string
- **Location**: `LocalizationService.GetString()`

## Error Handling and Data Flow Recovery

### Retry Logic
1. **Email Sync Failures**: Exponential backoff (1s, 2s, 4s, 8s)
2. **API Rate Limits**: 429 response → wait and retry
3. **Network Issues**: 3 retries with increasing delay

### Data Consistency
1. **Transactional Operations**: Use EF Core transactions for multi-step updates
2. **Idempotent Operations**: Sync operations can be safely retried
3. **Conflict Resolution**: Last-write-wins for email updates

### Monitoring Points
1. **Sync Duration**: Track time per email account
2. **API Latency**: Monitor external API response times
3. **Error Rates**: Track failures by category (auth, network, parsing)
4. **Data Volume**: Count emails synced, alerts created, chat messages

## Performance Considerations

### Data Flow Bottlenecks
1. **Email Sync**: Batch processing (100 emails/batch)
2. **AI Analysis**: Queue-based processing for non-real-time analysis
3. **UI Updates**: Debounced refresh (min 500ms between updates)

### Caching Strategy
1. **Localization Strings**: Memory cache with file watcher
2. **User Session**: In-memory with periodic persistence
3. **Email Metadata**: LRU cache for frequently accessed emails

### Database Optimization
1. **Indexes**: UserId, EmailAccountId, CreatedAt timestamps
2. **Partitioning**: Consider time-based partitioning for email messages
3. **Archiving**: Move old emails to cold storage after 90 days

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-30  
**Author:** Cline (AI Assistant)  
**Status:** Complete
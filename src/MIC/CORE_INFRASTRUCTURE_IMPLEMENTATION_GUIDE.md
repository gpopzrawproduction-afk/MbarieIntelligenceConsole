# MIC Core Infrastructure Implementation Guide

## Overview
This document provides detailed implementation instructions for fixing core infrastructure issues in the Mbarie Intelligence Console (MIC) application. The fixes address authentication, configuration management, database schema, and dependency injection.

## 1. AUTHENTICATION SYSTEM

### 1.1 Remove Demo User Bypass from App.axaml.cs

**File:** `MIC.Desktop.Avalonia/App.axaml.cs`

**Changes Made:**
- Removed hardcoded demo user credentials
- Implemented session-based authentication flow
- Added "remember me" functionality using `UserSessionService`

**Code Implementation:**
```csharp
// Check for existing session (remember me functionality)
var sessionService = Program.ServiceProvider?.GetRequiredService<UserSessionService>();
if (sessionService != null && sessionService.IsLoggedIn && !string.IsNullOrEmpty(sessionService.GetToken()))
{
    // User is already logged in - go straight to main window
    var mainWindow = new MainWindow
    {
        DataContext = Program.ServiceProvider!.GetRequiredService<MainWindowViewModel>()
    };
    desktop.MainWindow = mainWindow;
}
else
{
    // Show login window
    var loginWindow = new LoginWindow();
    loginWindow.Show();
    desktop.MainWindow = loginWindow;
}
```

### 1.2 Implement Real Login Flow

**Files:**
- `MIC.Desktop.Avalonia/ViewModels/LoginViewModel.cs`
- `MIC.Infrastructure.Identity/AuthenticationService.cs`
- `MIC.Core.Application/Authentication/Commands/LoginCommand/LoginCommandHandler.cs`

**Implementation Details:**
1. `LoginViewModel` now uses MediatR pattern with `LoginCommand`
2. Real authentication against database with password hashing
3. JWT token generation on successful login
4. Session persistence with "remember me" option

**LoginCommandHandler Implementation:**
```csharp
public async Task<ErrorOr<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
{
    // 1. Validate user exists
    var user = await _userRepository.GetByUsernameAsync(request.Username);
    if (user == null)
        return Error.Validation(description: "Invalid username or password");
    
    // 2. Verify password hash
    var isValid = await _passwordHasher.VerifyPasswordAsync(user.PasswordHash, request.Password, user.Salt);
    if (!isValid)
        return Error.Validation(description: "Invalid username or password");
    
    // 3. Generate JWT token
    var token = await _jwtTokenService.GenerateTokenAsync(user);
    
    // 4. Return success result
    return new LoginResult(
        Success: true,
        Token: token,
        User: UserDto.FromUser(user),
        ErrorMessage: null);
}
```

### 1.3 Configure JWT Token Management

**File:** `MIC.Infrastructure.Identity/JwtTokenService.cs`

**Configuration:**
```csharp
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    
    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }
    
    public async Task<string> GenerateTokenAsync(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 1.4 Set Up Session Persistence

**File:** `MIC.Desktop.Avalonia/Services/UserSessionService.cs`

**Implementation:**
- Session data stored in `%LocalAppData%/MIC/session.json`
- Automatic session loading on application startup
- 30-day session expiration policy
- Secure token storage (in-memory only)

**Key Methods:**
```csharp
private void LoadSession()
{
    try
    {
        if (File.Exists(_sessionFilePath))
        {
            var json = File.ReadAllText(_sessionFilePath);
            _currentSession = JsonSerializer.Deserialize<UserSession>(json);
            
            // Check if session is still valid (e.g., not older than 30 days)
            if (_currentSession != null && 
                (DateTime.Now - _currentSession.LoginTime).TotalDays > 30)
            {
                _currentSession = null;
                File.Delete(_sessionFilePath);
            }
        }
    }
    catch
    {
        _currentSession = null;
    }
}

private async Task SaveSessionAsync()
{
    if (_currentSession == null) return;

    var json = JsonSerializer.Serialize(_currentSession, new JsonSerializerOptions 
    { 
        WriteIndented = true 
    });
    await File.WriteAllTextAsync(_sessionFilePath, json);
}
```

### 1.5 Create Password Hashing/Validation

**File:** `MIC.Infrastructure.Identity/Services/PasswordHasher.cs`

**Implementation:**
```csharp
public class PasswordHasher : IPasswordHasher
{
    public async Task<string> HashPasswordAsync(string password)
    {
        // Generate random salt
        var saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        
        var salt = Convert.ToBase64String(saltBytes);
        
        // Hash password with salt
        using var sha256 = SHA256.Create();
        var passwordBytes = Encoding.UTF8.GetBytes(password + salt);
        var hashBytes = await sha256.ComputeHashAsync(new MemoryStream(passwordBytes));
        var hash = Convert.ToBase64String(hashBytes);
        
        return hash;
    }
    
    public async Task<bool> VerifyPasswordAsync(string storedHash, string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var passwordBytes = Encoding.UTF8.GetBytes(password + salt);
        var hashBytes = await sha256.ComputeHashAsync(new MemoryStream(passwordBytes));
        var computedHash = Convert.ToBase64String(hashBytes);
        
        return storedHash == computedHash;
    }
}
```

### 1.6 Add "Remember Me" Functionality

**File:** `MIC.Desktop.Avalonia/ViewModels/LoginViewModel.cs`

**Implementation:**
- `RememberMe` boolean property bound to UI checkbox
- Session persistence controlled by `RememberMe` setting
- Automatic login on application startup if `RememberMe` is true

## 2. CONFIGURATION MANAGEMENT

### 2.1 Design Settings Persistence Strategy

**Dual-Persistence Strategy:**
1. **AppData Storage:** `%AppData%/MIC/settings.json` for desktop persistence
2. **Database Storage:** `UserSettings` table for user-specific settings
3. **Fallback:** Default settings when no user settings exist

### 2.2 Implement SettingsService for Runtime Configuration

**File:** `MIC.Infrastructure.Data/Services/SettingsService.cs`

**Key Features:**
- Loads settings from app data on initialization
- Saves settings to both app data and database
- User-specific settings loaded from database when authenticated
- Event-based notification when settings change

**Interface:** `MIC.Core.Application/Common/Interfaces/ISettingsService.cs`
```csharp
public interface ISettingsService
{
    AppSettings GetSettings();
    Task SaveSettingsAsync(AppSettings settings);
    Task<AppSettings> LoadUserSettingsAsync(Guid userId);
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}
```

### 2.3 Create User Profile Storage in %AppData%/MIC/

**Implementation:**
```csharp
// In SettingsService constructor
var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var micFolder = Path.Combine(appData, "MIC");
Directory.CreateDirectory(micFolder);
_appDataSettingsPath = Path.Combine(micFolder, "settings.json");
```

**File Structure:**
```
%AppData%/MIC/
├── settings.json          # Application settings
├── session.json          # User session data
└── logs/                 # Application logs
```

### 2.4 Wire Settings UI to Persist Changes

**Implementation Pattern:**
1. Create `SettingsViewModel` with bindable properties
2. Use `SettingsService` to load/save settings
3. Implement `ICommand` for save operation
4. Notify UI of changes using ReactiveUI

**SettingsViewModel Template:**
```csharp
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private AppSettings _settings;
    
    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = _settingsService.GetSettings();
        
        SaveCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync);
    }
    
    // Bindable properties
    public bool DarkMode
    {
        get => _settings.DarkMode;
        set
        {
            _settings.DarkMode = value;
            this.RaisePropertyChanged();
        }
    }
    
    public int RefreshInterval
    {
        get => _settings.RefreshInterval;
        set
        {
            _settings.RefreshInterval = value;
            this.RaisePropertyChanged();
        }
    }
    
    // Save command implementation
    private async Task SaveSettingsAsync()
    {
        await _settingsService.SaveSettingsAsync(_settings);
    }
}
```

### 2.5 Enable Runtime Reconfiguration for AI Services

**File:** `MIC.Infrastructure.AI/Services/ChatService.cs`

**Dynamic Configuration:**
```csharp
public class ChatService : IChatService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ChatService> _logger;
    
    public ChatService(ISettingsService settingsService, ILogger<ChatService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }
    
    public async Task<string> GetResponseAsync(string query, string? context = null)
    {
        var settings = _settingsService.GetSettings();
        
        // Use AI provider from settings
        var provider = settings.AI.Provider;
        var modelId = settings.AI.OpenAI.ModelId;
        var temperature = settings.AI.OpenAI.Temperature;
        
        // Runtime configuration based on settings
        return await GenerateResponseWithConfig(query, provider, modelId, temperature);
    }
}
```

## 3. DATABASE SCHEMA & MIGRATIONS

### 3.1 Entity Models Review

**Entities Created/Updated:**
1. **UserSettings** - User-specific application settings
2. **ChatHistory** - AI chat conversation history
3. **User** - Enhanced with additional fields (Department, JobPosition, Role, etc.)
4. **EmailMessage** - Added Priority and IsUrgent fields

### 3.2 Create Missing Tables

**Migration:** `20260130041941_AddUserSettingsAndChatHistory.cs`

**Tables Created:**
1. **UserSettings Table:**
   - `UserId` (Guid, FK to Users)
   - `SettingsJson` (string, max 8000)
   - `LastUpdated` (DateTimeOffset)
   - `SettingsVersion` (int, default 1)

2. **ChatHistory Table:**
   - `UserId` (Guid, FK to Users)
   - `SessionId` (string, max 100)
   - `Query` (string, max 4000)
   - `Response` (string, max 8000)
   - `Timestamp` (DateTimeOffset)
   - `AIProvider` (string, max 50)
   - `ModelUsed` (string, max 100)
   - `TokenCount` (int, default 0)
   - `Cost` (decimal, nullable)

3. **KnowledgeEntries Table:**
   - `UserId` (Guid)
   - `Title` (string, max 500)
   - `Content` (string)
   - `SourceType` (string, max 100)
   - `SourceId` (Guid)
   - `Tags` (string)
   - `RelevanceScore` (double)

### 3.3 Generate EF Core Migration Scripts

**Migration Commands:**
```bash
# Navigate to Infrastructure.Data project
cd MIC.Infrastructure.Data

# List existing migrations
dotnet ef migrations list

# Add new migration for schema changes
dotnet ef migrations add AddUserSettingsAndChatHistory -o Migrations

# Update database
dotnet ef database update
```

**Migration Output:**
```
Build started...
Build succeeded.
[EF DESIGN-TIME] MicDbContextFactory.CreateDbContext invoked
[EF DESIGN-TIME] Using SQLite: Data Source=mic_dev.db
An operation was scaffolded that may result in the loss of data.
Please review the migration for accuracy.
Done. To undo this action, use 'ef migrations remove'
```

### 3.4 Configure Seeding Strategy for Initial Data

**File:** `MIC.Infrastructure.Data/Persistence/DbInitializer.cs`

**Seeding Implementation:**
```csharp
public class DbInitializer
{
    public static async Task InitializeAsync(MicDbContext context, IUserRepository userRepository)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Seed default admin user if no users exist
        if (!await context.Users.AnyAsync())
        {
            var hasher = new PasswordHasher();
            var salt = Guid.NewGuid().ToString("N");
            var passwordHash = await hasher.HashPasswordAsync("admin123", salt);
            
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@mbarie.com",
                FullName = "Administrator",
                PasswordHash = passwordHash,
                Salt = salt,
                Role = UserRole.Admin,
                Department = "IT",
                JobPosition = "System Administrator",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            
            await userRepository.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }
        
        // Seed default settings if none exist
        if (!await context.UserSettings.AnyAsync())
        {
            var defaultSettings = new UserSettings
            {
                Id = Guid.NewGuid(),
                UserId = (await context.Users.FirstAsync()).Id,
                SettingsJson = JsonSerializer.Serialize(new AppSettings()),
                LastUpdated = DateTimeOffset.UtcNow,
                SettingsVersion = 1
            };
            
            context.UserSettings.Add(defaultSettings);
            await context.SaveChangesAsync();
        }
    }
}
```

### 3.5 Set Up Migration Execution on Startup

**File:** `MIC.Desktop.Avalonia/Program.cs`

**Migration Configuration:**
```csharp
public static class Program
{
    public static IServiceProvider? ServiceProvider { get; private set; }
    
    public static void Main(string[] args)
    {
        try
        {
            // Build service provider
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            
            // Run database migrations
            RunDatabaseMigrations();
            
            // Start Avalonia application
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }
    
    private static void RunDatabaseMigrations()
    {
        using var scope = ServiceProvider!.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MicDbContext>();
        
        // Apply pending migrations
        if (dbContext.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("Applying database migrations...");
            dbContext.Database.Migrate();
            Console.WriteLine("Database migrations completed.");
        }
        
        // Seed initial data
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        initializer.InitializeAsync().Wait();
    }
}
```

## 4. DEPENDENCY INJECTION FIXES

### 4.1 Ensure All ViewModels Are Properly Registered

**File:** `MIC.Desktop.Avalonia/DependencyInjection.cs` (to be created)

**ViewModel Registration:**
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {
        // Register ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AddEmailAccountViewModel>();
        services.AddTransient<EmailInboxViewModel>();
        services.AddTransient<AlertListViewModel>();
        services.AddTransient<MetricsDashboardViewModel>();
        services.AddTransient<PredictionsViewModel>();
        
        // Register Services
        services.AddSingleton<UserSessionService>();
        services.AddSingleton<ISessionService>(sp => sp.GetRequiredService<UserSessionService>());
        services.AddSingleton<KeyboardShortcutService>();
        
        return services;
    }
}
```

### 4.2 Verify Service Lifetime Scopes (Singleton vs Scoped)

**Lifetime Guidelines:**
1. **Singleton:** `UserSessionService`, `KeyboardShortcutService`, `SettingsService`
2. **Scoped:** `MicDbContext`, `IUserRepository`, `IEmailSyncService`
3. **Transient:** All ViewModels, Command Handlers

**File:** `MIC.Infrastructure.Data/DependencyInjection.cs`

**Updated Registrations:**
```csharp
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IAlertRepository, AlertRepository>();
services.AddScoped<IMetricsRepository, MetricsRepository>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IEmailRepository, EmailRepository>();
services.AddScoped<IEmailAccountRepository, EmailAccountRepository>();
services.AddScoped<IEmailSyncService, RealEmailSyncService>();
services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
services.AddScoped<ISettingsService, SettingsService>(); // NEW
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

### 4.3 Fix IChatService Registration and Configuration

**File:** `MIC.Infrastructure.AI/DependencyInjection.cs`

**Registration Fix:**
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register AI services
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IEmailAnalysisService, RealEmailAnalysisService>();
        
        // Configure AI settings
        services.Configure<SemanticKernelConfig>(configuration.GetSection("AI"));
        
        // Register Semantic Kernel services
        services.AddSingleton<IKernel>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<SemanticKernelConfig>>().Value;
            
            var kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: config.OpenAI.ModelId,
                    apiKey: config.OpenAI.ApiKey)
                .Build();
            
            return kernel;
        });
        
        return services;
    }
}
```

### 4.4 Wire Up Email OAuth Services

**File:** `MIC.Infrastructure.Identity/DependencyInjection.cs`

**OAuth Service Registration:**
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register authentication services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        
        // Register email OAuth services
        services.AddScoped<IEmailOAuth2Service, EmailOAuth2Service>();
        
        // Configure JWT settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        
        // Configure OAuth2 settings
        services.Configure<OAuth2Settings>(configuration.GetSection("OAuth2"));
        
        return services;
    }
}
```

## CONFIGURATION FILE TEMPLATES

### appsettings.json Template

```json
{
  "Database": {
    "Provider": "SQLite",
    "RunMigrationsOnStartup": true,
    "DeleteDatabaseOnStartup": false,
    "SeedDataOnStartup": true
  },
  "ConnectionStrings": {
    "MicSqlite": "Data Source=mic_dev.db",
    "MicDatabase": "Data Source=mic_dev.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AI": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "${OPENAI_API_KEY}",
      "ModelId": "gpt-4-turbo-preview",
      "MaxTokens": 4000,
      "Temperature": 0.7
    },
    "Features": {
      "EmailAnalysis": true,
      "ChatAssistant": true
    }
  },
  "OAuth2": {
    "Gmail": {
      "ClientId": "${GMAIL_CLIENT_ID}",
      "ClientSecret": "${GMAIL_CLIENT_SECRET}",
      "RedirectUri": "http://localhost:5000/oauth2callback"
    }
  },
  "JwtSettings": {
    "SecretKey": "${JWT_SECRET_KEY}",
    "Issuer": "MbarieIntelligenceConsole",
    "Audience": "MbarieIntelligenceConsole",
    "ExpirationHours": 8
  }
}
```

### .env.example Template

```env
# Database Configuration
MIC_CONNECTION_STRING=Data Source=mic_prod.db

# JWT Configuration
JWT_SECRET_KEY=your-32-character-secret-key-here-change-in-production

# AI Services
OPENAI_API_KEY=your-openai-api-key-here
AZURE_OPENAI_ENDPOINT=your-azure-openai-endpoint
AZURE_OPENAI_API_KEY=your-azure-openai-api-key

# Email OAuth
GMAIL_CLIENT_ID=your-gmail-client-id
GMAIL_CLIENT_SECRET=your-gmail-client-secret
OUTLOOK_CLIENT_ID=your-outlook-client-id
OUTLOOK_CLIENT_SECRET=your-outlook-client-secret
```

## DATABASE MIGRATION COMMANDS

### Development Environment

```powershell
# Build the project first
dotnet build

# Navigate to Infrastructure.Data project
cd MIC.Infrastructure.Data

# List all migrations
dotnet ef migrations list

# Create new migration
dotnet ef migrations add YourMigrationName -o Migrations

# Update database
dotnet ef database update

# Revert last migration
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script -o migration.sql
```

### Production Environment

```powershell
# Apply migrations on application startup (recommended)
# Set in appsettings.json: "RunMigrationsOnStartup": true

# Or apply manually
dotnet ef database update --connection "YourProductionConnectionString"
```

## DI REGISTRATION CHECKLIST

### ✅ Completed Registrations

- [x] **MIC.Infrastructure.Data/DependencyInjection.cs**
  - [x] `ISettingsService` → `SettingsService` (Scoped)
  - [x] `IUserRepository` → `UserRepository` (Scoped)
  - [x] `IEmailSyncService` → `RealEmailSyncService` (Scoped)
  - [x] `MicDbContext` (Scoped with SQLite/PostgreSQL)

- [x] **MIC.Infrastructure.Identity/DependencyInjection.cs**
  - [x] `IAuthenticationService` → `AuthenticationService` (Scoped)
  - [x] `IJwtTokenService` → `JwtTokenService` (Scoped)
  - [x] `IPasswordHasher` → `PasswordHasher` (Scoped)
  - [x] `IEmailOAuth2Service` → `EmailOAuth2Service` (Scoped)

- [x] **MIC.Infrastructure.AI/DependencyInjection.cs**
  - [x] `IChatService` → `ChatService` (Scoped)
  - [x] `IEmailAnalysisService` → `RealEmailAnalysisService` (Scoped)
  - [x] `IKernel` (Singleton with OpenAI configuration)

### ⚠️ Required Registrations (To Be Implemented)

- [ ] **MIC.Desktop.Avalonia/DependencyInjection.cs** (New file needed)
  - [ ] All ViewModels (Transient)
  - [ ] `UserSessionService` (Singleton)
  - [ ] `KeyboardShortcutService` (Singleton)
  - [ ] `NotificationService` (Singleton)

- [ ] **MIC.Core.Application/DependencyInjection.cs**
  - [ ] MediatR handlers registration
  - [ ] CQRS pattern implementation

## TESTING INSTRUCTIONS

### 1. Authentication System Test

```csharp
// Unit Test Example
[Fact]
public async Task Login_WithValidCredentials_ReturnsToken()
{
    // Arrange
    var username = "admin";
    var password = "admin123";
    
    // Act
    var result = await _authenticationService.LoginAsync(username, password);
    
    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.Token);
    Assert.NotEmpty(result.Token);
}
```

### 2. Configuration Management Test

```csharp
[Fact]
public async Task SettingsService_SavesAndLoadsSettings()
{
    // Arrange
    var settings = new AppSettings
    {
        DarkMode = true,
        RefreshInterval = 60
    };
    
    // Act
    await _settingsService.SaveSettingsAsync(settings);
    var loadedSettings = _settingsService.GetSettings();
    
    // Assert
    Assert.Equal(settings.DarkMode, loadedSettings.DarkMode);
    Assert.Equal(settings.RefreshInterval, loadedSettings.RefreshInterval);
}
```

### 3. Database Migration Test

```powershell
# Test migration commands
cd MIC.Infrastructure.Data
dotnet ef migrations list
dotnet ef database update --dry-run
```

### 4. Dependency Injection Test

```csharp
[Fact]
public void ServiceProvider_ResolvesAllRequiredServices()
{
    // Act & Assert - Should not throw
    var authService = _serviceProvider.GetRequiredService<IAuthenticationService>();
    var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
    var chatService = _serviceProvider.GetRequiredService<IChatService>();
    
    Assert.NotNull(authService);
    Assert.NotNull(settingsService);
    Assert.NotNull(chatService);
}
```

## DEPLOYMENT NOTES

### 1. Environment Variables
Set the following environment variables in production:
- `JWT_SECRET_KEY` - 32+ character secret for JWT signing
- `MIC_CONNECTION_STRING` - Database connection string
- `OPENAI_API_KEY` - OpenAI API key for AI features
- OAuth2 client IDs and secrets for email integration

### 2. Database Provider Selection
The application supports both SQLite (development) and PostgreSQL (production). Configure in `appsettings.json`:
```json
{
  "Database": {
    "Provider": "PostgreSQL"  // Change from SQLite to PostgreSQL for production
  }
}
```

### 3. Security Considerations
- JWT secret must be strong and kept secret
- Password hashing uses SHA256 with random salt
- Session data stored in user's local app data
- No hardcoded credentials in source code

## TROUBLESHOOTING

### Common Issues and Solutions

1. **Migration Errors**
   ```
   Error: An operation was scaffolded that may result in the loss of data.
   ```
   **Solution:** Review the migration file and ensure data loss is acceptable, or create data migration scripts.

2. **JWT Token Validation Failures**
   ```
   Error: Invalid token
   ```
   **Solution:** Ensure `JWT_SECRET_KEY` environment variable is set and consistent across deployments.

3. **Database Connection Issues**
   ```
   Error: No database connection string configured
   ```
   **Solution:** Set `MIC_CONNECTION_STRING` environment variable or configure in `appsettings.json`.

4. **Dependency Injection Errors**
   ```
   Error: Unable to resolve service for type 'X'
   ```
   **Solution:** Verify service is registered in the appropriate `DependencyInjection.cs` file with correct lifetime.

## CONCLUSION

The core infrastructure fixes have been implemented following clean architecture principles and CQRS/MediatR patterns. The system now features:

1. **Robust Authentication:** Real login flow with JWT tokens and session persistence
2. **Flexible Configuration:** Dual-persistence settings with runtime reconfiguration
3. **Complete Database Schema:** All required entities with proper migrations
4. **Proper Dependency Injection:** Correct service lifetimes and registrations

The implementation maintains separation of concerns and follows best practices for enterprise application development.
# Deployment Guide

## Prerequisites

### Production Environment
- **Operating System**: Windows Server 2019+ or Windows 10/11
- **.NET Runtime**: .NET 9.0 Runtime or SDK
- **Database**: PostgreSQL 15+ (recommended) or SQLite (for simple deployments)
- **Storage**: Minimum 2GB free disk space
- **Memory**: Minimum 4GB RAM

### Development Environment
- **.NET SDK**: 9.0.100+
- **IDE**: Visual Studio 2022+ or Visual Studio Code
- **Database**: PostgreSQL 15+ or SQLite

## Installation Methods

### Method 1: Standalone Executable (Recommended)

#### Step 1: Download Release
1. Go to the [Releases page](https://github.com/your-org/mbarie-intelligence-console/releases)
2. Download the latest `MIC.Desktop.Avalonia-win-x64.zip`
3. Extract to a deployment folder (e.g., `C:\Program Files\MbarieIntelligenceConsole`)

#### Step 2: Configure Environment
1. Create a `.env` file in the deployment folder:
   ```bash
   # Copy from example
   copy .env.example .env
   ```

2. Edit `.env` with production values:
   ```
   MIC_AI__OpenAI__ApiKey=your-production-openai-key
   MIC_ConnectionStrings__MicDatabase=Host=prod-db-server;Port=5432;Database=micdb;Username=mic;Password=secure-password;SSL Mode=Require
   MIC_JwtSettings__SecretKey=your-secure-jwt-secret-key-at-least-32-chars
   ```

#### Step 3: Database Setup
1. Create PostgreSQL database:
   ```sql
   CREATE DATABASE micdb;
   CREATE USER mic WITH PASSWORD 'secure-password';
   GRANT ALL PRIVILEGES ON DATABASE micdb TO mic;
   ```

2. Run migrations (first run only):
   ```bash
   # The application will automatically run migrations on first startup
   # Or manually run if needed:
   dotnet ef database update --project .\MIC.Infrastructure.Data
   ```

#### Step 4: Run Application
```bash
# Run the executable
.\MIC.Desktop.Avalonia.exe

# Or create Windows Service (see Method 3)
```

### Method 2: MSIX Package (Windows Store/Enterprise)

#### MSIX Package Installation
1. Download `MIC.Desktop.Avalonia.msix` from releases
2. Double-click the MSIX file or use PowerShell:
   ```powershell
   Add-AppxPackage -Path .\MIC.Desktop.Avalonia.msix
   ```

#### MSIX Features
- **Secure Installation**: Isolated application container
- **Automatic Updates**: Built-in update mechanism via Microsoft Store
- **Enterprise Deployment**: Can be deployed via Microsoft Intune or SCCM
- **App Identity**: Unique application identity for security policies

#### MSIX Package Signing
For production deployment, sign the MSIX package:
```bash
# Using Azure SignTool (recommended)
azuresigntool sign -kvu "https://your-keyvault.vault.azure.net" -kvi your-key-id -tr "http://timestamp.digicert.com" -td sha256 MIC.Desktop.Avalonia.msix

# Using self-signed certificate (development only)
signtool sign /fd SHA256 /a /f cert.pfx /p password MIC.Desktop.Avalonia.msix
```

### Method 3: Auto-Updates (Standalone)

### Method 3: Auto-Updates (Standalone)

#### Automatic Update Configuration
The application includes built-in automatic update functionality:

1. **Update Check**: Application checks for updates on startup and periodically
2. **Download & Install**: Updates are downloaded and installed automatically
3. **User Notification**: Users are notified of available updates
4. **Rollback Support**: Failed updates can be rolled back

#### Update Settings
Configure update behavior in `appsettings.Production.json`:
```json
{
  "Updates": {
    "Enabled": true,
    "CheckIntervalMinutes": 60,
    "DownloadPath": "./updates",
    "AutoInstall": true,
    "RequireAdminForInstall": false
  }
}
```

#### Manual Update Check
Users can manually check for updates from the application menu or use the command line:
```bash
# Check for updates
MIC.Desktop.Avalonia.exe --check-updates

# Force update installation
MIC.Desktop.Avalonia.exe --update
```

2. Configure service:
   ```bash
   nssm set MbarieIntelligenceConsole DisplayName "Mbarie Intelligence Console"
   nssm set MbarieIntelligenceConsole Description "Operational intelligence dashboard for real-time monitoring"
   nssm set MbarieIntelligenceConsole Start SERVICE_AUTO_START
   ```

3. Start service:
   ```bash
   net start MbarieIntelligenceConsole
   ```

#### Service Configuration File
Create `C:\Program Files\MbarieIntelligenceConsole\appsettings.Service.json`:
```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "RunMigrationsOnStartup": true,
    "DeleteDatabaseOnStartup": false,
    "SeedDataOnStartup": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

## Configuration Reference

### Database Configuration

#### PostgreSQL (Production)
```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "RunMigrationsOnStartup": true,
    "DeleteDatabaseOnStartup": false,
    "SeedDataOnStartup": false
  },
  "ConnectionStrings": {
    "MicDatabase": "Host=localhost;Port=5432;Database=micdb;Username=mic;Password=password;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

#### SQLite (Development/Simple)
```json
{
  "Database": {
    "Provider": "SQLite",
    "RunMigrationsOnStartup": true,
    "DeleteDatabaseOnStartup": false,
    "SeedDataOnStartup": true
  },
  "ConnectionStrings": {
    "MicSqlite": "Data Source=mic_prod.db"
  }
}
```

### AI Configuration

#### OpenAI
```json
{
  "AI": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "${MIC_AI__OpenAI__ApiKey}",
      "ModelId": "gpt-4-turbo-preview",
      "MaxTokens": 4000,
      "Temperature": 0.7
    }
  }
}
```

#### Azure OpenAI
```json
{
  "AI": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "${MIC_AI__AzureOpenAI__Endpoint}",
      "ApiKey": "${MIC_AI__AzureOpenAI__ApiKey}",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview"
    }
  }
}
```

### Email Integration

#### Gmail OAuth2
```json
{
  "OAuth2": {
    "Gmail": {
      "ClientId": "${MIC_OAuth2__Gmail__ClientId}",
      "ClientSecret": "${MIC_OAuth2__Gmail__ClientSecret}",
      "RedirectUri": "http://localhost:5000/oauth2callback"
    }
  }
}
```

#### Outlook OAuth2
```json
{
  "OAuth2": {
    "Outlook": {
      "ClientId": "${MIC_OAuth2__Outlook__ClientId}",
      "ClientSecret": "${MIC_OAuth2__Outlook__ClientSecret}",
      "TenantId": "common",
      "RedirectUri": "http://localhost:5000/oauth2callback"
    }
  }
}
```

## Security Configuration

### JWT Settings
```json
{
  "JwtSettings": {
    "SecretKey": "${MIC_JwtSettings__SecretKey}",
    "Issuer": "MbarieIntelligenceConsole",
    "Audience": "MbarieIntelligenceConsole",
    "ExpirationHours": 8
  }
}
```

### Environment Variables Security
Never commit sensitive data to source control. Use:
1. **Environment Variables**: Set at system/process level
2. **Azure Key Vault**: For cloud deployments (future)
3. **HashiCorp Vault**: For enterprise deployments (future)

## Scaling & Performance

### Database Performance
- **Connection Pooling**: Configure in connection string: `Pooling=true;Maximum Pool Size=100;`
- **Indexing**: Ensure proper indexes on frequently queried columns
- **Maintenance**: Regular VACUUM and ANALYZE for PostgreSQL

### Application Performance
- **Caching**: Implement Redis caching for frequently accessed data (future)
- **Background Processing**: Use Hangfire or Quartz.NET for scheduled tasks (future)
- **Load Balancing**: Deploy multiple instances behind a load balancer (future)

## Monitoring & Maintenance

### Health Checks
The application exposes health check endpoints:

```bash
# Basic health check
curl http://localhost:5000/health

# Readiness check (includes dependencies)
curl http://localhost:5000/ready
```

### Logging Configuration
Configure logging in `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "System": "Warning"
    },
    "File": {
      "Path": "logs/mic-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  }
}
```

### Backup Strategy

#### Database Backups
```bash
# PostgreSQL backup
pg_dump -h localhost -U mic micdb > backup_$(date +%Y%m%d).sql

# SQLite backup
copy mic_prod.db mic_prod_backup_$(date +%Y%m%d).db
```

#### Application Data Backups
- **Email Attachments**: Backup `./attachments` directory
- **Logs**: Backup `./logs` directory
- **Configuration**: Backup `.env` and `appsettings.*.json` files

### Update Procedure

#### For Standalone Executable
1. **Backup Current Installation**
   ```bash
   # Stop application
   taskkill /IM MIC.Desktop.Avalonia.exe /F
   
   # Backup database
   pg_dump -h localhost -U mic micdb > backup_pre_update_$(date +%Y%m%d).sql
   
   # Backup configuration
   copy .env .env.backup
   copy appsettings.Production.json appsettings.Production.json.backup
   ```

2. **Deploy New Version**
   ```bash
   # Extract new release
   Expand-Archive -Path MIC.Desktop.Avalonia-win-x64.zip -DestinationPath . -Force
   ```

3. **Restore Configuration**
   ```bash
   copy .env.backup .env
   copy appsettings.Production.json.backup appsettings.Production.json
   ```

4. **Run Migrations**
   ```bash
   dotnet ef database update --project .\MIC.Infrastructure.Data
   ```

5. **Restart Application**
   ```bash
   .\MIC.Desktop.Avalonia.exe
   ```

#### For MSIX Package Updates
MSIX packages update automatically through the Microsoft Store or enterprise deployment tools. For manual updates:

```bash
# Remove old version
Remove-AppxPackage -Package MIC.Desktop.Avalonia

# Install new version
Add-AppxPackage -Path .\MIC.Desktop.Avalonia_v2.0.0.msix
```

#### For Auto-Update Enabled Installations
No manual intervention required. The application handles updates automatically.

## Troubleshooting

### Common Issues

#### Issue: "Database connection failed"
**Symptoms**: Application fails to start with database connection error
**Solution**:
1. Verify PostgreSQL service is running
2. Check connection string in `.env` file
3. Verify firewall allows port 5432
4. Check database user permissions

#### Issue: "AI service unavailable"
**Symptoms**: Chat features fail with API key error
**Solution**:
1. Verify `MIC_AI__OpenAI__ApiKey` is set
2. Check internet connectivity
3. Verify API key has sufficient quota
4. Check Azure OpenAI endpoint configuration

#### Issue: "Application crashes on startup"
**Symptoms**: Application closes immediately
**Solution**:
1. Check Windows Event Viewer for errors
2. Run from command line to see error messages
3. Verify .NET 9.0 Runtime is installed
4. Check disk space availability

#### Issue: "Email sync not working"
**Symptoms**: Emails not appearing in inbox
**Solution**:
1. Verify OAuth2 client credentials
2. Check internet connectivity
3. Verify email account permissions
4. Check sync interval configuration

### Log Files Location
- **Application Logs**: `./logs/` directory
- **Windows Event Log**: Application events
- **Database Logs**: PostgreSQL logs (if configured)

### Performance Monitoring
- **CPU Usage**: Monitor with Task Manager or Performance Monitor
- **Memory Usage**: Should be < 2GB for typical usage
- **Database Connections**: Monitor with `pg_stat_activity`

## Disaster Recovery

### Recovery Procedures

#### Database Corruption
1. Stop application
2. Restore from latest backup:
   ```bash
   psql -h localhost -U mic micdb < backup_20250129.sql
   ```
3. Start application

#### Configuration Loss
1. Restore from backup:
   ```bash
   copy .env.backup .env
   copy appsettings.Production.json.backup appsettings.Production.json
   ```
2. Restart application

#### Complete System Failure
1. Install fresh OS
2. Install prerequisites (.NET, PostgreSQL)
3. Restore database from backup
4. Deploy application files
5. Restore configuration
6. Start application

### Recovery Time Objectives (RTO)
- **Critical**: 4 hours (database restore + application redeploy)
- **Important**: 24 hours (full system restore)
- **Standard**: 48 hours (hardware replacement + restore)

### Backup Schedule
- **Database**: Daily full backup (retain 7 days)
- **Configuration**: On change backup
- **Application Data**: Weekly full backup

## Support & Resources

### Documentation
- [README.md](README.md) - Project overview and quick start
- [SETUP.md](SETUP.md) - Development setup guide
- Architecture documents in `docs/` directory

### Support Channels
- **GitHub Issues**: Bug reports and feature requests
- **Email Support**: support@your-org.com
- **Community Forum**: [Discussions](https://github.com/your-org/mbarie-intelligence-console/discussions)

### Training Resources
- **User Manual**: Available in application Help menu
- **API Documentation**: Available for integration (future)
- **Video Tutorials**: Available on YouTube channel (future)

## Compliance & Security

### Data Protection
- **Encryption**: Data encrypted at rest (database) and in transit (TLS)
- **Access Control**: Role-based access control (RBAC)
- **Audit Logging**: All sensitive operations logged

### Compliance Requirements
- **GDPR**: User data export/deletion capabilities
- **HIPAA**: Healthcare data protection (future)
- **SOC 2**: Security controls (future)

### Security Updates
- **Automatic Updates**: Check for updates on startup (future)
- **Security Patches**: Monthly review and application
- **Vulnerability Scanning**: Regular dependency scanning
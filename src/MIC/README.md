# Mbarie Intelligence Console

Operational intelligence dashboard for real-time monitoring and AI-powered insights.

## Quick Start

### Prerequisites
- .NET 9 SDK
- PostgreSQL 15+ (or SQLite for local dev)
- OpenAI API key (optional for AI features)

### Local Development

1. Clone the repository:
   ```bash
   git clone https://github.com/your-org/mbarie-intelligence-console.git
   cd mbarie-intelligence-console/src/MIC
   ```

2. Configure environment variables:
   ```bash
   # Copy the example environment file
   copy .env.example .env
   # Edit .env with your actual values
   ```

3. Set up user secrets (for development):
   ```bash
   dotnet user-secrets init -p .\MIC.Desktop.Avalonia\MIC.Desktop.Avalonia.csproj
   dotnet user-secrets set "AI:OpenAI:ApiKey" "your-openai-api-key" -p .\MIC.Desktop.Avalonia\MIC.Desktop.Avalonia.csproj
   ```

4. Run the application:
   ```bash
   dotnet run --project .\MIC.Desktop.Avalonia
   ```

5. Login with default credentials:
   - Username: `admin`
   - Password: `Admin@123`

### Build & Deploy

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed deployment instructions.

### Testing

```bash
# Run unit tests
dotnet test MIC.Tests.Unit

# Run integration tests
dotnet test MIC.Tests.Integration

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture

### High-Level Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    MIC.Desktop.Avalonia                     │
│  (Avalonia UI Layer - Views, ViewModels, Services)         │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                 MIC.Core.Application                        │
│  (Application Logic - Commands, Queries, Handlers)         │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    MIC.Core.Domain                          │
│  (Domain Models - Entities, Value Objects, Domain Events)  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│              MIC.Infrastructure.*                           │
│  (Data, Identity, AI, Monitoring Implementation)           │
└─────────────────────────────────────────────────────────────┘
```

### Key Design Patterns
- **Clean Architecture**: Separation of concerns with dependency inversion
- **CQRS**: Separate command and query responsibilities using MediatR
- **Repository Pattern**: Abstract data access layer
- **Dependency Injection**: Built on Microsoft.Extensions.DependencyInjection
- **Error Handling**: Using ErrorOr pattern for functional error handling

## Features

### Core Features
- **Real-time Monitoring**: Dashboard with operational metrics and alerts
- **AI-Powered Insights**: Email analysis, chat assistant, predictions
- **Email Integration**: Sync with Gmail/Outlook, intelligent prioritization
- **Alert Management**: Create, view, and manage intelligence alerts
- **User Authentication**: JWT-based authentication with role-based access

### AI Capabilities
- **Email Analysis**: Priority detection, sentiment analysis, action item extraction
- **Chat Assistant**: Context-aware conversations with business data
- **Predictive Analytics**: Trend analysis and anomaly detection
- **Automated Summarization**: Brief summaries of emails and reports

## Configuration

### Environment Variables
Key environment variables (see `.env.example` for complete list):
```
# AI Configuration
MIC_AI__OpenAI__ApiKey=your-openai-api-key
MIC_AI__AzureOpenAI__Endpoint=your-azure-endpoint
MIC_AI__AzureOpenAI__ApiKey=your-azure-key

# Database
MIC_ConnectionStrings__MicDatabase=Host=localhost;Port=5432;Database=micdb;Username=postgres;Password=password

# OAuth2 (Email Integration)
MIC_OAuth2__Gmail__ClientId=your-gmail-client-id
MIC_OAuth2__Gmail__ClientSecret=your-gmail-client-secret
MIC_OAuth2__Outlook__ClientId=your-outlook-client-id
MIC_OAuth2__Outlook__ClientSecret=your-outlook-client-secret

# JWT Settings
MIC_JwtSettings__SecretKey=your-jwt-secret-key
```

### Configuration Files
- `appsettings.json`: Base configuration (committed)
- `appsettings.Development.json`: Development overrides (committed)
- `appsettings.Production.json`: Production overrides (committed)
- User Secrets: Development-only secrets (not committed)

## Development

### Project Structure
```
MIC/
├── MIC.Desktop.Avalonia/          # Avalonia UI application
├── MIC.Core.Application/          # Application layer (CQRS)
├── MIC.Core.Domain/              # Domain layer (entities, aggregates)
├── MIC.Core.Intelligence/        # Intelligence services
├── MIC.Infrastructure.AI/        # AI service implementations
├── MIC.Infrastructure.Data/      # Data persistence (EF Core)
├── MIC.Infrastructure.Identity/  # Authentication & authorization
├── MIC.Infrastructure.Monitoring/# Telemetry and monitoring
├── MIC.Tests.Unit/               # Unit tests
├── MIC.Tests.Integration/        # Integration tests
└── MIC.Tests.E2E/                # End-to-end tests
```

### Adding New Features
1. **Domain Layer**: Add entities in `MIC.Core.Domain/Entities/`
2. **Application Layer**: Add commands/queries in `MIC.Core.Application/Features/`
3. **Infrastructure**: Implement repositories/services in appropriate infrastructure project
4. **UI Layer**: Add views/viewmodels in `MIC.Desktop.Avalonia/`

### Testing Strategy
- **Unit Tests**: Test individual components in isolation (handlers, services)
- **Integration Tests**: Test with real database using Testcontainers
- **E2E Tests**: Test complete user workflows
- **Code Coverage**: Target ≥ 70% code coverage

## Security

### Authentication & Authorization
- JWT-based authentication with configurable expiration
- Password hashing using Argon2id
- Environment-based secret management
- No hard-coded credentials in source code

### Security Best Practices
- **Secrets Management**: Use environment variables or user secrets
- **Input Validation**: Validate all user inputs
- **SQL Injection Protection**: Use EF Core parameterized queries
- **XSS Protection**: Avalonia provides built-in protection

## Deployment

### CI/CD Pipeline
GitHub Actions workflow automates:
- Build on every push
- Run tests with code coverage
- Publish artifacts on release tags
- Create GitHub releases

### Deployment Options
1. **Standalone Executable**: Self-contained .NET publish
2. **Installer**: Windows installer using Inno Setup
3. **Container**: Docker container (future)

### Health Checks
- Database connectivity check
- AI service availability
- Authentication service status

## Monitoring & Observability

### Logging
- Structured logging with Serilog (configuration pending)
- Console and file output
- Log levels configurable per environment

### Health Checks
- `/health` endpoint for application health
- `/ready` endpoint for readiness checks
- Database connectivity monitoring

## Contributing

### Development Workflow
1. Fork the repository
2. Create a feature branch
3. Make changes with tests
4. Run test suite
5. Submit pull request

### Code Style
- Follow C# coding conventions
- Use meaningful variable names
- Add XML documentation for public APIs
- Write unit tests for new features

### Commit Messages
Follow Conventional Commits format:
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `test:` Test changes
- `refactor:` Code refactoring
- `chore:` Maintenance tasks

## License

[Add your license here]

## Support

- **Documentation**: [docs/](docs/)
- **Issue Tracker**: [GitHub Issues](https://github.com/your-org/mbarie-intelligence-console/issues)
- **Discussion**: [GitHub Discussions](https://github.com/your-org/mbarie-intelligence-console/discussions)

## Acknowledgments

- Built with [Avalonia UI](https://avaloniaui.net/)
- AI powered by [OpenAI](https://openai.com/) / [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
- Icons from [Material Design Icons](https://materialdesignicons.com/)
- Database by [PostgreSQL](https://www.postgresql.org/) / [SQLite](https://www.sqlite.org/)
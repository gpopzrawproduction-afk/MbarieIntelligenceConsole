using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Win32;
using MIC.Core.Application;
using MIC.Core.Application.Common.Interfaces;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Desktop.Avalonia.Services;
using MIC.Infrastructure.AI;
using MIC.Infrastructure.Data;
using MIC.Infrastructure.Identity;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Serilog;
using Serilog.Events;

namespace MIC.Desktop.Avalonia
{
    internal static class Program
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

        var configuration = BuildConfiguration();
        ConfigureSerilog(configuration);

        try
        {
            Log.Information("Program.Main started");

            var services = new ServiceCollection();
            ConfigureServices(services, configuration);
            ServiceProvider = services.BuildServiceProvider();

            Log.Information("Initializing database...");
            try
            {
                InitializeDatabaseAsync(ServiceProvider).GetAwaiter().GetResult();
                Log.Information("Database initialized");
            }
            catch (Exception dbEx)
            {
                Log.Error(dbEx, "Database initialization failed, but continuing startup");
                // Continue without database - app may show error dialog later
            }

            var app = BuildAvaloniaApp();
            Log.Information("Starting Avalonia desktop lifetime");
            app.StartWithClassicDesktopLifetime(args);
            Log.Information("Application shutdown");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error during startup");
            // Try to show a message box if possible
            try
            {
                // Use Avalonia's message box if available
                // For now, just log and rethrow
            }
            catch { }
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    private static IConfiguration BuildConfiguration()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
            
        builder.AddEnvironmentVariables();
        
        // Note: User secrets are not used in this project
        // Use environment variables or .env files for development secrets
        
        
        return builder.Build();
    }

    private static void ConfigureSerilog(IConfiguration configuration)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MIC",
            "logs");
        Directory.CreateDirectory(basePath);
        var logPath = Path.Combine(basePath, "mic-.log");

        var minimumLevel = env.Equals("Development", StringComparison.OrdinalIgnoreCase)
            ? LogEventLevel.Debug
            : LogEventLevel.Information;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();

        Log.Information("Serilog configured for {Environment}", env);
    }

    private static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        // Register MediatR first to ensure IMediator is available
        // Register MediatR to scan all necessary assemblies for handlers
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(MIC.Core.Application.DependencyInjection));
            cfg.RegisterServicesFromAssemblyContaining(typeof(MIC.Infrastructure.Data.DependencyInjection));
            cfg.RegisterServicesFromAssemblyContaining(typeof(MIC.Infrastructure.AI.DependencyInjection));
            cfg.RegisterServicesFromAssemblyContaining(typeof(MIC.Infrastructure.Identity.IdentityDependencyInjection)); // Correct class name
            cfg.RegisterServicesFromAssemblyContaining(typeof(DashboardViewModel)); // Include view models
        });
        
        services.AddApplication();                 // from MIC.Core.Application
        services.AddDataInfrastructure(configuration); // from MIC.Infrastructure.Data
        services.AddAIServices(configuration);         // from MIC.Infrastructure.AI
        services.AddIdentityInfrastructure();          // from MIC.Infrastructure.Identity

        // Register session service for desktop application (single shared instance)
        var sessionService = UserSessionService.Instance;
        services.AddSingleton<ISessionService>(sessionService);
        services.AddSingleton(sessionService);

        // Option A: secrets stored locally (DPAPI) and surfaced through a provider.
        services.AddSingleton<ISecretProvider, SecretProvider>();

        // Register NavigationService
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<LoginViewModel>();
        
        // Register all ViewModels that are used in navigation
        services.AddTransient<AlertListViewModel>();
        services.AddTransient<AlertDetailsViewModel>();
        services.AddTransient<MetricsDashboardViewModel>();
        services.AddTransient<PredictionsViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<EmailInboxViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CreateAlertViewModel>();
        services.AddTransient<UserProfileViewModel>();
        services.AddTransient<CommandPaletteViewModel>();
        services.AddTransient<AddEmailAccountViewModel>(); // Added for email account setup

        // Add logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        return services;
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
            await initializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            throw;
        }
    }
    }
}

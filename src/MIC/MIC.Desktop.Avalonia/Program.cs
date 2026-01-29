using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Win32;
using MIC.Core.Application;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Infrastructure.AI;
using MIC.Infrastructure.Data;
using MIC.Infrastructure.Identity;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Data.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MIC.Desktop.Avalonia
{
    internal static class Program
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("[STARTUP] Program.Main() called");
        Console.WriteLine("Building configuration...");
        
        var configuration = BuildConfiguration();
        Console.WriteLine("Configuration built");
        
        Console.WriteLine("Configuring services...");
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        Console.WriteLine("Services configured");
        
        Console.WriteLine("Building service provider...");
        ServiceProvider = services.BuildServiceProvider();
        Console.WriteLine("Service provider built");

        Console.WriteLine("Initializing database...");
        InitializeDatabaseAsync(ServiceProvider).GetAwaiter().GetResult();
        Console.WriteLine("Database initialized");
        
        Console.WriteLine("[STARTUP] Building app...");
        var app = BuildAvaloniaApp();
        
        Console.WriteLine("[STARTUP] Starting app with classic desktop lifetime...");
        app.StartWithClassicDesktopLifetime(args);
        Console.WriteLine("[SHUTDOWN] Program.Main() exiting");
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        Console.WriteLine("[STARTUP] BuildAvaloniaApp() called");
        
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
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
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

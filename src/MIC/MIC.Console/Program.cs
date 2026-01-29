using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MIC.Infrastructure.Data;
using MIC.Infrastructure.Data.Persistence;
using MIC.Core.Application;
using MIC.Core.Intelligence;
using MIC.Infrastructure.AI;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Validate AI Configuration
ValidateAIConfiguration(configuration);

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddApplication();
services.AddDataInfrastructure(configuration);
services.AddAIServices(configuration); // Add AI services with configuration
services.AddIntelligenceLayer(); // Add intelligence layer services

var provider = services.BuildServiceProvider();

Console.WriteLine("Console startup OK.");

private static void ValidateAIConfiguration(IConfiguration configuration)
{
    var apiKey = configuration["AI:OpenAI:ApiKey"]
                 ?? Environment.GetEnvironmentVariable("MIC_AI__OpenAI__ApiKey");

    if (string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine("⚠️  WARNING: OpenAI API key not configured");
        Console.WriteLine("   AI features will be disabled");
        Console.WriteLine("   Set MIC_AI__OpenAI__ApiKey environment variable");
        Console.WriteLine();
    }
    else
    {
        Console.WriteLine("✓ OpenAI configured successfully");
    }
}

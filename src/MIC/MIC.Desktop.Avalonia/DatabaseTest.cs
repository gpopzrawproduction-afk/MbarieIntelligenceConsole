using System;
using System.Threading.Tasks;
using MediatR;
using MIC.Core.Application.Alerts.Commands.CreateAlert;
using MIC.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace MIC.Desktop.Avalonia;

/// <summary>
/// Simple database and CQRS test for verification
/// </summary>
public static class DatabaseTest
{
    public static async Task RunAsync(System.IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CreateAlertCommand(
            "System Test Alert",
            "Verifying CQRS pipeline and database persistence",
            AlertSeverity.Info,
            "Automated Test");

        var result = await mediator.Send(command);

        if (result.IsError)
        {
            Console.WriteLine($"? CQRS Test Failed: {result.FirstError.Description}");
        }
        else
        {
            Console.WriteLine($"? CQRS Test Passed - Alert created with ID: {result.Value}");
        }
    }
}

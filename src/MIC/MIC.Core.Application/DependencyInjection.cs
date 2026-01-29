using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MIC.Core.Application;

/// <summary>
/// Dependency injection registration for application services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application layer services
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

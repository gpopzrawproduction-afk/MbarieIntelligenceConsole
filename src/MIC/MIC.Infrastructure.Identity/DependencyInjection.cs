using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MIC.Core.Application.Authentication;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Configuration;
using MIC.Infrastructure.Identity.Services;

namespace MIC.Infrastructure.Identity;

public static class IdentityDependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        // Register JWT settings from configuration
        services.AddOptions<JwtSettings>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("JwtSettings").Bind(settings);
            });

        // Register services
        services.AddScoped<IEmailOAuth2Service, EmailOAuth2Service>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<JwtSettings>>();
            var settings = options.Value;
            
            // Use a fallback secret key if not configured (for development only)
            var secretKey = settings.SecretKey;
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                secretKey = "dev-secret-key-change-in-production-1234567890";
            }
            
            return new JwtTokenService(secretKey, TimeSpan.FromHours(settings.ExpirationHours));
        });
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return services;
    }
}

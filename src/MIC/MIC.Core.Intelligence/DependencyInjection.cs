using Microsoft.Extensions.DependencyInjection;

namespace MIC.Core.Intelligence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIntelligenceLayer(this IServiceCollection services)
        {
            services.AddScoped<PositionQuestionnaireService>();
            services.AddScoped<IntelligenceProcessor>();
            
            return services;
        }
    }
}

using ErrorOr;
using MediatR;
using MIC.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace MIC.Core.Application.Settings.Queries.GetSettings;

public class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, ErrorOr<AppSettings>>
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<GetSettingsQueryHandler> _logger;

    public GetSettingsQueryHandler(
        ISettingsService settingsService,
        ILogger<GetSettingsQueryHandler> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<ErrorOr<AppSettings>> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Loading settings for user {UserId}", request.UserId);
            
            // Load user-specific settings
            var settings = await _settingsService.LoadUserSettingsAsync(request.UserId);
            
            if (settings == null)
            {
                _logger.LogWarning("No settings found for user {UserId}, returning defaults", request.UserId);
                return new AppSettings(); // Return default settings
            }
            
            _logger.LogInformation("Settings loaded successfully for user {UserId}", request.UserId);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings for user {UserId}", request.UserId);
            return Error.Failure("Settings.LoadFailed", ex.Message);
        }
    }
}
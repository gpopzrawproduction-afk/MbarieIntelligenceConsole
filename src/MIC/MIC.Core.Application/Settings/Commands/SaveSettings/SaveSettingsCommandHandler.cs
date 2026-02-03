using ErrorOr;
using MediatR;
using MIC.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace MIC.Core.Application.Settings.Commands.SaveSettings;

public class SaveSettingsCommandHandler : ICommandHandler<SaveSettingsCommand, bool>
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SaveSettingsCommandHandler> _logger;

    public SaveSettingsCommandHandler(
        ISettingsService settingsService,
        ILogger<SaveSettingsCommandHandler> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(SaveSettingsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Saving settings for user {UserId}", request.UserId);
            
            // Save user-specific settings to database
            await _settingsService.SaveUserSettingsAsync(request.UserId, request.Settings);
            
            // Also save to local app data for desktop persistence
            await _settingsService.SaveSettingsAsync(request.Settings);
            
            _logger.LogInformation("Settings saved successfully for user {UserId}", request.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings for user {UserId}", request.UserId);
            return Error.Failure("Settings.SaveFailed", ex.Message);
        }
    }
}
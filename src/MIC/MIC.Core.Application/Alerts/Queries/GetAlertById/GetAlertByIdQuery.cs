using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application.Alerts.Queries.GetAlertById;

/// <summary>
/// Query to retrieve a single alert by its ID.
/// </summary>
public record GetAlertByIdQuery(Guid AlertId) : IQuery<AlertDto>;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MIC.Core.Application.Alerts.Common;
using MIC.Core.Application.Metrics.Common;

namespace MIC.Desktop.Avalonia.Services;

/// <summary>
/// Export service for generating reports in various formats.
/// </summary>
public class ExportService
{
    private static ExportService? _instance;
    public static ExportService Instance => _instance ??= new ExportService();

    private readonly string _exportDirectory;

    public ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _exportDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MIC Exports");
        
        Directory.CreateDirectory(_exportDirectory);
    }

    #region CSV Export

    /// <summary>
    /// Exports alerts to a CSV file.
    /// </summary>
    public async Task<string> ExportAlertsToCsvAsync(IEnumerable<AlertDto> alerts, string? filename = null)
    {
        filename ??= $"alerts_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var filepath = Path.Combine(_exportDirectory, filename);

        var sb = new StringBuilder();
        sb.AppendLine("ID,Alert Name,Description,Severity,Status,Source,Triggered At,Created At");

        foreach (var alert in alerts)
        {
            sb.AppendLine($"\"{alert.Id}\",\"{EscapeCsv(alert.AlertName)}\",\"{EscapeCsv(alert.Description)}\",\"{alert.Severity}\",\"{alert.Status}\",\"{EscapeCsv(alert.Source)}\",\"{alert.TriggeredAt:yyyy-MM-dd HH:mm:ss}\",\"{alert.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        await File.WriteAllTextAsync(filepath, sb.ToString());
        
        NotificationService.Instance.ShowSuccess($"Exported to {filename}", "Export Complete");
        return filepath;
    }

    /// <summary>
    /// Exports metrics to a CSV file.
    /// </summary>
    public async Task<string> ExportMetricsToCsvAsync(IEnumerable<MetricDto> metrics, string? filename = null)
    {
        filename ??= $"metrics_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var filepath = Path.Combine(_exportDirectory, filename);

        var sb = new StringBuilder();
        sb.AppendLine("ID,Name,Category,Value,Unit,Timestamp");

        foreach (var metric in metrics)
        {
            sb.AppendLine($"\"{metric.Id}\",\"{EscapeCsv(metric.MetricName)}\",\"{EscapeCsv(metric.Category)}\",{metric.Value},\"{EscapeCsv(metric.Unit)}\",\"{metric.Timestamp:yyyy-MM-dd HH:mm:ss}\"");
        }

        await File.WriteAllTextAsync(filepath, sb.ToString());
        
        NotificationService.Instance.ShowSuccess($"Exported to {filename}", "Export Complete");
        return filepath;
    }

    #endregion

    #region PDF Export

    /// <summary>
    /// Exports predictions to a PDF file.
    /// </summary>
    public async Task<string> ExportPredictionsToPdfAsync(IEnumerable<PredictionExportRow> predictions, string? filename = null)
    {
        filename ??= $"predictions_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filepath = Path.Combine(_exportDirectory, filename);

        var rows = predictions.ToList();

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text("Mbarie Intelligence Console - Predictions")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Metric");
                            header.Cell().Element(CellStyle).Text("Current");
                            header.Cell().Element(CellStyle).Text("Predicted");
                            header.Cell().Element(CellStyle).Text("Change %");
                            header.Cell().Element(CellStyle).Text("Confidence");
                            header.Cell().Element(CellStyle).Text("Timeframe");
                        });

                        foreach (var row in rows)
                        {
                            table.Cell().Element(RowStyle).Text(row.MetricName);
                            table.Cell().Element(RowStyle).Text(row.CurrentValue.ToString("N2"));
                            table.Cell().Element(RowStyle).Text(row.PredictedValue.ToString("N2"));
                            table.Cell().Element(RowStyle).Text($"{row.ChangePercent:+0.0;-0.0;0.0}%");
                            table.Cell().Element(RowStyle).Text($"{row.Confidence * 100:F0}%");
                            table.Cell().Element(RowStyle).Text(row.TimeFrame);
                        }

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold())
                                .PaddingVertical(6)
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2);
                        }

                        static IContainer RowStyle(IContainer container)
                        {
                            return container.PaddingVertical(4)
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten4);
                        }
                    });

                    page.Footer()
                        .AlignRight()
                        .Text($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf(filepath);
        });

        NotificationService.Instance.ShowSuccess($"Exported to {filename}", "Export Complete");
        return filepath;
    }

    #endregion

    #region JSON Export

    /// <summary>
    /// Exports data to a JSON file.
    /// </summary>
    public async Task<string> ExportToJsonAsync<T>(IEnumerable<T> data, string name, string? filename = null)
    {
        filename ??= $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var filepath = Path.Combine(_exportDirectory, filename);

        var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filepath, json);
        
        NotificationService.Instance.ShowSuccess($"Exported to {filename}", "Export Complete");
        return filepath;
    }

    #endregion

    #region Report Generation

    /// <summary>
    /// Generates a summary report in HTML format.
    /// </summary>
    public async Task<string> GenerateHtmlReportAsync(
        IEnumerable<AlertDto> alerts,
        IEnumerable<MetricDto> metrics,
        string? filename = null)
    {
        filename ??= $"report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        var filepath = Path.Combine(_exportDirectory, filename);

        var alertList = alerts.ToList();
        var metricList = metrics.ToList();

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>MIC Report - {DateTime.Now:yyyy-MM-dd}</title>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; background: #f5f5f5; }}
        .container {{ max-width: 1200px; margin: 0 auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #0B0C10; border-bottom: 3px solid #00E5FF; padding-bottom: 10px; }}
        h2 {{ color: #333; margin-top: 30px; }}
        .summary {{ display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin: 20px 0; }}
        .card {{ background: linear-gradient(135deg, #0D1117 0%, #1a1f2e 100%); color: white; padding: 20px; border-radius: 8px; text-align: center; }}
        .card h3 {{ margin: 0 0 10px 0; font-size: 14px; opacity: 0.8; }}
        .card .value {{ font-size: 32px; font-weight: bold; color: #00E5FF; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }}
        th {{ background: #0D1117; color: white; }}
        tr:hover {{ background: #f5f5f5; }}
        .critical {{ color: #FF0055; font-weight: bold; }}
        .warning {{ color: #FF9500; }}
        .info {{ color: #00E5FF; }}
        .footer {{ margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>?? Mbarie Intelligence Console Report</h1>
        <p>Generated: {DateTime.Now:MMMM dd, yyyy at HH:mm}</p>
        
        <div class='summary'>
            <div class='card'>
                <h3>TOTAL ALERTS</h3>
                <div class='value'>{alertList.Count}</div>
            </div>
            <div class='card'>
                <h3>CRITICAL</h3>
                <div class='value' style='color: #FF0055;'>{alertList.Count(a => a.Severity == Core.Domain.Entities.AlertSeverity.Critical)}</div>
            </div>
            <div class='card'>
                <h3>ACTIVE</h3>
                <div class='value'>{alertList.Count(a => a.Status != Core.Domain.Entities.AlertStatus.Resolved)}</div>
            </div>
            <div class='card'>
                <h3>METRICS</h3>
                <div class='value'>{metricList.Count}</div>
            </div>
        </div>

        <h2>?? Recent Alerts</h2>
        <table>
            <tr>
                <th>Alert</th>
                <th>Severity</th>
                <th>Status</th>
                <th>Source</th>
                <th>Time</th>
            </tr>
            {string.Join("\n", alertList.Take(20).Select(a => $@"
            <tr>
                <td>{System.Net.WebUtility.HtmlEncode(a.AlertName)}</td>
                <td class='{a.Severity.ToString().ToLower()}'>{a.Severity}</td>
                <td>{a.Status}</td>
                <td>{System.Net.WebUtility.HtmlEncode(a.Source)}</td>
                <td>{a.TriggeredAt:MMM dd HH:mm}</td>
            </tr>"))}
        </table>

        <h2>?? Key Metrics</h2>
        <table>
            <tr>
                <th>Metric</th>
                <th>Category</th>
                <th>Value</th>
                <th>Unit</th>
                <th>Recorded</th>
            </tr>
            {string.Join("\n", metricList.Take(20).Select(m => $@"
            <tr>
                <td>{System.Net.WebUtility.HtmlEncode(m.MetricName)}</td>
                <td>{System.Net.WebUtility.HtmlEncode(m.Category)}</td>
                <td><strong>{m.Value:N2}</strong></td>
                <td>{System.Net.WebUtility.HtmlEncode(m.Unit)}</td>
                <td>{m.Timestamp:MMM dd HH:mm}</td>
            </tr>"))}
        </table>

        <div class='footer'>
            <p>� {DateTime.Now.Year} Mbarie Intelligence Console. All rights reserved.</p>
            <p>This report was automatically generated by MIC v1.0</p>
        </div>
    </div>
</body>
</html>";

        await File.WriteAllTextAsync(filepath, html);
        
        NotificationService.Instance.ShowSuccess($"Report saved to {filename}", "Report Generated");
        return filepath;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Opens the export directory in file explorer.
    /// </summary>
    public void OpenExportDirectory()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _exportDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Opening export directory");
        }
    }

    /// <summary>
    /// Opens a specific file in the default application.
    /// </summary>
    public void OpenFile(string filepath)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filepath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorHandlingService.Instance.HandleException(ex, "Opening file");
        }
    }

    public string ExportDirectory => _exportDirectory;

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\"", "\"\"");
    }

    #endregion
}

public record PredictionExportRow(
    string MetricName,
    double CurrentValue,
    double PredictedValue,
    double ChangePercent,
    double Confidence,
    string Direction,
    string TimeFrame);

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using MIC.Core.Application.Common.Interfaces;
using ReactiveUI;
using System.Reactive.Linq;
using MIC.Core.Application.KnowledgeBase.Commands.UploadDocument;

namespace MIC.Desktop.Avalonia.ViewModels;

/// <summary>
/// Minimal knowledge base view model that wires up placeholder commands and data.
/// </summary>
public class KnowledgeBaseViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ISessionService _sessionService;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<KnowledgeBaseViewModel> _logger;

    public KnowledgeBaseViewModel(
        IMediator mediator,
        ISessionService sessionService,
        IKnowledgeBaseService knowledgeBaseService,
        ILogger<KnowledgeBaseViewModel> logger)
    {
        _mediator = mediator;
        _sessionService = sessionService;
        _knowledgeBaseService = knowledgeBaseService;
        _logger = logger;

        UploadDocumentsCommand = ReactiveCommand.CreateFromTask(UploadDocumentAsync);
        LoadEntriesCommand = ReactiveCommand.CreateFromTask(LoadDocumentsAsync);
        SearchCommand = ReactiveCommand.CreateFromTask<string>(PerformSearchAsync);

        // Set up reactive search with debouncing
        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Where(searchText => !string.IsNullOrWhiteSpace(searchText))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async searchText =>
            {
                await PerformSearchAsync(searchText);
            });
    }

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> UploadDocumentsCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> LoadEntriesCommand { get; }
    public ReactiveCommand<string, System.Reactive.Unit> SearchCommand { get; }

    private ObservableCollection<KnowledgeEntryViewModel> _entries = new();
    public ObservableCollection<KnowledgeEntryViewModel> Entries
    {
        get => _entries;
        set => this.RaiseAndSetIfChanged(ref _entries, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private string _successMessage = string.Empty;
    public string SuccessMessage
    {
        get => _successMessage;
        set => this.RaiseAndSetIfChanged(ref _successMessage, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    private async Task UploadDocumentAsync()
    {
        try
        {
            // Get the main window as the parent for the file picker
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow == null)
            {
                ErrorMessage = "Cannot open file picker - main window not available";
                return;
            }

            // Open file picker
            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Documents to Upload",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("All Documents")
                    {
                        Patterns = new[] { "*.pdf", "*.docx", "*.txt", "*.md", "*.xlsx", "*.pptx", "*.doc", "*.csv" }
                    },
                    new FilePickerFileType("PDF Documents")
                    {
                        Patterns = new[] { "*.pdf" }
                    },
                    new FilePickerFileType("Word Documents")
                    {
                        Patterns = new[] { "*.docx", "*.doc" }
                    },
                    new FilePickerFileType("Text Files")
                    {
                        Patterns = new[] { "*.txt", "*.md" }
                    },
                    new FilePickerFileType("Spreadsheets")
                    {
                        Patterns = new[] { "*.xlsx", "*.csv" }
                    }
                }
            });

            if (files.Count == 0) return;

            // Validate file count
            if (files.Count > _maxFiles)
            {
                ErrorMessage = $"Maximum {_maxFiles} files allowed. You selected {files.Count} files.";
                return;
            }

            IsLoading = true;
            SuccessMessage = string.Empty;
            ErrorMessage = string.Empty;

            int uploadedCount = 0;
            var uploadErrors = new List<string>();
            long totalSize = 0L;

            foreach (var file in files)
            {
                try
                {
                    // Check file size
                    var properties = await file.GetBasicPropertiesAsync();
                    var fileSize = (long)(properties?.Size ?? 0);

                    if (fileSize > _maxFileSizeBytes)
                    {
                        uploadErrors.Add($"{file.Name}: File too large (max 150MB, actual: {fileSize / 1024 / 1024}MB)");
                        continue;
                    }

                    totalSize += fileSize;

                    _logger.LogInformation("Uploading document: {FileName}, Size: {FileSize}MB", 
                        file.Name, fileSize / 1024 / 1024);

                    // Read file content
                    await using var stream = await file.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var content = memoryStream.ToArray();

                    // Upload to knowledge base service
                    var command = new UploadDocumentCommand
                    {
                        FileName = file.Name,
                        Content = content,
                        FileSize = fileSize,
                        ContentType = GetContentType(file.Name),
                        UserId = _sessionService.GetUser()?.Id ?? Guid.Empty
                    };

                    var result = await _mediator.Send(command);

                    if (result.IsSuccess)
                    {
                        uploadedCount++;
                        _logger.LogInformation("Document uploaded successfully: {FileName}", file.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to upload {FileName}: {Error}", file.Name, result.Error);
                        uploadErrors.Add($"{file.Name}: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading {FileName}", file.Name);
                    uploadErrors.Add($"{file.Name}: {ex.Message}");
                }
            }

            IsLoading = false;

            // Show results
            if (uploadedCount > 0)
            {
                SuccessMessage = $"✅ {uploadedCount} of {files.Count} document(s) uploaded successfully ({totalSize / 1024 / 1024}MB total)";
                await LoadDocumentsAsync();
            }

            if (uploadErrors.Any())
            {
                ErrorMessage = "⚠️ Some uploads failed:\n" + string.Join("\n", uploadErrors.Take(5));
                if (uploadErrors.Count > 5)
                {
                    ErrorMessage += $"\n... and {uploadErrors.Count - 5} more errors";
                }
            }

            if (uploadedCount == 0 && uploadErrors.Any())
            {
                ErrorMessage = "❌ All uploads failed. Check file sizes and formats.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in document upload");
            ErrorMessage = "❌ Failed to upload documents: " + ex.Message;
            IsLoading = false;
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".csv" => "text/csv",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }

    private const int _maxFiles = 50;
    private const long _maxFileSizeBytes = 150 * 1024 * 1024; // 150MB

    public async Task LoadDocumentsAsync()
    {
        try
        {
            IsLoading = true;
            var user = _sessionService.GetUser();
            if (user.Id == Guid.Empty)
            {
                Entries.Clear();
                return;
            }

            var results = await _knowledgeBaseService.SearchAsync("", user.Id);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Entries.Clear();
                foreach (var entry in results)
                {
                    Entries.Add(new KnowledgeEntryViewModel
                    {
                        Title = entry.Title,
                        Content = entry.Content,
                        Tags = string.Join(", ", entry.Tags),
                        CreatedAt = entry.CreatedAt
                    });
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load knowledge base documents");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task PerformSearchAsync(string searchText)
    {
        try
        {
            IsLoading = true;
            var user = _sessionService.GetUser();
            if (user.Id == Guid.Empty)
            {
                Entries.Clear();
                return;
            }

            var results = await _knowledgeBaseService.SearchAsync(searchText, user.Id);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Entries.Clear();
                foreach (var entry in results)
                {
                    Entries.Add(new KnowledgeEntryViewModel
                    {
                        Title = entry.Title,
                        Content = entry.Content,
                        Tags = string.Join(", ", entry.Tags),
                        CreatedAt = entry.CreatedAt
                    });
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search knowledge base documents: {SearchText}", searchText);
        }
        finally
        {
            IsLoading = false;
        }
    }

}

public class KnowledgeEntryViewModel : ViewModelBase
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
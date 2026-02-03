using System.Text.Json;
using Santy.Core.Models;
using Santy.Core.Services;
using Santy.Web.Models;

namespace Santy.Web.Services;

public class SantyOperationsService
{
    private readonly SettingsService _settingsService;
    private readonly AppStateService _stateService;
    private readonly ILogger<SantyOperationsService> _logger;

    public SantyOperationsService(
        SettingsService settingsService,
        AppStateService stateService,
        ILogger<SantyOperationsService> logger)
    {
        _settingsService = settingsService;
        _stateService = stateService;
        _logger = logger;
    }

    public async Task ScanLocalAsync()
    {
        if (_stateService.IsScanRunning)
        {
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                _stateService.IsScanRunning = true;
                _stateService.CurrentOperation = "Scanning local files...";
                _stateService.LastError = null;
                _stateService.NotifyStateChanged();

                var settings = _settingsService.LoadSettings();
                
                if (string.IsNullOrWhiteSpace(settings.LocalRootPath))
                {
                    throw new InvalidOperationException("Local root path is not configured. Please set it in Settings.");
                }

                if (!Directory.Exists(settings.LocalRootPath))
                {
                    throw new DirectoryNotFoundException($"Local root path does not exist: {settings.LocalRootPath}");
                }

                _logger.LogInformation("Starting local scan of {Path}", settings.LocalRootPath);

                var scanner = new LocalScanner();
                var files = scanner.Scan(settings.LocalRootPath);

                _logger.LogInformation("Scanned {Count} files", files.Count);

                using var store = new SqliteStore(settings.DatabasePath);
                store.Initialize();
                store.ReplaceLocalFiles(files);

                _stateService.ScanInfo = new ScanInfo
                {
                    LastScanTime = DateTime.UtcNow,
                    FilesScanned = files.Count
                };

                _logger.LogInformation("Scan completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scan operation");
                _stateService.LastError = ex.Message;
            }
            finally
            {
                _stateService.IsScanRunning = false;
                _stateService.CurrentOperation = null;
                _stateService.NotifyStateChanged();
            }
        });
    }

    public async Task ImportRemoteAsync(string jsonlContent)
    {
        if (_stateService.IsImportRunning)
        {
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                _stateService.IsImportRunning = true;
                _stateService.CurrentOperation = "Importing remote assets...";
                _stateService.LastError = null;
                _stateService.NotifyStateChanged();

                var settings = _settingsService.LoadSettings();

                _logger.LogInformation("Starting remote import for source {Source}", settings.SourceName);

                var assets = ParseJsonLines(jsonlContent, settings.SourceName);

                _logger.LogInformation("Parsed {Count} assets", assets.Count);

                using var store = new SqliteStore(settings.DatabasePath);
                store.Initialize();
                store.UpsertRemoteAssets(assets);

                _stateService.ImportInfo = new ImportInfo
                {
                    LastImportTime = DateTime.UtcNow,
                    AssetsImported = assets.Count
                };

                _logger.LogInformation("Import completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during import operation");
                _stateService.LastError = ex.Message;
            }
            finally
            {
                _stateService.IsImportRunning = false;
                _stateService.CurrentOperation = null;
                _stateService.NotifyStateChanged();
            }
        });
    }

    public async Task RunDiffAsync()
    {
        if (_stateService.IsDiffRunning)
        {
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                _stateService.IsDiffRunning = true;
                _stateService.CurrentOperation = "Computing diff...";
                _stateService.LastError = null;
                _stateService.NotifyStateChanged();

                var settings = _settingsService.LoadSettings();

                _logger.LogInformation("Starting diff for source {Source}", settings.SourceName);

                using var store = new SqliteStore(settings.DatabasePath);
                store.Initialize();

                var remoteAssets = store.LoadRemoteAssets(settings.SourceName);
                var localFiles = store.LoadLocalFiles();

                _logger.LogInformation("Loaded {RemoteCount} remote assets and {LocalCount} local files",
                    remoteAssets.Count, localFiles.Count);

                var diffEngine = new DiffEngine();
                var results = diffEngine.ComputeDiff(remoteAssets, localFiles);

                store.SaveDiffResults(results);

                var missing = results.Count(r => r.Status == DiffStatus.Missing);
                var present = results.Count(r => r.Status == DiffStatus.Present);
                var uncertain = results.Count(r => r.Status == DiffStatus.Uncertain);

                _stateService.DiffInfo = new DiffInfo
                {
                    LastDiffTime = DateTime.UtcNow,
                    MissingCount = missing,
                    PresentCount = present,
                    UncertainCount = uncertain,
                    Results = results.ToList()
                };

                _logger.LogInformation("Diff completed: Present={Present}, Missing={Missing}, Uncertain={Uncertain}",
                    present, missing, uncertain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during diff operation");
                _stateService.LastError = ex.Message;
            }
            finally
            {
                _stateService.IsDiffRunning = false;
                _stateService.CurrentOperation = null;
                _stateService.NotifyStateChanged();
            }
        });
    }

    private List<RemoteAsset> ParseJsonLines(string content, string sourceName)
    {
        var assets = new List<RemoteAsset>();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var asset = JsonSerializer.Deserialize<RemoteAsset>(line, options);
                if (asset != null)
                {
                    // Ensure source name is set
                    if (string.IsNullOrEmpty(asset.SourceName))
                    {
                        asset = asset with { SourceName = sourceName };
                    }
                    assets.Add(asset);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse line: {Line}", line);
            }
        }

        return assets;
    }
}

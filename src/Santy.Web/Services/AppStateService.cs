using Santy.Web.Models;

namespace Santy.Web.Services;

public class AppStateService
{
    public ScanInfo ScanInfo { get; set; } = new();
    public ImportInfo ImportInfo { get; set; } = new();
    public DiffInfo DiffInfo { get; set; } = new();
    
    public bool IsScanRunning { get; set; }
    public bool IsImportRunning { get; set; }
    public bool IsDiffRunning { get; set; }
    
    public string? CurrentOperation { get; set; }
    public string? LastError { get; set; }

    public event Action? OnChange;

    public void NotifyStateChanged() => OnChange?.Invoke();
}

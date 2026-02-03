# Santy.Web - Blazor Server UI

This is a Blazor Server application providing a web-based interface for Santy backup management operations.

## Features

### 1. Settings Page (`/settings`)
Configure application settings persisted to `santy-settings.json`:
- **Database Path**: Location of SQLite database (default: `./santy.db`)
- **Source Name**: Name of remote backup source (default: `icloud`)
- **Local Root Path**: Directory to scan for local backup files

Settings are auto-saved and persist across sessions.

### 2. Scan Page (`/scan`)
Inventory local backup directory:
- Click "Scan Local Files" to start scanning
- Displays last scan time and file count
- Runs in background to keep UI responsive
- Shows progress status during operation
- Error handling with clear feedback

### 3. Import Page (`/import`)
Import remote asset inventory:
- Drag-and-drop or click to select JSONL file
- Shows file name and size preview
- Click "Import Assets" to process
- Displays import count and timestamp
- Supports files up to 10MB

JSONL Format:
```jsonl
{"sourceName":"icloud","sourceAssetId":"IMG001","filename":"photo.jpg","sizeBytes":2048576,"createdUtc":"2025-07-15T14:30:22Z","mediaType":"image/jpeg"}
{"sourceName":"icloud","sourceAssetId":"IMG002","filename":"video.mp4","sizeBytes":52428800}
```

### 4. Diff Analysis Page (`/diff`)
Compare remote and local inventories:
- Click "Run Diff" to compute differences
- View summary statistics:
  - **Present**: Files found in backup
  - **Missing**: Files not found locally
  - **Uncertain**: Ambiguous matches (multiple candidates)
- Filter results by status using tabs
- Export results as JSONL file

Columns:
- **Asset ID**: Remote asset identifier
- **Filename**: File name/path
- **Status**: Present / Missing / Uncertain
- **Reason**: Explanation of match result
- **Matched Path**: Local file path (if matched)

## Architecture

### Services

#### SettingsService
Manages application configuration:
- Loads/saves settings from `santy-settings.json`
- Provides caching for performance
- Settings are plain JSON (no secrets)

#### AppStateService
Maintains UI state:
- Tracks running operations (scan, import, diff)
- Stores operation results
- Provides notifications for state changes
- Observable by all pages via dependency injection

#### SantyOperationsService
Executes background operations:
- **ScanLocalAsync**: Scans directory using LocalScanner
- **ImportRemoteAsync**: Parses JSONL and stores in database
- **RunDiffAsync**: Computes diff and saves results
- All operations run asynchronously to keep UI responsive
- Comprehensive error handling and logging

#### FileLoggerProvider
Custom logging provider:
- Writes logs to `santy-web.log` in root directory
- Thread-safe file operations
- Appends to log file (preserves history)
- Filters by log level

### Pages

All pages use Blazor components with:
- Auto-refresh on state changes via `AppStateService.OnChange` event
- Proper cleanup in `Dispose()` 
- Async operations that don't block UI
- Error display and status messages

## Dependency Injection

Services registered in `Program.cs`:

```csharp
builder.Services.AddSingleton<SettingsService>();      // Single instance
builder.Services.AddSingleton<AppStateService>();      // Single instance
builder.Services.AddScoped<SantyOperationsService>();  // Per-request instance
```

## Logging

Logs are written to `santy-web.log` with format:
```
[2026-02-03 14:30:45] [Information] Santy.Web.Services.SantyOperationsService: Starting local scan of /home/user/backup
[2026-02-03 14:30:47] [Information] Santy.Web.Services.SantyOperationsService: Scanned 1250 files
[2026-02-03 14:30:48] [Information] Santy.Web.Services.SantyOperationsService: Scan completed successfully
```

## Running the Web UI

```bash
# Build
dotnet build

# Run
dotnet run --project src/Santy.Web

# Navigate to https://localhost:5001
```

## JavaScript Integration

File `wwwroot/santy.js` provides utilities:
- `downloadFile(filename, content)`: Download JSONL reports to browser

## Styling

- **Modern minimal design** with professional color scheme
- **Responsive layout** with sidebar navigation
- **Status indicators** for operations (running, error, success)
- **Color-coded badges** for diff results (green/red/yellow)
- **Accessible UI** with semantic HTML

## Data Flow

```
Settings Page → SettingsService → santy-settings.json
                     ↓
              AppStateService

Scan Page → SantyOperationsService → LocalScanner → SqliteStore
                                                   ↓
                                          remote_assets table

Import Page → SantyOperationsService → JsonParser → SqliteStore
                                                   ↓
                                          local_files table

Diff Page → SantyOperationsService → DiffEngine → SqliteStore
                                                 ↓
                                        diff_results table
```

## Error Handling

All operations include:
- Try-catch blocks
- Logging of exceptions
- User-friendly error messages
- Graceful degradation (operations continue even if one fails)

## Future Enhancements

- Real-time progress bars for long operations
- Email notifications for completed operations
- Dark mode toggle
- Advanced search/filtering in results
- Conflict resolution UI for uncertain matches
- Scheduling for automatic scans/imports
- Multi-user support with authentication

## Tips

1. **Configure settings first** before running any operations
2. **Use local paths** that you have read access to
3. **Keep JSONL files small** for better performance
4. **Check logs** (`santy-web.log`) for detailed operation info
5. **Export results** regularly for backup and analysis

## Troubleshooting

### Page won't load
- Check browser console for errors (F12)
- Verify .NET runtime is installed (`dotnet --version`)
- Clear browser cache and reload

### Settings not saving
- Check file permissions on `santy-settings.json`
- Verify application has write access to directory
- Check logs for errors

### Scan fails
- Ensure local root path is set in Settings
- Verify path exists and is readable
- Check logs for file access errors
- Look for inaccessible subdirectories

### Import fails
- Validate JSONL format (one JSON object per line)
- Check file size (max 10MB)
- Ensure all required fields are present
- Check logs for parsing errors

### Diff takes too long
- Large inventories (>100K files) may take time
- Check CPU usage during operation
- Consider breaking into smaller batches
- Verify database indexes are present

## Project Structure

```
src/Santy.Web/
├── Components/
│   ├── Layout/
│   │   └── MainLayout.razor       # Navigation sidebar
│   ├── Pages/
│   │   ├── Home.razor             # Welcome page
│   │   ├── Settings.razor         # Configuration
│   │   ├── Scan.razor             # Local scanning
│   │   ├── Import.razor           # JSONL upload
│   │   ├── Diff.razor             # Diff analysis
│   │   ├── Error.razor            # Error page
│   │   └── _Imports.razor         # Global imports
│   ├── App.razor                  # Root component
│   └── Routes.razor               # Route definitions
├── Models/
│   ├── AppSettings.cs             # Configuration model
│   ├── ScanInfo.cs                # Scan state
│   ├── ImportInfo.cs              # Import state
│   └── DiffInfo.cs                # Diff state
├── Services/
│   ├── SettingsService.cs         # Settings persistence
│   ├── AppStateService.cs         # Application state
│   ├── SantyOperationsService.cs  # Core operations
│   └── FileLoggerProvider.cs      # File logging
├── wwwroot/
│   ├── app.css                    # Global styles
│   └── santy.js                   # JavaScript utilities
├── Properties/
│   └── launchSettings.json        # Launch configuration
├── appsettings.json               # App configuration
├── appsettings.Development.json   # Dev configuration
├── Program.cs                     # Startup code
└── Santy.Web.csproj              # Project file
```

## References

- [Blazor Server Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [Blazor Forms & Validation](https://docs.microsoft.com/en-us/aspnet/core/blazor/forms-validation)
- [Blazor Components](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/)
- [Blazor JavaScript Interop](https://docs.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/)

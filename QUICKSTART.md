# Quick Start Guide

Get Santy v0.1 running in 5 minutes.

## 1. Clone and Navigate

```bash
cd santy-cloud-backup-mananger
```

## 2. Build the Solution

```bash
dotnet build Santy.sln
```

## 3. Choose Your Interface

### Option A: Web UI (Recommended for First Time)

```bash
dotnet run --project src/Santy.Web
```

Then open: **http://localhost:5000**

### Option B: Command Line

```bash
# Scan local directory
dotnet run --project src/Santy.Cli -- scan-local --root ~/Downloads --db ./santy.db

# Import remote inventory (create test file first)
echo '{"sourceAssetId":"photo1","filename":"vacation.jpg","sizeBytes":2048000,"createdUtc":"2024-01-15T10:30:00Z"}' > remote.jsonl
dotnet run --project src/Santy.Cli -- import-remote --source "Test" --file remote.jsonl --db ./santy.db

# Run diff analysis
dotnet run --project src/Santy.Cli -- diff --source "Test" --db ./santy.db

# Generate report
dotnet run --project src/Santy.Cli -- report --source "Test" --db ./santy.db --out report.jsonl
```

## 4. Run Tests

```bash
dotnet test Santy.sln
```

Expected: **8 passed, 0 failed**

## Web UI Workflow

1. **Settings** → Configure database path, source name, local directory
2. **Scan Local** → Click scan button (takes a few seconds for large directories)
3. **Import** → Upload a JSONL file with remote asset list
4. **Diff** → Click run diff to compare and see results
5. **Export** → Save results to JSONL

## File Formats

### Remote Assets (JSONL)

```jsonl
{"sourceAssetId":"id123","filename":"photo.jpg","sizeBytes":2048000,"createdUtc":"2024-01-15T10:30:00Z"}
{"sourceAssetId":"id124","filename":"video.mp4","sizeBytes":1073741824,"createdUtc":"2024-01-16T14:22:00Z"}
```

### Report Output (JSONL)

```jsonl
{"sourceName":"iCloud","sourceAssetId":"id123","status":"present","reason":"Exact match by asset ID","matchedLocalPath":"/home/user/Photos/photo.jpg"}
{"sourceName":"iCloud","sourceAssetId":"id124","status":"missing","reason":"No suitable match found","matchedLocalPath":null}
```

## Project Structure

```
src/
  Santy.Core/      ← Business logic (models, services)
  Santy.Cli/       ← Command-line interface
  Santy.Web/       ← Blazor Server web UI
tests/
  Santy.Core.Tests/ ← Unit tests (DiffEngine)
docs/             ← Architecture and guides
```

## Common Tasks

### Clear Database and Start Fresh
```bash
rm santy.db santy-settings.json santy-web.log 2>/dev/null
```

### View Web UI Logs
```bash
tail -f santy-web.log
```

### Test Specific Component
```bash
# Test diff engine only
dotnet test --filter "DiffEngine"

# Build only core library
dotnet build src/Santy.Core/Santy.Core.csproj
```

## What Each Project Does

- **Santy.Core**: Handles scanning, database storage, and diff analysis
- **Santy.Cli**: Command-line tools for scripts and automation
- **Santy.Web**: User-friendly dashboard with forms and visualizations

All are pre-configured with logging and error handling.

## Typical Results

After running a diff:
- **Present** (✓): Files exist in both local and remote
- **Missing** (✗): Files in remote but not found locally  
- **Uncertain** (?): Possible matches but not confident enough

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Build fails | Run `dotnet restore` |
| Web won't start | Check port 5000/5001 not in use |
| Settings not saving | Verify write permissions in current directory |
| Tests fail | Run `dotnet clean && dotnet build` |
| Logs missing | Check working directory contains `santy-web.log` |

## Next: Deep Dive

- Full docs: See `docs/ARCHITECTURE.md`
- Setup guide: See `docs/SETUP_AND_TESTING.md`
- Implementation details: See `docs/IMPLEMENTATION.md`

**Ready to go!** 🚀

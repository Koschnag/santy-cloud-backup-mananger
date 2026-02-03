# CLI Commands

## Scan Local Files

Scan a local directory and store file inventory in the database:

```bash
dotnet run --project src/Santy.Cli -- scan-local --root /path/to/backup --db santy.db
```

## Import Remote Assets

Import remote asset inventory from a JSONL file:

```bash
dotnet run --project src/Santy.Cli -- import-remote --source icloud --file inventory.jsonl --db santy.db
```

## Compute Diff

Compare remote assets with local files:

```bash
dotnet run --project src/Santy.Cli -- diff --source icloud --db santy.db
```

## Generate Report

Export diff results to a JSONL report file:

```bash
dotnet run --project src/Santy.Cli -- report --source icloud --db santy.db --out report.jsonl
```

## Complete Workflow

```bash
# 1. Scan local backup directory
dotnet run --project src/Santy.Cli -- scan-local --root ~/backups/icloud --db santy.db

# 2. Import remote inventory
dotnet run --project src/Santy.Cli -- import-remote --source icloud --file icloud-inventory.jsonl --db santy.db

# 3. Compute diff
dotnet run --project src/Santy.Cli -- diff --source icloud --db santy.db

# 4. Generate report
dotnet run --project src/Santy.Cli -- report --source icloud --db santy.db --out diff-report.jsonl
```

## JSONL Format

Input format for remote assets:

```jsonl
{"sourceName":"icloud","sourceAssetId":"ABC123","filename":"photo.jpg","sizeBytes":1024000,"createdUtc":"2026-01-15T10:30:00Z","mediaType":"image/jpeg"}
{"sourceName":"icloud","sourceAssetId":"DEF456","filename":"video.mp4","sizeBytes":50240000,"createdUtc":"2026-01-16T14:22:00Z","mediaType":"video/mp4"}
```

## Report Output

Report lines with diff results:

```jsonl
{"sourceName":"icloud","sourceAssetId":"ABC123","status":"Present","reason":"exact match by asset ID prefix","matchedLocalPath":"/backups/icloud/ABC123_photo.jpg"}
{"sourceName":"icloud","sourceAssetId":"DEF456","status":"Missing","reason":"no matching local file found","matchedLocalPath":null}
```

### Status Values

- **Present**: File found locally and matched
- **Missing**: File not found locally
- **Uncertain**: Multiple candidate matches found

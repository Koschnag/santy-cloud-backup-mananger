# Santy v0.1 Architecture

## Overview

Santy is a backup management system designed to inventory, compare, and verify cloud backup completeness. Version 0.1 focuses on the inventory and diff phases.

## Component Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Santy.Cli                            │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────┐ │
│  │scan-local│  │import-   │  │   diff   │  │ report │ │
│  │          │  │remote    │  │          │  │        │ │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └───┬────┘ │
└───────┼─────────────┼─────────────┼─────────────┼──────┘
        │             │             │             │
        └─────────────┴─────────────┴─────────────┘
                      │
        ┌─────────────▼──────────────────────────────────┐
        │            Santy.Core                          │
        │  ┌────────────────┐  ┌──────────────────────┐ │
        │  │ Domain Models  │  │     Services         │ │
        │  │                │  │                      │ │
        │  │ • RemoteAsset  │  │ • LocalScanner       │ │
        │  │ • LocalFile    │  │ • SqliteStore        │ │
        │  │ • DiffResult   │  │ • DiffEngine         │ │
        │  │ • DiffStatus   │  │                      │ │
        │  └────────────────┘  └──────────────────────┘ │
        └────────────────────────────────────────────────┘
                      │
        ┌─────────────▼──────────────────┐
        │      SQLite Database           │
        │  • remote_assets               │
        │  • local_files                 │
        │  • diff_results                │
        │  • jobs                        │
        └────────────────────────────────┘
```

## Domain Models

### RemoteAsset
Represents a file from a remote source (e.g., iCloud, OneDrive).

- **SourceName**: Identifier for the remote source
- **SourceAssetId**: Unique ID from the remote system
- **Filename**: Original filename
- **SizeBytes**: File size in bytes
- **CreatedUtc**: Creation timestamp (optional)
- **MediaType**: MIME type (optional)

### LocalFile
Represents a scanned local file.

- **FullPath**: Absolute path on local filesystem
- **Filename**: File basename
- **SizeBytes**: File size in bytes
- **LastWriteUtc**: Last modification timestamp

### DiffResult
Result of comparing a remote asset with local files.

- **SourceName**: Remote source identifier
- **SourceAssetId**: Asset ID from remote
- **Status**: DiffStatus enum (Missing/Present/Uncertain)
- **Reason**: Human-readable explanation
- **MatchedLocalPath**: Path to matched file (if found)

### DiffStatus
Enumeration of diff states:
- **Missing**: No matching local file
- **Present**: File found and matched
- **Uncertain**: Multiple ambiguous candidates

## Services

### LocalScanner
Recursively scans a directory tree and catalogs all files.

**Features:**
- Recursive directory traversal
- Filters out junk files (.DS_Store, thumbs.db, ._* files)
- Captures file metadata (size, timestamps)
- Handles access errors gracefully

**API:**
```csharp
IReadOnlyList<LocalFile> Scan(string rootPath)
```

### SqliteStore
Manages persistence of all data in a single SQLite database.

**Schema:**
- `remote_assets`: Inventory from remote sources
- `local_files`: Snapshot of local file system
- `diff_results`: Comparison results
- `jobs`: Execution tracking (minimal in v0.1)

**Key Methods:**
- `Initialize()`: Create schema if not exists
- `UpsertRemoteAssets()`: Insert/update remote inventory
- `ReplaceLocalFiles()`: Overwrite local snapshot
- `SaveDiffResults()`: Store diff results
- `LoadRemoteAssets()`: Query by source name
- `LoadLocalFiles()`: Load entire local snapshot
- `LoadLatestDiffResults()`: Query diff results

### DiffEngine
Matches remote assets to local files using configurable rules.

**Matching Algorithm:**

1. **Exact Match by Asset ID Prefix** (Priority 1)
   - If local filename starts with `<SourceAssetId>_`, it's an exact match
   - Example: `IMG001_beach.jpg` matches asset ID `IMG001`

2. **Match by Filename + Size** (Priority 2)
   - Compare filename (case-insensitive) and size
   - If exactly one match → Present
   - If multiple matches → Uncertain
   - If no matches → Missing

**API:**
```csharp
IReadOnlyList<DiffResult> ComputeDiff(
    IEnumerable<RemoteAsset> remoteAssets,
    IEnumerable<LocalFile> localFiles)
```

## CLI Commands

### scan-local
Scans local directory and stores inventory in database.

```bash
scan-local --root <path> --db <dbpath>
```

### import-remote
Imports remote asset inventory from JSONL file.

```bash
import-remote --source <name> --file <jsonl> --db <dbpath>
```

### diff
Computes diff between remote and local inventories.

```bash
diff --source <name> --db <dbpath>
```

### report
Exports diff results to JSONL report file.

```bash
report --source <name> --db <dbpath> --out <report.jsonl>
```

## Data Flow

### Typical Usage Workflow

```
1. External Tool → Generate remote inventory JSONL
                    (e.g., pyicloud script)
                    
2. Santy CLI → import-remote
                ↓
            SQLite (remote_assets table)

3. Santy CLI → scan-local
                ↓
            SQLite (local_files table)

4. Santy CLI → diff
                ↓
            SQLite (diff_results table)

5. Santy CLI → report
                ↓
            Output JSONL report file
```

## File Formats

### JSONL (JSON Lines)
All import/export uses newline-delimited JSON.

- One JSON object per line
- No enclosing array brackets
- Each line independently parseable
- Robust against partial failures

**Example:**
```jsonl
{"sourceName":"icloud","sourceAssetId":"A1","filename":"photo.jpg","sizeBytes":1024}
{"sourceName":"icloud","sourceAssetId":"A2","filename":"video.mp4","sizeBytes":5120}
```

## Testing Strategy

### Unit Tests (Santy.Core.Tests)

- **DiffEngineTests**: Comprehensive coverage of matching rules
  - Exact match scenarios
  - Filename+size matching
  - Ambiguous cases
  - Case sensitivity
  - Priority rules

### Future Testing
- Integration tests for database operations
- CLI command end-to-end tests
- Performance tests for large inventories

## Design Decisions

### Why SQLite?
- Single-file database (easy backup/portability)
- Zero configuration
- Supports structured queries
- Good performance for local workloads
- .NET has excellent support via Microsoft.Data.Sqlite

### Why JSONL over JSON arrays?
- Streamable (no need to load entire file)
- Resilient to partial corruption
- Easy to append/inspect
- Standard format for log-structured data

### Why Central Package Management?
- Consistent versions across all projects
- Easier dependency updates
- Better tooling support in modern .NET

### Why File Scanning vs. inotify?
- Simplicity for v0.1 MVP
- Cross-platform compatibility
- Stateless operation (no daemon required)
- Future: Can add watch mode

## Future Enhancements (v0.2+)

1. **Transfer Phase**: Actually copy missing files
2. **Verify Phase**: Checksum validation
3. **Incremental Updates**: Track what changed since last run
4. **Multi-cloud Support**: OneDrive, Google Drive adapters
5. **Web UI**: Blazor dashboard for visualization
6. **Conflict Resolution**: Handle uncertain matches interactively
7. **Deduplication**: Detect duplicate files by content hash
8. **Compression**: Optional compression for archive storage

## Dependencies

- **.NET 8**: Modern C# features, performance
- **Microsoft.Data.Sqlite**: Database access
- **System.CommandLine**: CLI parsing (beta)
- **xUnit**: Test framework

## Performance Considerations

### v0.1 Scope
- Expected: <100K files per inventory
- Database size: ~10-50 MB typical
- Diff computation: O(n*m) worst case, O(n) typical with indexing

### Optimization Strategies
- SQLite indexes on filename+size
- Lookup structures in DiffEngine
- Streaming JSONL parsing
- Batched database writes

## Security Considerations

- Database files contain file paths (consider encryption for sensitive data)
- No network operations in v0.1
- File system permissions respected
- Future: Add option for hashed filenames in reports

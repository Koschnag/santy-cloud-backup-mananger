# Santy Core Tests

Unit tests for Santy.Core library using xUnit.

## Running Tests

```bash
dotnet test
```

## Test Coverage

- **DiffEngineTests**: Comprehensive tests for diff matching logic
  - Exact match by asset ID prefix
  - Match by filename and size
  - Multiple candidates (uncertain)
  - No match (missing)
  - Case insensitive matching
  - Priority rules

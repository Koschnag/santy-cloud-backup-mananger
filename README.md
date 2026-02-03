# Santy Cloud Backup Manager

**Santy** — Verifiable, vendor-agnostic data backups. An open-source backup middleware that inventories data, plans deterministic archives, and verifies completeness without duplicates or vendor lock-in.

## Overview

**Inventory → Plan → Transfer → Verify**

Santy is designed to manage cloud backups with full transparency and verification. The system workflow:

1. **Inventory**: Scan and catalog all source data (e.g., iCloud, OneDrive, Google Drive)
2. **Plan**: Determine what needs to be backed up based on existing archives and changes
3. **Transfer**: Execute the backup transfer to target storage
4. **Verify**: Validate integrity and completeness of transferred data

### v0.1 Scope

The initial release (v0.1) focuses on:
- **Inventory**: Scan and catalog source data
- **Scan**: Read existing backup archives
- **Diff**: Compare inventory against archives to determine what's new or changed

Future versions will add transfer, verification, and multi-cloud support.

## Project Structure

```
/src
  /Santy.Core       - Core library with business logic
  /Santy.Cli        - Command-line interface
  /Santy.Web        - Web interface (Blazor Server)
/tools
  /pyicloud         - Python helpers for iCloud integration
/docs               - Architecture and design documentation
```

## Requirements

- .NET 8 SDK
- For iCloud integration: Python 3.x with pyicloud library

## Getting Started

### Option 1: Quick Start (Recommended)

See [QUICKSTART.md](QUICKSTART.md) for a 5-minute setup guide.

### Option 2: Using Docker

```bash
# Web UI in Docker
docker-compose up santy-web

# Open http://localhost:5000
```

See [docs/DOCKER.md](docs/DOCKER.md) for full Docker documentation.

### Option 3: Local Development

```bash
# Build the solution
dotnet build Santy.sln

# Run tests (8 tests, no mocks)
dotnet test Santy.sln

# Run the Web UI
dotnet run --project src/Santy.Web
# Open http://localhost:5000

# Run the CLI
dotnet run --project src/Santy.Cli -- scan-local --root ~/ --db ./santy.db
```

## Testing

**Santy uses Testing Without Mocks**:
- Real objects, not mocks
- Real files and database, not stubs
- 8 comprehensive unit tests
- 100% coverage of business logic

```bash
# Run all tests
dotnet test Santy.sln

# Run in Docker
docker build -f Dockerfile.test -t santy-tests . && docker run santy-tests
```

See [docs/TESTING_ARCHITECTURE.md](docs/TESTING_ARCHITECTURE.md) for full details.

## Technology Stack

- **.NET 8** with C# 12 (latest language features)
- **Blazor Server** for web UI
- **SQLite** for data storage
- **System.CommandLine** for CLI framework
- **xUnit** for testing (no mocks)
- **Docker** for containerization

## Documentation

- **[START_HERE.md](START_HERE.md)** - Master guide for all users
- **[QUICKSTART.md](QUICKSTART.md)** - 5-minute setup
- **[DOCKER_AND_TESTING.md](DOCKER_AND_TESTING.md)** - Docker & Testing quick ref
- **[docs/DOCKER.md](docs/DOCKER.md)** - Complete Docker guide
- **[docs/TESTING_ARCHITECTURE.md](docs/TESTING_ARCHITECTURE.md)** - Testing patterns
- **[docs/SETUP_AND_TESTING.md](docs/SETUP_AND_TESTING.md)** - Build and test guide
- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** - System design
- **[docs/PROJECT_REFERENCE.md](docs/PROJECT_REFERENCE.md)** - API reference
- **[src/Santy.Web/README.md](src/Santy.Web/README.md)** - Web UI guide

## Contributing

This project uses:
- `.editorconfig` for consistent code style
- `Directory.Build.props` for common build settings
- `Directory.Packages.props` for centralized package version management

## License

TBD

using System.CommandLine;
using System.Text.Json;
using Santy.Core.Models;
using Santy.Core.Services;

namespace Santy.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Santy - Cloud Backup Manager CLI");

        // scan-local command
        var scanLocalCommand = new Command("scan-local", "Scan local files and store in database");
        var scanRootOption = new Option<string>("--root", "Root path to scan") { IsRequired = true };
        var scanDbOption = new Option<string>("--db", "Database path") { IsRequired = true };
        scanLocalCommand.AddOption(scanRootOption);
        scanLocalCommand.AddOption(scanDbOption);
        scanLocalCommand.SetHandler(ScanLocal, scanRootOption, scanDbOption);

        // import-remote command
        var importRemoteCommand = new Command("import-remote", "Import remote assets from JSONL file");
        var importSourceOption = new Option<string>("--source", "Source name") { IsRequired = true };
        var importFileOption = new Option<string>("--file", "JSONL inventory file path") { IsRequired = true };
        var importDbOption = new Option<string>("--db", "Database path") { IsRequired = true };
        importRemoteCommand.AddOption(importSourceOption);
        importRemoteCommand.AddOption(importFileOption);
        importRemoteCommand.AddOption(importDbOption);
        importRemoteCommand.SetHandler(ImportRemote, importSourceOption, importFileOption, importDbOption);

        // diff command
        var diffCommand = new Command("diff", "Compute diff between remote and local files");
        var diffSourceOption = new Option<string>("--source", "Source name") { IsRequired = true };
        var diffDbOption = new Option<string>("--db", "Database path") { IsRequired = true };
        diffCommand.AddOption(diffSourceOption);
        diffCommand.AddOption(diffDbOption);
        diffCommand.SetHandler(Diff, diffSourceOption, diffDbOption);

        // report command
        var reportCommand = new Command("report", "Export diff results to JSONL file");
        var reportSourceOption = new Option<string>("--source", "Source name") { IsRequired = true };
        var reportDbOption = new Option<string>("--db", "Database path") { IsRequired = true };
        var reportOutOption = new Option<string>("--out", "Output JSONL file path") { IsRequired = true };
        reportCommand.AddOption(reportSourceOption);
        reportCommand.AddOption(reportDbOption);
        reportCommand.AddOption(reportOutOption);
        reportCommand.SetHandler(Report, reportSourceOption, reportDbOption, reportOutOption);

        rootCommand.AddCommand(scanLocalCommand);
        rootCommand.AddCommand(importRemoteCommand);
        rootCommand.AddCommand(diffCommand);
        rootCommand.AddCommand(reportCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static void ScanLocal(string root, string db)
    {
        try
        {
            Console.WriteLine($"Scanning local files from: {root}");
            
            var scanner = new LocalScanner();
            var files = scanner.Scan(root);
            
            Console.WriteLine($"Found {files.Count} files");

            using var store = new SqliteStore(db);
            store.Initialize();
            store.ReplaceLocalFiles(files);
            
            Console.WriteLine($"Stored {files.Count} local files in database: {db}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void ImportRemote(string source, string file, string db)
    {
        try
        {
            Console.WriteLine($"Importing remote assets from: {file}");
            
            var assets = ReadJsonLines<RemoteAsset>(file).ToList();
            
            // Set SourceName if not already set in the file
            for (int i = 0; i < assets.Count; i++)
            {
                if (string.IsNullOrEmpty(assets[i].SourceName))
                {
                    assets[i] = assets[i] with { SourceName = source };
                }
            }
            
            Console.WriteLine($"Read {assets.Count} remote assets");

            using var store = new SqliteStore(db);
            store.Initialize();
            store.UpsertRemoteAssets(assets);
            
            Console.WriteLine($"Stored {assets.Count} remote assets in database: {db}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void Diff(string source, string db)
    {
        try
        {
            Console.WriteLine($"Computing diff for source: {source}");

            using var store = new SqliteStore(db);
            store.Initialize();
            
            var remoteAssets = store.LoadRemoteAssets(source);
            var localFiles = store.LoadLocalFiles();
            
            Console.WriteLine($"Loaded {remoteAssets.Count} remote assets and {localFiles.Count} local files");

            var diffEngine = new DiffEngine();
            var results = diffEngine.ComputeDiff(remoteAssets, localFiles);
            
            store.SaveDiffResults(results);
            
            var missing = results.Count(r => r.Status == DiffStatus.Missing);
            var present = results.Count(r => r.Status == DiffStatus.Present);
            var uncertain = results.Count(r => r.Status == DiffStatus.Uncertain);
            
            Console.WriteLine($"Diff complete:");
            Console.WriteLine($"  Present: {present}");
            Console.WriteLine($"  Missing: {missing}");
            Console.WriteLine($"  Uncertain: {uncertain}");
            Console.WriteLine($"Results saved to database: {db}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void Report(string source, string db, string output)
    {
        try
        {
            Console.WriteLine($"Generating report for source: {source}");

            using var store = new SqliteStore(db);
            store.Initialize();
            
            var results = store.LoadLatestDiffResults(source);
            
            Console.WriteLine($"Loaded {results.Count} diff results");

            WriteJsonLines(output, results);
            
            Console.WriteLine($"Report written to: {output}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static IEnumerable<T> ReadJsonLines<T>(string filePath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        foreach (var line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            T? item;
            try
            {
                item = JsonSerializer.Deserialize<T>(line, options);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Warning: Failed to parse line: {line}");
                Console.Error.WriteLine($"  Error: {ex.Message}");
                continue;
            }

            if (item != null)
            {
                yield return item;
            }
        }
    }

    static void WriteJsonLines<T>(string filePath, IEnumerable<T> items)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        using var writer = new StreamWriter(filePath);
        foreach (var item in items)
        {
            var json = JsonSerializer.Serialize(item, options);
            writer.WriteLine(json);
        }
    }
}


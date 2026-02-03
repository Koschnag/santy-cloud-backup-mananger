using Santy.Core.Models;

namespace Santy.Core.Services;

public class LocalScanner
{
    private static readonly HashSet<string> JunkFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ".DS_Store",
        "thumbs.db",
        "Thumbs.db"
    };

    public IReadOnlyList<LocalFile> Scan(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Root path does not exist: {rootPath}");
        }

        var results = new List<LocalFile>();
        ScanDirectory(rootPath, results);
        return results;
    }

    private void ScanDirectory(string directoryPath, List<LocalFile> results)
    {
        try
        {
            // Scan files in current directory
            foreach (var filePath in Directory.EnumerateFiles(directoryPath))
            {
                try
                {
                    var fileName = Path.GetFileName(filePath);
                    
                    // Skip junk files
                    if (JunkFiles.Contains(fileName))
                    {
                        continue;
                    }

                    // Skip files starting with "._"
                    if (fileName.StartsWith("._"))
                    {
                        continue;
                    }

                    var fileInfo = new FileInfo(filePath);
                    
                    results.Add(new LocalFile
                    {
                        FullPath = filePath,
                        Filename = fileName,
                        SizeBytes = fileInfo.Length,
                        LastWriteUtc = fileInfo.LastWriteTimeUtc
                    });
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    // Skip files we can't access
                    Console.Error.WriteLine($"Warning: Could not access file {filePath}: {ex.Message}");
                }
            }

            // Recursively scan subdirectories
            foreach (var subDir in Directory.EnumerateDirectories(directoryPath))
            {
                try
                {
                    ScanDirectory(subDir, results);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    // Skip directories we can't access
                    Console.Error.WriteLine($"Warning: Could not access directory {subDir}: {ex.Message}");
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            Console.Error.WriteLine($"Warning: Could not enumerate directory {directoryPath}: {ex.Message}");
        }
    }
}

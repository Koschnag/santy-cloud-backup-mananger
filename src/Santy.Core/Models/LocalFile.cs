namespace Santy.Core.Models;

public record LocalFile
{
    public required string FullPath { get; init; }
    public required string Filename { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime LastWriteUtc { get; init; }
}

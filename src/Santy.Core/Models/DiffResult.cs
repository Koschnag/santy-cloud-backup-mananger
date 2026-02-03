namespace Santy.Core.Models;

public record DiffResult
{
    public required string SourceName { get; init; }
    public required string SourceAssetId { get; init; }
    public required DiffStatus Status { get; init; }
    public required string Reason { get; init; }
    public string? MatchedLocalPath { get; init; }
}

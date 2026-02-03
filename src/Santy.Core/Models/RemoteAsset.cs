namespace Santy.Core.Models;

public record RemoteAsset
{
    public required string SourceName { get; init; }
    public required string SourceAssetId { get; init; }
    public required string Filename { get; init; }
    public required long SizeBytes { get; init; }
    public DateTime? CreatedUtc { get; init; }
    public string? MediaType { get; init; }
}

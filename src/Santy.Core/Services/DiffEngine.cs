using Santy.Core.Models;

namespace Santy.Core.Services;

public class DiffEngine
{
    public IReadOnlyList<DiffResult> ComputeDiff(
        IEnumerable<RemoteAsset> remoteAssets,
        IEnumerable<LocalFile> localFiles)
    {
        var results = new List<DiffResult>();
        var localFilesList = localFiles.ToList();

        // Build lookup structures for efficient matching
        var localByFilename = localFilesList
            .ToLookup(f => f.Filename, StringComparer.OrdinalIgnoreCase);

        foreach (var remote in remoteAssets)
        {
            var diffResult = MatchRemoteAsset(remote, localFilesList, localByFilename);
            results.Add(diffResult);
        }

        return results;
    }

    private DiffResult MatchRemoteAsset(
        RemoteAsset remote,
        List<LocalFile> allLocalFiles,
        ILookup<string, LocalFile> localByFilename)
    {
        // Rule 1: Check if any local filename starts with "<SourceAssetId>_"
        var exactMatch = allLocalFiles.FirstOrDefault(f =>
            f.Filename.StartsWith($"{remote.SourceAssetId}_", StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            return new DiffResult
            {
                SourceName = remote.SourceName,
                SourceAssetId = remote.SourceAssetId,
                Status = DiffStatus.Present,
                Reason = "exact match by asset ID prefix",
                MatchedLocalPath = exactMatch.FullPath
            };
        }

        // Rule 2: Match by (Filename + SizeBytes)
        var candidatesByName = localByFilename[remote.Filename].ToList();
        var matchingCandidates = candidatesByName
            .Where(f => f.SizeBytes == remote.SizeBytes)
            .ToList();

        if (matchingCandidates.Count == 1)
        {
            return new DiffResult
            {
                SourceName = remote.SourceName,
                SourceAssetId = remote.SourceAssetId,
                Status = DiffStatus.Present,
                Reason = "matched by filename and size",
                MatchedLocalPath = matchingCandidates[0].FullPath
            };
        }

        if (matchingCandidates.Count > 1)
        {
            return new DiffResult
            {
                SourceName = remote.SourceName,
                SourceAssetId = remote.SourceAssetId,
                Status = DiffStatus.Uncertain,
                Reason = $"ambiguous candidates ({matchingCandidates.Count} files match)",
                MatchedLocalPath = null
            };
        }

        // Rule 3: No matches found
        return new DiffResult
        {
            SourceName = remote.SourceName,
            SourceAssetId = remote.SourceAssetId,
            Status = DiffStatus.Missing,
            Reason = "no matching local file found",
            MatchedLocalPath = null
        };
    }
}

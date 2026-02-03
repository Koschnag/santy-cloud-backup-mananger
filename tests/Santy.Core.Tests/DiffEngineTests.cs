using Santy.Core.Models;
using Santy.Core.Services;
using Xunit;

namespace Santy.Core.Tests;

public class DiffEngineTests
{
    [Fact]
    public void ComputeDiff_ExactMatchByAssetIdPrefix_ReturnsPresent()
    {
        // Arrange
        var diffEngine = new DiffEngine();
        var remoteAssets = new[]
        {
            new RemoteAsset
            {
                SourceName = "icloud",
                SourceAssetId = "ABC123",
                Filename = "photo.jpg",
                SizeBytes = 1000
            }
        };

        var localFiles = new[]
        {
            new LocalFile
            {
                FullPath = "/backup/ABC123_photo.jpg",
                Filename = "ABC123_photo.jpg",
                SizeBytes = 1000,
                LastWriteUtc = DateTime.UtcNow
            }
        };

        // Act
        var results = diffEngine.ComputeDiff(remoteAssets, localFiles);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.Equal(DiffStatus.Present, result.Status);
        Assert.Equal("exact match by asset ID prefix", result.Reason);
        Assert.Equal("/backup/ABC123_photo.jpg", result.MatchedLocalPath);
    }

    [Fact]
    public void ComputeDiff_MatchByFilenameAndSize_ReturnsPresent()
    {
        // Arrange
        var diffEngine = new DiffEngine();
        var remoteAssets = new[]
        {
            new RemoteAsset
            {
                SourceName = "icloud",
                SourceAssetId = "XYZ789",
                Filename = "document.pdf",
                SizeBytes = 5000
            }
        };

        var localFiles = new[]
        {
            new LocalFile
            {
                FullPath = "/backup/document.pdf",
                Filename = "document.pdf",
                SizeBytes = 5000,
                LastWriteUtc = DateTime.UtcNow
            }
        };

        // Act
        var results = diffEngine.ComputeDiff(remoteAssets, localFiles);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.Equal(DiffStatus.Present, result.Status);
        Assert.Equal("matched by filename and size", result.Reason);
        Assert.Equal("/backup/document.pdf", result.MatchedLocalPath);
    }

    [Fact]
    public void ComputeDiff_MultipleCandidates_ReturnsUncertain()
    {
        // Arrange
        var diffEngine = new DiffEngine();
        var remoteAssets = new[]
        {
            new RemoteAsset
            {
                SourceName = "icloud",
                SourceAssetId = "DEF456",
                Filename = "video.mp4",
                SizeBytes = 10000
            }
        };

        var localFiles = new[]
        {
            new LocalFile
            {
                FullPath = "/backup/folder1/video.mp4",
                Filename = "video.mp4",
                SizeBytes = 10000,
                LastWriteUtc = DateTime.UtcNow
            },
            new LocalFile
            {
                FullPath = "/backup/folder2/video.mp4",
                Filename = "video.mp4",
                SizeBytes = 10000,
                LastWriteUtc = DateTime.UtcNow
            }
        };

        // Act
        var results = diffEngine.ComputeDiff(remoteAssets, localFiles);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.Equal(DiffStatus.Uncertain, result.Status);
        Assert.Contains("ambiguous candidates", result.Reason);
        Assert.Null(result.MatchedLocalPath);
    }

    [Fact]
    public void ComputeDiff_NoMatch_ReturnsMissing()
    {
        // Arrange
        var diffEngine = new DiffEngine();
        var remoteAssets = new[]
        {
            new RemoteAsset
            {
                SourceName = "icloud",
                SourceAssetId = "GHI789",
                Filename = "missing.txt",
                SizeBytes = 500
            }
        };

        var localFiles = new[]
        {
            new LocalFile
            {
                FullPath = "/backup/other.txt",
                Filename = "other.txt",
                SizeBytes = 500,
                LastWriteUtc = DateTime.UtcNow
            }
        };

        // Act
        var results = diffEngine.ComputeDiff(remoteAssets, localFiles);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.Equal(DiffStatus.Missing, result.Status);
        Assert.Equal("no matching local file found", result.Reason);
        Assert.Null(result.MatchedLocalPath);
    }

    [Fact]
    public void ComputeDiff_SameFilenameDifferentSize_ReturnsMissing()
    {
        // Arrange
        var diffEngine = new DiffEngine();
        var remoteAssets = new[]
        {
            new RemoteAsset
            {
                SourceName = "icloud",
                SourceAssetId = "JKL012",
                Filename = "photo.jpg",
                SizeBytes = 2000
            }
        };

        var localFiles = new[]
        {
            new LocalFile
            {
                FullPath = "/backup/photo.jpg",
                Filename = "photo.jpg",
                SizeBytes = 3000, // Different size
                LastWriteUtc = DateTime.UtcNow
            }
        };

        // Act
        var results = diffEngine.ComputeDiff(remoteAssets, localFiles);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.Equal(DiffStatus.Missing, result.Status);
        Assert.Equal("no matching local file found", result.Reason);
    }

    [Fact]
    public void ComputeDiff_AssetIdPrefixTakesPrecedenceOverFilenameMatch()
    {
        // Arrange
        var diffEngine = new DiffEngine();
        var remoteAssets = new[]
        {
            new RemoteAsset
            {
                SourceName = "icloud",
                SourceAssetId = "PRIORITY123",
                Filename = "image.jpg",
                SizeBytes = 1500
            }
        };

        var localFiles = new[]
        {
            new LocalFile
            {
                FullPath = "/backup/image.jpg",
                Filename = "image.jpg",
                SizeBytes = 1500,
                LastWriteUtc = DateTime.UtcNow
            },
            new LocalFile
            {
                FullPath = "/backup/PRIORITY123_image.jpg",
                Filename = "PRIORITY123_image.jpg",
                SizeBytes = 1500,
                LastWriteUtc = DateTime.UtcNow
            }
        };

        // Act
        var results = diffEngine.ComputeDiff(remoteAssets, localFiles);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.Equal(DiffStatus.Present, result.Status);
        Assert.Equal("exact match by asset ID prefix", result.Reason);
        Assert.Equal("/backup/PRIORITY123_image.jpg", result.MatchedLocalPath);
    }

    [Fact]
    public void ComputeDiff_CaseInsensitiveMatching()
    {
        // Arrange
        var diffEngine = new DiffEngine();
        var remoteAssets = new[]
        {
            new RemoteAsset
            {
                SourceName = "icloud",
                SourceAssetId = "case123",
                Filename = "Photo.JPG",
                SizeBytes = 2500
            }
        };

        var localFiles = new[]
        {
            new LocalFile
            {
                FullPath = "/backup/CASE123_photo.jpg",
                Filename = "CASE123_photo.jpg",
                SizeBytes = 2500,
                LastWriteUtc = DateTime.UtcNow
            }
        };

        // Act
        var results = diffEngine.ComputeDiff(remoteAssets, localFiles);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.Equal(DiffStatus.Present, result.Status);
        Assert.Equal("exact match by asset ID prefix", result.Reason);
    }

    [Fact]
    public void ComputeDiff_MultipleRemoteAssets_ProcessesAll()
    {
        // Arrange
        var diffEngine = new DiffEngine();
        var remoteAssets = new[]
        {
            new RemoteAsset
            {
                SourceName = "icloud",
                SourceAssetId = "ASSET1",
                Filename = "file1.txt",
                SizeBytes = 100
            },
            new RemoteAsset
            {
                SourceName = "icloud",
                SourceAssetId = "ASSET2",
                Filename = "file2.txt",
                SizeBytes = 200
            },
            new RemoteAsset
            {
                SourceName = "icloud",
                SourceAssetId = "ASSET3",
                Filename = "file3.txt",
                SizeBytes = 300
            }
        };

        var localFiles = new[]
        {
            new LocalFile
            {
                FullPath = "/backup/ASSET1_file1.txt",
                Filename = "ASSET1_file1.txt",
                SizeBytes = 100,
                LastWriteUtc = DateTime.UtcNow
            },
            new LocalFile
            {
                FullPath = "/backup/file2.txt",
                Filename = "file2.txt",
                SizeBytes = 200,
                LastWriteUtc = DateTime.UtcNow
            }
            // file3.txt is missing
        };

        // Act
        var results = diffEngine.ComputeDiff(remoteAssets, localFiles);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(DiffStatus.Present, results[0].Status);
        Assert.Equal(DiffStatus.Present, results[1].Status);
        Assert.Equal(DiffStatus.Missing, results[2].Status);
    }
}

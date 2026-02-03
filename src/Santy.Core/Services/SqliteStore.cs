using System.Data;
using Microsoft.Data.Sqlite;
using Santy.Core.Models;

namespace Santy.Core.Services;

public class SqliteStore : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public SqliteStore(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS remote_assets (
                source_name TEXT NOT NULL,
                source_asset_id TEXT NOT NULL,
                filename TEXT NOT NULL,
                size_bytes INTEGER NOT NULL,
                created_utc TEXT,
                media_type TEXT,
                PRIMARY KEY (source_name, source_asset_id)
            );

            CREATE TABLE IF NOT EXISTS local_files (
                full_path TEXT PRIMARY KEY,
                filename TEXT NOT NULL,
                size_bytes INTEGER NOT NULL,
                last_write_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS diff_results (
                source_name TEXT NOT NULL,
                source_asset_id TEXT NOT NULL,
                status TEXT NOT NULL,
                reason TEXT NOT NULL,
                matched_local_path TEXT,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (source_name, source_asset_id)
            );

            CREATE TABLE IF NOT EXISTS jobs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                job_type TEXT NOT NULL,
                source_name TEXT,
                started_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                completed_at TEXT,
                status TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_local_files_filename_size 
                ON local_files(filename, size_bytes);
            CREATE INDEX IF NOT EXISTS idx_remote_assets_source 
                ON remote_assets(source_name);
        ";
        cmd.ExecuteNonQuery();
    }

    public void UpsertRemoteAssets(IEnumerable<RemoteAsset> assets)
    {
        EnsureConnection();

        using var transaction = _connection!.BeginTransaction();
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = transaction;
        
        cmd.CommandText = @"
            INSERT INTO remote_assets (source_name, source_asset_id, filename, size_bytes, created_utc, media_type)
            VALUES (@source_name, @source_asset_id, @filename, @size_bytes, @created_utc, @media_type)
            ON CONFLICT(source_name, source_asset_id) DO UPDATE SET
                filename = excluded.filename,
                size_bytes = excluded.size_bytes,
                created_utc = excluded.created_utc,
                media_type = excluded.media_type
        ";

        var pSourceName = cmd.CreateParameter();
        pSourceName.ParameterName = "@source_name";
        cmd.Parameters.Add(pSourceName);

        var pSourceAssetId = cmd.CreateParameter();
        pSourceAssetId.ParameterName = "@source_asset_id";
        cmd.Parameters.Add(pSourceAssetId);

        var pFilename = cmd.CreateParameter();
        pFilename.ParameterName = "@filename";
        cmd.Parameters.Add(pFilename);

        var pSizeBytes = cmd.CreateParameter();
        pSizeBytes.ParameterName = "@size_bytes";
        cmd.Parameters.Add(pSizeBytes);

        var pCreatedUtc = cmd.CreateParameter();
        pCreatedUtc.ParameterName = "@created_utc";
        cmd.Parameters.Add(pCreatedUtc);

        var pMediaType = cmd.CreateParameter();
        pMediaType.ParameterName = "@media_type";
        cmd.Parameters.Add(pMediaType);

        foreach (var asset in assets)
        {
            pSourceName.Value = asset.SourceName;
            pSourceAssetId.Value = asset.SourceAssetId;
            pFilename.Value = asset.Filename;
            pSizeBytes.Value = asset.SizeBytes;
            pCreatedUtc.Value = asset.CreatedUtc?.ToString("O") ?? (object)DBNull.Value;
            pMediaType.Value = asset.MediaType ?? (object)DBNull.Value;

            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void ReplaceLocalFiles(IEnumerable<LocalFile> files)
    {
        EnsureConnection();

        using var transaction = _connection!.BeginTransaction();
        
        // Clear existing local files
        using (var deleteCmd = _connection.CreateCommand())
        {
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = "DELETE FROM local_files";
            deleteCmd.ExecuteNonQuery();
        }

        // Insert new local files
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
            INSERT INTO local_files (full_path, filename, size_bytes, last_write_utc)
            VALUES (@full_path, @filename, @size_bytes, @last_write_utc)
        ";

        var pFullPath = cmd.CreateParameter();
        pFullPath.ParameterName = "@full_path";
        cmd.Parameters.Add(pFullPath);

        var pFilename = cmd.CreateParameter();
        pFilename.ParameterName = "@filename";
        cmd.Parameters.Add(pFilename);

        var pSizeBytes = cmd.CreateParameter();
        pSizeBytes.ParameterName = "@size_bytes";
        cmd.Parameters.Add(pSizeBytes);

        var pLastWriteUtc = cmd.CreateParameter();
        pLastWriteUtc.ParameterName = "@last_write_utc";
        cmd.Parameters.Add(pLastWriteUtc);

        foreach (var file in files)
        {
            pFullPath.Value = file.FullPath;
            pFilename.Value = file.Filename;
            pSizeBytes.Value = file.SizeBytes;
            pLastWriteUtc.Value = file.LastWriteUtc.ToString("O");

            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void SaveDiffResults(IEnumerable<DiffResult> results)
    {
        EnsureConnection();

        using var transaction = _connection!.BeginTransaction();
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = transaction;

        cmd.CommandText = @"
            INSERT INTO diff_results (source_name, source_asset_id, status, reason, matched_local_path)
            VALUES (@source_name, @source_asset_id, @status, @reason, @matched_local_path)
            ON CONFLICT(source_name, source_asset_id) DO UPDATE SET
                status = excluded.status,
                reason = excluded.reason,
                matched_local_path = excluded.matched_local_path,
                created_at = CURRENT_TIMESTAMP
        ";

        var pSourceName = cmd.CreateParameter();
        pSourceName.ParameterName = "@source_name";
        cmd.Parameters.Add(pSourceName);

        var pSourceAssetId = cmd.CreateParameter();
        pSourceAssetId.ParameterName = "@source_asset_id";
        cmd.Parameters.Add(pSourceAssetId);

        var pStatus = cmd.CreateParameter();
        pStatus.ParameterName = "@status";
        cmd.Parameters.Add(pStatus);

        var pReason = cmd.CreateParameter();
        pReason.ParameterName = "@reason";
        cmd.Parameters.Add(pReason);

        var pMatchedLocalPath = cmd.CreateParameter();
        pMatchedLocalPath.ParameterName = "@matched_local_path";
        cmd.Parameters.Add(pMatchedLocalPath);

        foreach (var result in results)
        {
            pSourceName.Value = result.SourceName;
            pSourceAssetId.Value = result.SourceAssetId;
            pStatus.Value = result.Status.ToString();
            pReason.Value = result.Reason;
            pMatchedLocalPath.Value = result.MatchedLocalPath ?? (object)DBNull.Value;

            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public IReadOnlyList<RemoteAsset> LoadRemoteAssets(string sourceName)
    {
        EnsureConnection();

        var results = new List<RemoteAsset>();

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            SELECT source_name, source_asset_id, filename, size_bytes, created_utc, media_type
            FROM remote_assets
            WHERE source_name = @source_name
        ";

        var pSourceName = cmd.CreateParameter();
        pSourceName.ParameterName = "@source_name";
        pSourceName.Value = sourceName;
        cmd.Parameters.Add(pSourceName);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new RemoteAsset
            {
                SourceName = reader.GetString(0),
                SourceAssetId = reader.GetString(1),
                Filename = reader.GetString(2),
                SizeBytes = reader.GetInt64(3),
                CreatedUtc = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                MediaType = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return results;
    }

    public IReadOnlyList<LocalFile> LoadLocalFiles()
    {
        EnsureConnection();

        var results = new List<LocalFile>();

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            SELECT full_path, filename, size_bytes, last_write_utc
            FROM local_files
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new LocalFile
            {
                FullPath = reader.GetString(0),
                Filename = reader.GetString(1),
                SizeBytes = reader.GetInt64(2),
                LastWriteUtc = DateTime.Parse(reader.GetString(3))
            });
        }

        return results;
    }

    public IReadOnlyList<DiffResult> LoadLatestDiffResults(string sourceName)
    {
        EnsureConnection();

        var results = new List<DiffResult>();

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            SELECT source_name, source_asset_id, status, reason, matched_local_path
            FROM diff_results
            WHERE source_name = @source_name
        ";

        var pSourceName = cmd.CreateParameter();
        pSourceName.ParameterName = "@source_name";
        pSourceName.Value = sourceName;
        cmd.Parameters.Add(pSourceName);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new DiffResult
            {
                SourceName = reader.GetString(0),
                SourceAssetId = reader.GetString(1),
                Status = Enum.Parse<DiffStatus>(reader.GetString(2)),
                Reason = reader.GetString(3),
                MatchedLocalPath = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return results;
    }

    private void EnsureConnection()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Database not initialized. Call Initialize() first.");
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

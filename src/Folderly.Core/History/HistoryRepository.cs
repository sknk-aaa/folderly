using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Folderly.Core.History;

/// <summary>
/// SQLite を使った履歴と設定の永続化。
/// テスト時は dbPath に ":memory:" を渡す。
/// </summary>
public sealed class HistoryRepository : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ILogger<HistoryRepository> _logger;

    public HistoryRepository(string dbPath, ILogger<HistoryRepository>? logger = null)
    {
        SQLitePCL.Batteries_V2.Init();

        _logger = logger ?? NullLogger<HistoryRepository>.Instance;
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS folder_history (
                id                      INTEGER PRIMARY KEY AUTOINCREMENT,
                folder_path             TEXT NOT NULL UNIQUE,
                original_attributes     INTEGER NOT NULL,
                had_desktop_ini         INTEGER NOT NULL,
                original_desktop_ini    BLOB,
                original_desktop_ini_attrs INTEGER,
                source_image_path       TEXT NOT NULL,
                icon_hash               TEXT NOT NULL,
                icon_storage_path       TEXT NOT NULL,
                crop_mode               TEXT NOT NULL,
                image_scale             REAL NOT NULL DEFAULT 1.0,
                image_offset_x          REAL NOT NULL DEFAULT 0.0,
                image_offset_y          REAL NOT NULL DEFAULT 0.0,
                tag_color               TEXT,
                tag_key                 TEXT,
                tag_name                TEXT,
                tag_label_visible       INTEGER NOT NULL DEFAULT 0,
                applied_at              TEXT NOT NULL,
                schema_version          INTEGER NOT NULL DEFAULT 1
            );
            CREATE INDEX IF NOT EXISTS idx_history_applied_at
                ON folder_history(applied_at DESC);

            CREATE TABLE IF NOT EXISTS settings (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS schema_info (
                version    INTEGER PRIMARY KEY,
                applied_at TEXT NOT NULL
            );

            INSERT OR IGNORE INTO schema_info (version, applied_at)
                VALUES (1, datetime('now'));
            """;
        cmd.ExecuteNonQuery();

        EnsureColumn("folder_history", "tag_key", "TEXT");
        EnsureColumn("folder_history", "tag_name", "TEXT");
        EnsureColumn("folder_history", "tag_label_visible", "INTEGER NOT NULL DEFAULT 0");
    }

    private void EnsureColumn(string table, string column, string definition)
    {
        using var check = _conn.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table})";
        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(reader.GetOrdinal("name")), column, StringComparison.OrdinalIgnoreCase))
                return;
        }

        using var alter = _conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
        alter.ExecuteNonQuery();
    }

    /// <summary>同一フォルダパスは上書き（UPSERT）。</summary>
    public void Upsert(HistoryEntry entry)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO folder_history (
                folder_path, original_attributes, had_desktop_ini,
                original_desktop_ini, original_desktop_ini_attrs,
                source_image_path, icon_hash, icon_storage_path,
                crop_mode, image_scale, image_offset_x, image_offset_y,
                tag_color, tag_key, tag_name, tag_label_visible, applied_at, schema_version
            ) VALUES (
                $path, $orig_attrs, $had_ini,
                $orig_ini, $orig_ini_attrs,
                $src_img, $hash, $storage,
                $crop, $scale, $offx, $offy,
                $tag, $tag_key, $tag_name, $tag_label_visible, $applied_at, $schema_ver
            )
            ON CONFLICT(folder_path) DO UPDATE SET
                source_image_path       = excluded.source_image_path,
                icon_hash               = excluded.icon_hash,
                icon_storage_path       = excluded.icon_storage_path,
                crop_mode               = excluded.crop_mode,
                image_scale             = excluded.image_scale,
                image_offset_x          = excluded.image_offset_x,
                image_offset_y          = excluded.image_offset_y,
                tag_color               = excluded.tag_color,
                tag_key                 = excluded.tag_key,
                tag_name                = excluded.tag_name,
                tag_label_visible       = excluded.tag_label_visible,
                applied_at              = excluded.applied_at,
                schema_version          = excluded.schema_version;
            """;
        cmd.Parameters.AddWithValue("$path", entry.FolderPath);
        cmd.Parameters.AddWithValue("$orig_attrs", entry.OriginalAttributes);
        cmd.Parameters.AddWithValue("$had_ini", entry.HadDesktopIni ? 1 : 0);
        cmd.Parameters.AddWithValue("$orig_ini", (object?)entry.OriginalDesktopIniContent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$orig_ini_attrs", (object?)entry.OriginalDesktopIniAttrs ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$src_img", entry.SourceImagePath);
        cmd.Parameters.AddWithValue("$hash", entry.IconHash);
        cmd.Parameters.AddWithValue("$storage", entry.IconStoragePath);
        cmd.Parameters.AddWithValue("$crop", entry.CropMode);
        cmd.Parameters.AddWithValue("$scale", entry.ImageScale);
        cmd.Parameters.AddWithValue("$offx", entry.ImageOffsetX);
        cmd.Parameters.AddWithValue("$offy", entry.ImageOffsetY);
        cmd.Parameters.AddWithValue("$tag", (object?)entry.TagColor ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tag_key", (object?)entry.TagKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tag_name", (object?)entry.TagName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tag_label_visible", entry.TagLabelVisible ? 1 : 0);
        cmd.Parameters.AddWithValue("$applied_at", entry.AppliedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$schema_ver", entry.SchemaVersion);
        cmd.ExecuteNonQuery();
    }

    public HistoryEntry? GetByPath(string folderPath)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM folder_history WHERE folder_path = $path";
        cmd.Parameters.AddWithValue("$path", folderPath);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapEntry(reader) : null;
    }

    public IReadOnlyList<HistoryEntry> GetAll()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM folder_history ORDER BY applied_at DESC";
        using var reader = cmd.ExecuteReader();
        var list = new List<HistoryEntry>();
        while (reader.Read())
            list.Add(MapEntry(reader));
        return list;
    }

    public void Delete(string folderPath)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM folder_history WHERE folder_path = $path";
        cmd.Parameters.AddWithValue("$path", folderPath);
        cmd.ExecuteNonQuery();
    }

    public string? GetSetting(string key)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = $key";
        cmd.Parameters.AddWithValue("$key", key);
        var result = cmd.ExecuteScalar();
        return result is DBNull or null ? null : (string)result;
    }

    public void SetSetting(string key, string value)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO settings (key, value) VALUES ($key, $val)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            """;
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$val", value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>履歴件数が maxCount を超えた場合、古い順に削除する。</summary>
    public void EnforceMaxCount(int maxCount)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            DELETE FROM folder_history
            WHERE id NOT IN (
                SELECT id FROM folder_history
                ORDER BY applied_at DESC
                LIMIT $max
            );
            """;
        cmd.Parameters.AddWithValue("$max", maxCount);
        cmd.ExecuteNonQuery();
    }

    private static HistoryEntry MapEntry(SqliteDataReader r)
    {
        byte[]? iniContent = null;
        if (!r.IsDBNull(r.GetOrdinal("original_desktop_ini")))
            iniContent = (byte[])r["original_desktop_ini"];

        int? iniAttrs = null;
        if (!r.IsDBNull(r.GetOrdinal("original_desktop_ini_attrs")))
            iniAttrs = r.GetInt32(r.GetOrdinal("original_desktop_ini_attrs"));

        string? tagColor = null;
        if (!r.IsDBNull(r.GetOrdinal("tag_color")))
            tagColor = r.GetString(r.GetOrdinal("tag_color"));

        string? tagKey = null;
        if (!r.IsDBNull(r.GetOrdinal("tag_key")))
            tagKey = r.GetString(r.GetOrdinal("tag_key"));

        string? tagName = null;
        if (!r.IsDBNull(r.GetOrdinal("tag_name")))
            tagName = r.GetString(r.GetOrdinal("tag_name"));

        return new HistoryEntry(
            Id:                         r.GetInt64(r.GetOrdinal("id")),
            FolderPath:                 r.GetString(r.GetOrdinal("folder_path")),
            OriginalAttributes:         r.GetInt32(r.GetOrdinal("original_attributes")),
            HadDesktopIni:              r.GetInt32(r.GetOrdinal("had_desktop_ini")) != 0,
            OriginalDesktopIniContent:  iniContent,
            OriginalDesktopIniAttrs:    iniAttrs,
            SourceImagePath:            r.GetString(r.GetOrdinal("source_image_path")),
            IconHash:                   r.GetString(r.GetOrdinal("icon_hash")),
            IconStoragePath:            r.GetString(r.GetOrdinal("icon_storage_path")),
            CropMode:                   r.GetString(r.GetOrdinal("crop_mode")),
            ImageScale:                 r.GetDouble(r.GetOrdinal("image_scale")),
            ImageOffsetX:               r.GetDouble(r.GetOrdinal("image_offset_x")),
            ImageOffsetY:               r.GetDouble(r.GetOrdinal("image_offset_y")),
            TagColor:                   tagColor,
            AppliedAt:                  DateTime.Parse(
                                            r.GetString(r.GetOrdinal("applied_at")),
                                            null, System.Globalization.DateTimeStyles.RoundtripKind),
            SchemaVersion:              r.GetInt32(r.GetOrdinal("schema_version")),
            TagKey:                     tagKey,
            TagName:                    tagName,
            TagLabelVisible:            r.GetInt32(r.GetOrdinal("tag_label_visible")) != 0);
    }

    public void Dispose() => _conn.Dispose();
}

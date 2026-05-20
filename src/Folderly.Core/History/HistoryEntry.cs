namespace Folderly.Core.History;

/// <summary>
/// フォルダカスタマイズ履歴の1レコード（SPEC.md Section 5.1 対応）。
/// </summary>
public record HistoryEntry(
    long?    Id,
    string   FolderPath,
    int      OriginalAttributes,
    bool     HadDesktopIni,
    byte[]?  OriginalDesktopIniContent,
    int?     OriginalDesktopIniAttrs,
    string   SourceImagePath,
    string   IconHash,
    string   IconStoragePath,
    string   CropMode,
    double   ImageScale,
    double   ImageOffsetX,
    double   ImageOffsetY,
    string?  TagColor,
    DateTime AppliedAt,
    int      SchemaVersion = 1);

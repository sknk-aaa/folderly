# Folderly — 実装判断ログ

このファイルは SPEC.md に明記されていない細部の判断を記録します。

## 仕様判断ログ（Step 1〜10）

### 1. IShellNotifier インターフェースの追加
- **判断**: `Folderly.Core/Shell/IShellNotifier.cs` を追加した
- **理由**: SPEC.md Section 6 のファイル一覧には未記載だが、Section 6.1「Core は Windows 固有 API に依存しない」要件から依存性逆転が必要。Core の ApplyService が Shell の SHChangeNotify を直接呼べないため、インターフェースを Core に定義し Shell が実装する設計を採用。

### 2. フォルダテンプレートは PNG EmbeddedResource
- **判断**: `FolderTemplate.png` を `net8.0` Core プロジェクトに EmbeddedResource として同梱
- **理由**: SPEC.md Section 4.6「SVG として内蔵する」の記述があるが、SixLabors.ImageSharp は SVG をネイティブに読み込めない（ラスタライザには別ライブラリが必要）。PNG を EmbeddedResource にすることで WPF 依存なしに WSL2 上でもテスト可能。テンプレートが EmbeddedResource にない場合は `GenerateTemplatePng()` でプログラム的に生成するフォールバックを実装。

### 3. FolderAttributesService は .NET 標準 FileAttributes を使用
- **判断**: `System.IO.File.GetAttributes/SetAttributes` と `FileAttributes` 列挙型で実装（P/Invoke 不要）
- **理由**: `FILE_ATTRIBUTE_SYSTEM` 等の Windows NTFS 属性は .NET 標準の `FileAttributes` 列挙型に含まれる。P/Invoke を使わなくてもアクセス可能で、WSL2 上でのテストが可能になる。実際の NTFS 属性動作は Windows 環境での手動テストで確認。

### 4. HistoryRepository テストは `:memory:` SQLite 使用
- **判断**: コンストラクタが `dbPath=":memory:"` を受け取れるよう設計
- **理由**: ファイルシステム依存を排除してテスト速度と信頼性を向上させるため。Microsoft.Data.Sqlite は `:memory:` データソースをサポートする。

### 5. INI パーサーは外部ライブラリなし（自前行処理）
- **判断**: `DesktopIniManager.UpdateOrAddKey` を文字列行処理で自前実装
- **理由**: SPEC.md Section 2 の固定ライブラリリストに INI パーサーライブラリは含まれない。シンプルな行処理で十分機能する。

### 6. Dropbox パス判定はパス文字列に "Dropbox" を含むかで判定
- **判断**: `path.Contains("Dropbox", StringComparison.OrdinalIgnoreCase)` のみ
- **理由**: Dropbox は標準 Windows 環境変数を設定しない。正確な判定には `%APPDATA%\Dropbox\info.json` からパスを取得する必要があるが、ファイル読み込みと例外処理が複雑になる。シンプルで保守的な文字列判定を採用。将来的に改善可能。

### 7. IcoConverter は全サイズ PNG 埋め込み方式
- **判断**: ICO ファイル内の全サイズ（16/32/48/256px）を PNG として格納
- **理由**: Windows Vista 以降は PNG 埋め込み ICO をサポートしており、BMP/DIB 形式より SixLabors.ImageSharp との親和性が高くシンプル。

### 8. SixLabors.ImageSharp.Drawing 2.1.7 を追加
- **判断**: `Folderly.Core.csproj` に `SixLabors.ImageSharp.Drawing` を追加
- **理由**: `TemplateRenderer` でタグ領域の矩形塗りつぶし（`RectangularPolygon` + `ctx.Fill()`）に `SixLabors.ImageSharp.Drawing.Processing` が必要。SPEC.md Section 2「SixLabors.ImageSharp 3.x」の関連パッケージとして追加（メジャーバージョン範囲内）。

### 9. SixLabors.ImageSharp のバージョンを 3.1.12 に変更
- **判断**: プランの 3.1.5 から 3.1.12 へアップグレード
- **理由**: 3.1.5 に NuGet が報告する高深刻度の脆弱性 (GHSA-2cmq-823j-5qj8) があったため、同じ 3.x 系列の最新パッチ版に変更。SPEC.md Section 2「SixLabors.ImageSharp 3.x」の範囲内。

### 10. UNC パスチェックは Path.GetFullPath より前に実施
- **判断**: `CheckPath()` の冒頭で `path.StartsWith(@"\\")` を確認し、その後 `Path.GetFullPath` を呼ぶ
- **理由**: Linux/WSL2 では `Path.GetFullPath(@"\\server\share")` が UNC パスを正しく処理しない（バックスラッシュが通常文字として扱われる）。事前チェックにより WSL2 でも UNC 判定テストが通過する。

## 次セッション申し送り事項（Step 11以降）

- Step 11〜: WPF GUI の実装（net8.0-windows、Windows 環境が必要）
- Folderly.App の WPF コントロール実装は VS2022 または Windows の dotnet CLI で行う
- Shell 層（Folderly.Shell）のビルド・テストは Windows 環境で実施する
- FolderTemplate.png は現在シンプルな2色の矩形。Step 11 で WPF プレビューに合わせて視覚的に最適化すること

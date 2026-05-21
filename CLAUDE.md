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

## 仕様判断ログ（Step 11〜16）

### 11. TargetFramework を net8.0-windows10.0.17763.0 に変更
- **判断**: `Folderly.App.csproj` の TFM を `net8.0-windows` から `net8.0-windows10.0.17763.0` に変更
- **理由**: `Windows.Services.Store.StoreContext`（WinRT API）を使用するため、最低 Windows 10 1809 (17763) のバージョン修飾が必要。`net8.0-windows` では WinRT 型が解決されない。

### 12. AppServices は DI フレームワークなしの静的コンテナ
- **判断**: `AppServices` static class でサービスを保持。`Initialize()` を `App.OnStartup` から呼ぶ
- **理由**: YAGNI 原則。v1.0 のサービス数（5つ）では DI コンテナ導入のメリットがコストを下回る。テストは Core 層（DI 不要）のみ対象。

### 13. LocalizationService はインデクサーバインディングで即時反映
- **判断**: `this[string key]` インデクサー + `PropertyChanged(Binding.IndexerName)` で言語切替を即時反映
- **理由**: SPEC F-15「再起動不要で即時切替」。ViewModel が `public LocalizationService L => LocalizationService.Instance` を公開し、XAML が `{Binding L[Key]}` でバインド。`PropertyChanged` に `Binding.IndexerName`（= "Item[]"）を発火することで WPF がすべてのインデクサーバインディングを再評価する。

### 14. FileLogger はカスタム実装（Serilog 非使用）
- **判断**: `FileLoggerProvider` + `FileLogger` を自前実装。ローテーション: 5MB で rotate、最大 5 世代保持
- **理由**: SPEC.md Section 2 の固定 NuGet リストに Serilog は含まれない。`Microsoft.Extensions.Logging.Abstractions` は許可済みのため、ILogger インターフェースを満たすシンプルな実装を選択。

### 15. StoreLicenseService は StoreContext 失敗時に試用版フォールバック
- **判断**: `StoreContext.GetDefault()` の呼び出しを `try-catch` で囲み、例外時は `IsTrial=true, DaysRemaining=7` を返す
- **理由**: MSIX 未パッケージ環境（開発中・テスト中）では `StoreContext.GetDefault()` が例外を投げる。フォールバックにより開発環境でもアプリが動作する。本番（MSIX パッケージ）では正常に動作する。

### 16. タグボタンはコードビハインドで動的生成
- **判断**: `ApplyWindow.xaml.cs` の `BuildTagButtons()` でタグボタンを ControlTemplate + Ellipse で生成
- **理由**: XAML DataTemplate で `HexToColorConverter` + 選択状態管理を宣言的に記述すると、`MultiBinding` + `IMultiValueConverter` が必要になり複雑化する。コードビハインドで生成する方がシンプルで見通しがよい。

### 17. 単一インスタンス制御は Mutex + NamedPipe
- **判断**: `App.OnStartup` で `Mutex("Folderly_SingleInstance_v1")` を取得。既存インスタンスがあれば `NamedPipeClientStream` でフォルダパスを送信して終了
- **理由**: SPEC 3.3「単一インスタンス制約」。WPF 標準の単一インスタンス機構（`WindowsFormsApplicationBase`）は WinForms 依存のため不採用。Mutex + NamedPipe は WPF での一般的なパターンで依存追加なし。パイプスレッドはバックグラウンドスレッドで常時待機し、受信時に `Dispatcher.Invoke` で UI スレッドに切り替えて `MainWindow.OpenApplyWindow()` を呼ぶ。

### 18. SettingsWindow は OnClosing でも Save() を呼ぶ
- **判断**: `Close_Click` と `OnClosing` 両方で `_vm.Save()` を呼ぶ（二重 Save は冪等）
- **理由**: ×ボタンでウィンドウを閉じた場合も設定が保存されるべき（SPEC F-14「設定の永続化」）。`SetSetting` は `INSERT OR REPLACE` のため二重呼び出しは問題なし。

### 19. COM ハンドラは専用 DLL + Packaged COM SurrogateServer
- **判断**: `Folderly.ContextMenu` プロジェクトを追加し、`Folderly.ContextMenu.comhost.dll` を MSIX に同梱する。`Package.appxmanifest` は `com:SurrogateServer` + `desktop4:FileExplorerContextMenus` で `IExplorerCommand` を登録する。
- **理由**: Explorer の File Explorer context menu は Packaged COM の DLL surrogate 方式が安定する。`ExeServer` 方式ではメニュー登録・起動の互換性問題が出たため分離した。
- **COM インターフェース**: `IExplorerCommand` の IID は `A08CE4D0-FA25-44AB-B57C-C7B1C323E0B9`。誤った IID だとメニューは表示されても `Invoke` が動かない。
- **フォルダパスの受け渡し**: `Invoke` → `GetDisplayName(SIGDN_FILESYSPATH=0x80058000)` → package ルートの `Folderly.exe` を対象フォルダ引数付きで起動する。
- **診断ログ**: 右クリック Invoke は `%LOCALAPPDATA%\Folderly\context-menu.log` に記録する。

### 20. MSIX 版の実行依存関係
- **判断**: `Folderly.App` は framework-dependent とし、`.NET Desktop Runtime 8` を前提にする。SQLite は `SQLitePCLRaw.bundle_e_sqlite3` を参照し、`e_sqlite3.dll` を package 出力にコピーする。
- **理由**: MSIX 手動パックでは framework-dependent の runtimeconfig とネイティブ SQLite DLL の同梱が必要。`includedFrameworks` の runtimeconfig や `e_sqlite3.dll` 欠落は起動時クラッシュの原因になる。

### 21. Shell 通知は folder / desktop.ini / directory update をまとめて送る
- **判断**: 適用後に `SHCNE_UPDATEIMAGE`、フォルダ `SHCNE_UPDATEITEM`、`desktop.ini` `SHCNE_UPDATEITEM`、フォルダ `SHCNE_UPDATEDIR` を送る。
- **理由**: `desktop.ini` と folder system 属性は正しく設定されても Explorer が即時再評価しない場合があるため、対象メタデータの変更を明示的に通知する。

### 22. ICO ファイル名にコンテンツハッシュ 8 文字を付加（`cover_<hash8>.ico`）
- **判断**: `ApplyService` が ICO を生成する際、ICO バイト列の SHA256 先頭 8 文字をファイル名に含める（例: `cover_897add9f.ico`）。再適用時は新しい名前で保存し、旧 `cover_*.ico` を削除する。
- **理由**: Explorer はフォルダ + ICO パスの組み合わせをアイコンキャッシュのキーにするため、同名ファイルを上書きしても古いアイコンが残り続ける。パス自体を変えることでキャッシュを強制無効化する（コミット `d5aa336`）。

### 23. アイコン保存ディレクトリは `_folderly`（ドット無し）
- **判断**: 元の `.folderly`（ドット始まり）から `_folderly`（アンダースコア始まり）に変更。旧パスとの後方互換は Revert / ShellNotifier / MainViewModel の各所でフォールバック列挙を実装。
- **理由**: OneDrive Files-On-Demand はドット始まりの隠しディレクトリを「除外リスト」として dehydrate/削除する挙動がある。`_folderly` に変更すると OneDrive が通常フォルダとして扱い、`ReparsePoint` 属性付きで同期対象になることを実機（`C:\Users\625so\OneDrive\...`）で確認（コミット `70ee0d0`）。

## 次セッション申し送り事項（Store 申請前）

- Step 17/17.5 は完了（即時反映・Revert・再適用・OneDrive 互換が全て確認済み）
- Step 18 は手動テスト継続中。残り項目は [docs/TESTING.md](docs/TESTING.md) と [HANDOVER.md](HANDOVER.md) 参照
- FolderTemplate.png は現在シンプルな2色の矩形。WPF プレビューの視覚品質に合わせて最終調整すること
- `StoreLicenseService` の StoreContext 実装は MSIX 環境でのみ動作確認可能
- Store 申請前に Publisher を Partner Center の CN=... に変更すること（Package.appxmanifest コメント参照）

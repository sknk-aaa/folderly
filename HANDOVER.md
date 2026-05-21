# Folderly 引き継ぎドキュメント（WSL2 → Windows）

このドキュメントは WSL2 で実装したコードを、**Windows 環境の Claude Code**が継続実装・検証するための引き継ぎ書です。

---

## プロジェクト概要

**Folderly v1.0** — Windows フォルダのアイコンを「表紙画像 + 色タグ」でカスタマイズする C#/.NET 8 デスクトップアプリ（WPF + MSIX）。

- 仕様: [SPEC.md](SPEC.md)
- 実装判断ログ: [CLAUDE.md](CLAUDE.md)（21項目）
- 手動テストチェックリスト: [docs/TESTING.md](docs/TESTING.md)
- Microsoft Store 提出準備: [docs/STORE_SUBMISSION.md](docs/STORE_SUBMISSION.md)
- Store 掲載文下書き: [docs/STORE_LISTING_DRAFT.md](docs/STORE_LISTING_DRAFT.md)
- プライバシーポリシー下書き: [docs/PRIVACY_POLICY_DRAFT.md](docs/PRIVACY_POLICY_DRAFT.md)
- ビルド手順: [README.md](README.md)

---

## 完了済み Step

| Step | 名前 | 状態 |
|------|------|------|
| 1  | プロジェクトスケルトン | 完了 |
| 2  | Core - FolderTemplate + TagColors | 完了 |
| 3  | Core - ImageAdjuster | 完了 |
| 4  | Core - TemplateRenderer | 完了 |
| 5  | Core - IcoConverter | 完了 |
| 6  | Core - DesktopIniManager + FolderAttributesService | 完了 |
| 7  | Shell - ShellNotifier + IShellNotifier | 完了（通知だけでは不安定なため、最終的に対象 Explorer ウィンドウ開き直しで安定化） |
| 8  | Core - HistoryRepository | 完了 |
| 9  | Core - FolderProtection | 完了 |
| 10 | Core - ApplyService + RevertService | 完了 |
| 11 | WPF App スケルトン + DI（AppServices） | 完了 |
| 12 | LocalizationService + 言語切替 | 完了 |
| 13 | MainWindow / ApplyWindow / SettingsWindow | 完了 |
| 14 | FileLogger + Mutex/NamedPipe 単一インスタンス | 完了 |
| 15 | StoreLicenseService（試用版フォールバック） | 完了 |
| 16 | Resources/Brand.xaml + タグボタン動的生成 | 完了 |
| 17 | COM IExplorerCommand 右クリックメニュー | 実装完了・MSIX 動作確認済 |
| 18 | docs/TESTING.md + README.md 整備 | 完了（手動テストは継続中） |

Core テスト 110 件は Windows で全 pass 確認済み。

---

## 実装済みファイル一覧

### Core 層（net8.0）
- [src/Folderly.Core/Folderly.Core.csproj](src/Folderly.Core/Folderly.Core.csproj)
- [src/Folderly.Core/Composition/FolderTemplate.cs](src/Folderly.Core/Composition/FolderTemplate.cs)
- [src/Folderly.Core/Composition/TagColors.cs](src/Folderly.Core/Composition/TagColors.cs)
- [src/Folderly.Core/Composition/ImageAdjuster.cs](src/Folderly.Core/Composition/ImageAdjuster.cs)
- [src/Folderly.Core/Composition/TemplateRenderer.cs](src/Folderly.Core/Composition/TemplateRenderer.cs)
- [src/Folderly.Core/Conversion/IcoConverter.cs](src/Folderly.Core/Conversion/IcoConverter.cs)
- [src/Folderly.Core/Folder/DesktopIniManager.cs](src/Folderly.Core/Folder/DesktopIniManager.cs)
- [src/Folderly.Core/Folder/FolderAttributesService.cs](src/Folderly.Core/Folder/FolderAttributesService.cs)
- [src/Folderly.Core/Folder/FolderProtection.cs](src/Folderly.Core/Folder/FolderProtection.cs)
- [src/Folderly.Core/History/HistoryEntry.cs](src/Folderly.Core/History/HistoryEntry.cs)
- [src/Folderly.Core/History/HistoryRepository.cs](src/Folderly.Core/History/HistoryRepository.cs)
- [src/Folderly.Core/Shell/IShellNotifier.cs](src/Folderly.Core/Shell/IShellNotifier.cs)
- [src/Folderly.Core/Application/ApplyService.cs](src/Folderly.Core/Application/ApplyService.cs)
- [src/Folderly.Core/Application/RevertService.cs](src/Folderly.Core/Application/RevertService.cs)
- `src/Folderly.Core/Resources/FolderTemplate.png`（EmbeddedResource）

### Shell 層（net8.0-windows）
- [src/Folderly.Shell/Folderly.Shell.csproj](src/Folderly.Shell/Folderly.Shell.csproj)
- [src/Folderly.Shell/NativeMethods.cs](src/Folderly.Shell/NativeMethods.cs)
- [src/Folderly.Shell/ShellNotifier.cs](src/Folderly.Shell/ShellNotifier.cs)

### App 層（net8.0-windows10.0.17763.0、WPF）
- [src/Folderly.App/Folderly.App.csproj](src/Folderly.App/Folderly.App.csproj)
- [src/Folderly.App/App.xaml](src/Folderly.App/App.xaml) / [App.xaml.cs](src/Folderly.App/App.xaml.cs)
- [src/Folderly.App/ContextMenuHandler.cs](src/Folderly.App/ContextMenuHandler.cs)（App 側に残置、現在は ContextMenu プロジェクトが本体）
- [src/Folderly.App/Infrastructure/AppServices.cs](src/Folderly.App/Infrastructure/AppServices.cs)
- [src/Folderly.App/Infrastructure/Converters.cs](src/Folderly.App/Infrastructure/Converters.cs)
- [src/Folderly.App/Infrastructure/FileLogger.cs](src/Folderly.App/Infrastructure/FileLogger.cs)
- [src/Folderly.App/Infrastructure/GlobalUsings.cs](src/Folderly.App/Infrastructure/GlobalUsings.cs)
- [src/Folderly.App/Infrastructure/ViewModelBase.cs](src/Folderly.App/Infrastructure/ViewModelBase.cs)
- [src/Folderly.App/MainWindow.xaml](src/Folderly.App/MainWindow.xaml) / [.cs](src/Folderly.App/MainWindow.xaml.cs)
- [src/Folderly.App/Resources/Brand.xaml](src/Folderly.App/Resources/Brand.xaml)
- [src/Folderly.App/Services/LocalizationService.cs](src/Folderly.App/Services/LocalizationService.cs)
- [src/Folderly.App/Services/StoreLicenseService.cs](src/Folderly.App/Services/StoreLicenseService.cs)
- [src/Folderly.App/ViewModels/ApplyViewModel.cs](src/Folderly.App/ViewModels/ApplyViewModel.cs)
- [src/Folderly.App/ViewModels/MainViewModel.cs](src/Folderly.App/ViewModels/MainViewModel.cs)
- [src/Folderly.App/ViewModels/SettingsViewModel.cs](src/Folderly.App/ViewModels/SettingsViewModel.cs)
- [src/Folderly.App/Views/ApplyWindow.xaml](src/Folderly.App/Views/ApplyWindow.xaml) / [.cs](src/Folderly.App/Views/ApplyWindow.xaml.cs)
- [src/Folderly.App/Views/Controls/FolderPreview.xaml](src/Folderly.App/Views/Controls/FolderPreview.xaml) / [.cs](src/Folderly.App/Views/Controls/FolderPreview.xaml.cs)
- [src/Folderly.App/Views/SettingsWindow.xaml](src/Folderly.App/Views/SettingsWindow.xaml) / [.cs](src/Folderly.App/Views/SettingsWindow.xaml.cs)

### ContextMenu 層（Packaged COM SurrogateServer）
- [src/Folderly.ContextMenu/Folderly.ContextMenu.csproj](src/Folderly.ContextMenu/Folderly.ContextMenu.csproj)
- [src/Folderly.ContextMenu/FolderlyContextMenuHandler.cs](src/Folderly.ContextMenu/FolderlyContextMenuHandler.cs)

### Package（MSIX）
- [src/Folderly.Package/Folderly.Package.wapproj](src/Folderly.Package/Folderly.Package.wapproj)
- [src/Folderly.Package/Package.appxmanifest](src/Folderly.Package/Package.appxmanifest)

### テスト
- [tests/Folderly.Tests/](tests/Folderly.Tests/)（Core テスト 110 件、全 pass 確認済）

---

## 仕様判断ログ（要約）

詳細は [CLAUDE.md](CLAUDE.md) を参照。重要なものを抜粋:

| # | 判断 | 補足 |
|---|------|------|
| 1 | `IShellNotifier` を Core に追加 | Core の OS 非依存性を維持（依存性逆転） |
| 2 | FolderTemplate.png は EmbeddedResource | SVG は ImageSharp が非対応のため不採用 |
| 3 | FolderAttributesService は .NET 標準 `FileAttributes` | P/Invoke 不要、WSL2 テスト可能 |
| 5 | INI パーサーは自前実装 | 外部ライブラリ不要 |
| 7 | ICO は全サイズ PNG 埋め込み | Windows Vista 以降対応 |
| 11 | App TFM は `net8.0-windows10.0.17763.0` | WinRT `StoreContext` のため |
| 13 | Localization は `{Binding L[Key]}` で即時切替 | `Binding.IndexerName` を発火 |
| 15 | StoreContext は try-catch でフォールバック | 非 MSIX 環境でも起動可能に |
| 17 | 単一インスタンス: Mutex + NamedPipe | パイプ名 `FolderlyIPC_v1` |
| 19 | COM ハンドラは別 DLL の SurrogateServer | `Folderly.ContextMenu.comhost.dll` |
| 19 | IExplorerCommand IID | `a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9`（要注意：間違えやすい） |
| 19 | Context menu CLSID | `2A7A05DA-70D8-4302-8B23-AE8D79D801B6` |
| 20 | SQLite ネイティブ DLL は `e_sqlite3.dll` を package に同梱必須 | 起動時クラッシュ防止 |
| 21 | Shell 通知は folder / desktop.ini / dir 全部送る | Explorer への内部更新通知。通知だけでは再適用が遅れるため、対象 Explorer ウィンドウ開き直しを併用 |

### テスト証明書（サイドロード用）
- CN=Folderly
- Thumbprint: `4A918D2D05D473471D912E7D12268D4768EA9249`

---

## 現在の重要仕様・解決済み項目

### Explorer 反映仕様（2026-05-21 現在・解決済み）

**結論**:
Folderly は適用成功後、対象フォルダまたは親フォルダを表示している Explorer ウィンドウだけを閉じて開き直す。`explorer.exe` 本体、タスクバー、スタートメニューは再起動しない。

**理由**:
Windows Explorer は `desktop.ini` と ICO が正しく更新されていても、フォルダアイコン/サムネイルキャッシュを保持して古い見た目を表示し続けることがある。特に同一フォルダへの A→B 再適用では、通知だけだと 30〜40 秒遅れるケースが実機で確認された。

**ユーザー向け説明**:
Store/説明文には「アイコン更新時に Explorer ウィンドウを開き直します。これは Windows のアイコンキャッシュ更新のための仕様です。」と記載する。

**実機テスト結果**:
- 画像A 初回適用: 数秒以内に反映 ✓
- 画像B 再適用: 30〜40秒待ちなし、数秒以内に反映 ✓
- 画像C 再適用: 数秒以内に反映 ✓
- 対象 Explorer ウィンドウの開き直しは軽い ✓
- タスクバー / スタートメニューの乱れなし ✓
- 「全フォルダを元に戻す」成功 ✓
- OneDrive 配下で A→B→C 成功 ✓

**採用した実装**:
- `src/Folderly.App/Views/ApplyWindow.xaml.cs`
  - 適用成功後に `ReopenExplorerWindowsAsync(folderPath)` を実行
  - `Shell.Application.Windows()` から対象フォルダ/親フォルダを表示している Explorer ウィンドウを探す
  - 該当ウィンドウだけ `Quit()` し、同じパスを `explorer.exe "<path>"` で開き直す
- `src/Folderly.App/Views/SettingsWindow.xaml`
  - 設定名: 「フォルダアイコン適用後に Explorer ウィンドウを開き直す」
  - 既定オン

**試みた手法と結果**:

| 手法 | 結果 | 判断 |
|------|------|------|
| `SHGetFileInfo + SHCNE_UPDATEIMAGE | SHCNF_DWORD` | 初回は効くが、再適用は 30〜40 秒遅れる場合あり | 補助通知として維持 |
| PATH/PIDL の `SHChangeNotify` | 同上 | 補助通知として維持 |
| `Shell.Application.Document.Refresh()` | 開いているウィンドウの再描画には効くが、古いアイコンキャッシュが残る場合あり | 補助処理として維持 |
| `SHCNE_ASSOCCHANGED` | 初回適用まで 20 秒程度に悪化 | 削除済み |
| `SHCNE_RMDIR + SHCNE_MKDIR` | 黄色フォルダへ戻る瞬間が出る | 廃棄 |
| `ToggleSystemReadOnly` | OneDrive 同期を誘発し遅延悪化 | 廃棄 |
| `IThumbnailCache::GetThumbnail(WTS_FORCEEXTRACTION)` | 15〜20 秒ブロック | 廃棄 |
| `explorer.exe` 本体再起動 | 反映は確実だが Start/タスクバーが乱れる | 廃棄 |
| 対象 Explorer ウィンドウだけ開き直し | A/B/C すべて数秒以内、再起も軽い | 採用 |

### 修正済みバグ（2026-05-21）

以下はコードと MSIX 実機で確認済み:

| バグ | 修正コミット | 概要 |
|------|------------|------|
| Revert が OneDrive 配下で失敗 | `d5aa336` 以降 | 属性クリア → 操作 → 最大 5 回リトライに変更 |
| 再適用時に画像が更新されない / 30〜40 秒待ち | `0453fc2` | 対象 Explorer ウィンドウだけを開き直し、Explorer キャッシュを最新化 |
| Explorer 本体再起動で Start/タスクバーが乱れる | `0453fc2` | `explorer.exe` 本体 kill を廃止し、対象ウィンドウだけ `Quit()` |
| OneDrive 配下の ICO 参照が不安定 | `2770a81` 以降 | `desktop.ini` の参照先を `%LOCALAPPDATA%\Folderly\icons\cover_<hash>.ico` に変更 |
| 再適用時に Revert 用バックアップが壊れる | `78d3464` | History upsert 時に元状態バックアップを保持 |
| 「全フォルダを元に戻す」が実フォルダを戻さない/残骸が残る | `c9460d2` | 履歴 entries の Revert と、Desktop/Documents/OneDrive 周辺の orphan Folderly 残骸掃除を追加 |
| `SHCNE_ASSOCCHANGED` で初回適用まで遅くなる | `c8a1108` | 重い関連付け更新通知を削除 |
| MSIX後の手動 Explorer 再起動が重い | `a412b8c` | README の手順を軽量化。通常はインストール後に Explorer 本体再起動しない |

### 手動テスト未完了項目（[docs/TESTING.md](docs/TESTING.md)）

- [ ] 日本語フォルダ名で文字化けなく適用できること
- [ ] docs/TESTING.md 全項目を上から実施

---

## Windows 環境に移行後の注意点

### 1. まず最初にやること

```powershell
cd C:\path\to\folderly
git pull
git status
```

2026-05-21 時点の重要コミットは `0453fc2 fix: Explorer本体ではなく対象ウィンドウを開き直す`。これ以降の履歴を確認すること。

### 2. ビルド & MSIX パック手順

[README.md](README.md) の PowerShell スクリプトを使う。要点:

```powershell
# Folderly.App / Folderly.ContextMenu / Folderly.Package を Release/x64 でビルド
# makeappx pack で .msix 生成
# signtool sign でテスト証明書（Thumbprint: 4A918D2D05D473471D912E7D12268D4768EA9249）で署名
# Add-AppxPackage でインストール
```

### 3. SQLite ネイティブ DLL

`e_sqlite3.dll` が MSIX package 出力に含まれていないと起動時クラッシュ。`Folderly.App.csproj` で copy する設定が入っているはず。MSIX 内に存在するか確認すること。

### 4. WSL2 では検証不可だった項目

以下は **Windows でしか検証できない**:
- `Folderly.Shell` のビルド（`net8.0-windows`）
- `Folderly.App` のビルド・実行（WPF）
- `Folderly.ContextMenu` のビルド・COM 登録
- MSIX 全般（パック・署名・サイドロード）
- `SHChangeNotify` の挙動
- `StoreContext`（試用版・購入判定）
- Explorer のアイコン更新挙動

### 5. デバッグログの場所

- アプリログ: `%LOCALAPPDATA%\Folderly\logs\`
- 右クリックメニュー Invoke ログ: `%LOCALAPPDATA%\Folderly\context-menu.log`

### 6. ハマりポイント

- IExplorerCommand の IID は `a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9`。間違えるとメニューは出るが Invoke が動かない。
- COM の登録は Packaged COM SurrogateServer 方式（`com:SurrogateServer` + `Folderly.ContextMenu.comhost.dll`）。ExeServer 方式に戻すと不安定。
- `Package.appxmanifest` の Publisher は Partner Center の `CN=...` に Store 申請前に変更すること。

### 7. ユーザのルール（必ず守る）

- 日本語で返答
- コード変更したら commit まで実行（push はユーザが行う）
- `git push --force` 禁止
- コミットメッセージは日本語
- `.env` / 秘密鍵はコミットしない

---

## 次にやるべきこと

### Step 17.5: Explorer 反映（2026-05-21 解決済み）

- 初回適用 / 同フォルダ再適用 / A→B→C 連続適用 は数秒以内に反映 ✓
- 通知だけではなく、対象 Explorer ウィンドウを開き直して Explorer キャッシュを最新化する仕様
- Explorer 本体・タスクバー・スタートメニューは再起動しない
- Store/説明文では「アイコン更新時に Explorer ウィンドウを開き直します。これは Windows のアイコンキャッシュ更新のための仕様です。」と明記する

### Step 18 残作業: docs/TESTING.md 完走

[docs/TESTING.md](docs/TESTING.md) を上から順に実施し、全てのチェックを埋める。残り優先項目:
- 日本語フォルダ名で文字化けなく適用できること
- 画像調整（クロップ/拡大率スライダー/ドラッグ/中央リセット）
- WebP 画像で適用
- 長いパス（260 文字超）で警告ダイアログ
- 既存 desktop.ini ありフォルダで他キーが保持される
- タグ機能（各色での適用）
- 保護機能（C:\Windows, C:\Program Files, ドライブルート等）
- OneDrive 配下の警告ダイアログ
- Explorer ウィンドウ開き直し設定の ON/OFF
- 設定画面: 言語切替（即時反映）・履歴最大件数
- アンインストール後に右クリックメニューが消える
- 試用版バナー表示

### Step 19 以降（Store 申請）

- [docs/STORE_SUBMISSION.md](docs/STORE_SUBMISSION.md) に沿って提出素材を準備
- `Package.appxmanifest` の Publisher を Partner Center の正式な `CN=...` に変更
- 製品アイコンを正式版に差し替え
- Store 用スクリーンショット、説明文、Privacy Policy URL、サポートURLを準備
- Store提出用パッケージ（`.msixupload` / `.appxupload` 推奨）を作成
- Partner Center でパッケージをアップロード

---

## 参考リンク

- 実装計画: `/home/aaa/.claude/plans/folderly-windows-cheeky-cloud.md`（WSL2 側のみ）
- SPEC: [SPEC.md](SPEC.md)
- 実装判断ログ: [CLAUDE.md](CLAUDE.md)
- ビルド手順: [README.md](README.md)
- テストチェックリスト: [docs/TESTING.md](docs/TESTING.md)
- Store提出準備: [docs/STORE_SUBMISSION.md](docs/STORE_SUBMISSION.md)
- Store掲載文下書き: [docs/STORE_LISTING_DRAFT.md](docs/STORE_LISTING_DRAFT.md)
- プライバシーポリシー下書き: [docs/PRIVACY_POLICY_DRAFT.md](docs/PRIVACY_POLICY_DRAFT.md)

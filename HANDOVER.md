# Folderly 引き継ぎドキュメント（WSL2 → Windows）

このドキュメントは WSL2 で実装したコードを、**Windows 環境の Claude Code**が継続実装・検証するための引き継ぎ書です。

---

## プロジェクト概要

**Folderly v1.0** — Windows フォルダのアイコンを「表紙画像 + 色タグ」でカスタマイズする C#/.NET 8 デスクトップアプリ（WPF + MSIX）。

- 仕様: [SPEC.md](SPEC.md)
- 実装判断ログ: [CLAUDE.md](CLAUDE.md)（21項目）
- 手動テストチェックリスト: [docs/TESTING.md](docs/TESTING.md)
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
| 7  | Shell - ShellNotifier + IShellNotifier | 完了（即時反映調整は継続中） |
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

Core テスト 88 件は WSL2 で全 pass 確認済み。

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
- [tests/Folderly.Tests/](tests/Folderly.Tests/)（Core テスト 88 件、全 pass 確認済）

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
| 21 | Shell 通知は folder / desktop.ini / dir 全部送る | 即時反映のため |

### テスト証明書（サイドロード用）
- CN=Folderly
- Thumbprint: `4A918D2D05D473471D912E7D12268D4768EA9249`

---

## 未完了・継続中の項目

### 【最優先】Explorer 即時反映の問題

**症状**: アイコン適用後、**親ディレクトリの一覧に表示されるフォルダアイコン**が更新されない。

**確認済み事実**:
- `desktop.ini` と `cover.ico` は正しく生成されている（フォルダを開くと Explorer のタイトルバー・タスクバーには custom icon が表示される）
- 以下を実施しても親ディレクトリ表示は更新されない:
  - 全 SHChangeNotify 系の呼び出し（PATH / PIDL / UPDATEIMAGE / UPDATEITEM / UPDATEDIR / ATTRIBUTES / ASSOCCHANGED / RENAMEFOLDER 自己リネーム）
  - `iconcache_*.db` の削除 + Explorer 再起動

**最新の試み**（コミット `b5fd369`、まだユーザは MSIX 未ビルド）:
- [src/Folderly.Shell/ShellNotifier.cs](src/Folderly.Shell/ShellNotifier.cs#L61-L77) の `ForceIconIndexUpdate`: `SHGetFileInfo` でシステムイメージリストを強制更新し、特定の icon index に対して `SHCNE_UPDATEIMAGE | SHCNF_DWORD` を発火

**未試行のアプローチ**:
1. **C:\ ルート直下以外のフォルダ**（例: `C:\Users\<user>\Desktop\TestFolder`）でテスト → C:\ ルート固有問題か切り分け
2. `SHCNE_RMDIR` + `SHCNE_MKDIR` トリック（フォルダ削除→再作成を装う）
3. Explorer のタブ/ウィンドウを一旦閉じて開き直したらどうか
4. `IShellItem` / `IShellFolder::ParseDisplayName` 経由で明示的にメタデータ再取得

### 手動テスト未完了項目（[docs/TESTING.md](docs/TESTING.md)）

「要再確認」セクション:
- [ ] 適用後に Explorer 再起動なしで即時反映されること ← 上記の最優先課題
- [ ] 元に戻すが MSIX 版で完全復元すること
- [ ] 日本語フォルダ名で文字化けなく適用できること
- [ ] docs/TESTING.md 全項目を上から実施

---

## Windows 環境に移行後の注意点

### 1. まず最初にやること

```powershell
cd C:\path\to\folderly
git pull
git status   # PROMPT.md の変更が残っているかも（無視してよい）
```

WSL2 側の最新コミットは `b5fd369 fix: force icon index update via SHGetFileInfo`。これが pull できているか確認。

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

### Step 17.5（暫定）: Explorer 即時反映の決着

**前提**: 最新コミット `b5fd369` を含む MSIX をビルド・インストール。

#### 手順A: SHGetFileInfo アプローチの効果検証

1. 既存のテストフォルダで一度「元に戻す」
2. 新しい MSIX をインストール
3. アイコン適用 → Explorer 再起動なしで親ディレクトリ表示が更新されるか確認

→ 更新されれば **完了**、コミットしてユーザに報告。

#### 手順B: 切り分けテスト（手順 A が失敗した場合）

異なる場所のフォルダで挙動を比較:
- `C:\` ルート直下のフォルダ
- `C:\Users\<user>\Desktop\Test1`
- `C:\Users\<user>\Documents\Test2`

C:\ ルート固有問題なのか、Windows 全体の挙動なのかを切り分ける。

#### 手順C: 次の実装案（手順 B でも改善しない場合）

[src/Folderly.Shell/ShellNotifier.cs](src/Folderly.Shell/ShellNotifier.cs) に以下を追加:

1. **RMDIR + MKDIR トリック**: フォルダ削除→再作成を装う通知
   ```csharp
   NotifyPath(folderPath, SHCNE_RMDIR);
   NotifyPath(folderPath, SHCNE_MKDIR);
   ```

2. **親フォルダの中身列挙を強制**: `SHCNE_UPDATEDIR` を親に対して PIDL で複数回

3. それでもダメなら、**`IShellFolder::ParseDisplayName`** で明示的に再パースを試す（複雑なので最終手段）

### Step 18 残作業: docs/TESTING.md 完走

即時反映問題が解決したら、[docs/TESTING.md](docs/TESTING.md) を上から順に実施し、全てのチェックを埋める。

### Step 19 以降（Store 申請）

- `Package.appxmanifest` の Publisher を Partner Center の正式な `CN=...` に変更
- 製品アイコン（44x44, 50x50, 150x150, 310x310 等）を正式版に差し替え
- Store 用スクリーンショット、説明文の準備
- Partner Center で MSIX をアップロード

---

## 参考リンク

- 実装計画: `/home/aaa/.claude/plans/folderly-windows-cheeky-cloud.md`（WSL2 側のみ）
- SPEC: [SPEC.md](SPEC.md)
- 実装判断ログ: [CLAUDE.md](CLAUDE.md)
- ビルド手順: [README.md](README.md)
- テストチェックリスト: [docs/TESTING.md](docs/TESTING.md)

# Folderly Agent Notes

Folderly は Windows フォルダのアイコンをカバー画像とカラータグでカスタマイズするデスクトップアプリ。
WPF + WebView2 エディタ + ImageSharp + SQLite + MSIX（Explorer コンテキストメニュー付き）。

## Docs

- [docs/DESIGN.md](docs/DESIGN.md) — 仕様・データモデル・実装詳細
- [docs/OPERATIONS.md](docs/OPERATIONS.md) — ビルド・パッケージ・Store 提出・テスト
- [docs/HANDOFF.md](docs/HANDOFF.md) — 現状・残タスク・既知の問題
- [docs/TESTING.md](docs/TESTING.md) — 手動テストチェックリスト

## Current State

- Current Store package version: `1.0.17.0`
- Current Store candidate: `_out\Folderly_1.0.17.0_x64_store.msix`
- Tests: `dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"`

## Non-Negotiable Implementation Contracts

### Preview Performance

- `mousemove` ごとに正確レンダリングを実行しない。`transformPreview` でスロットル（50ms）、コミットは mouseup または遅延後。
- プレビュードラッグ中は X/Y スライダーのサムを動かさない。
- `scale`・`offsetX`・`offsetY`・`cropMode` は必ず4値まとめて `transform`/`transformPreview` メッセージに含める。cropMode を分離して送るとリグレッションが起きる。
- jank が出たら `ApplyWindow.html` の `scheduleTransformPreviewPost`・`scheduleTransformPost`・`postTransformNow`・`commitOffsetFromPreview` を確認。

### Preview/Final Icon Consistency

- `FolderTemplate.GetImageRegionPixelSize()` が画像領域サイズのソース。`TemplateRenderer` とプレビューコードはこれに従う。
- プレビューだけ合わせて最終 ICO がずれる修正、またはその逆はしない。

### Source Image Restoration

- 管理済みソース画像ディレクトリ: `%LOCALAPPDATA%\Folderly\source-images\`
- 履歴は管理済みコピーのパスを保持する。
- 参照されていない管理済みソース画像は reapply/revert 時にクリーンアップする。

### Explorer Refresh

- シェル通知を残す。
- 対象 Explorer ウィンドウの再オープン動作を残す。
- 通常の apply/install で Explorer プロセス全体を kill・再起動しない。

### UI Scope

- 画像エントリポイントはドロップエリア1箇所のみ。下部の重複ボタンは復活させない。
- `Reset image` で画像をクリアする。
- カスタムタグ作成が実装されるまで `Add new tag` を表示しない。
- Explorer でのタグソートはスコープ外。

## Important Files

- `src/Folderly.App/Resources/ApplyWindow.html`: エディタ UI と操作ロジック
- `src/Folderly.App/Views/ApplyWindow.xaml.cs`: WebView ブリッジ、画像ロード、apply、Explorer リフレッシュ
- `src/Folderly.Core/Application/ApplyService.cs`: apply パイプライン
- `src/Folderly.Core/Application/RevertService.cs`: revert パイプライン
- `src/Folderly.Core/Application/ManagedSourceImageStore.cs`: 管理済みソース画像クリーンアップ
- `src/Folderly.Core/Composition/FolderTemplate.cs`: テンプレートジオメトリ
- `src/Folderly.Core/Composition/TemplateRenderer.cs`: 最終アイコンレンダリング
- `src/Folderly.App/ContextMenuHandler.cs`: Explorer コンテキストメニュー EXE COM サーバー
- `src/Folderly.Package/Package.appxmanifest`: MSIX アイデンティティ、COM 登録、バージョン

## Packaging Notes

- `WebView2Loader.dll` はパッケージ出力ルートにある必要がある（`runtimes\win-x64\native` だけでは不足）。
- Store package identity: `KanekoApps.Folderly`
- Store publisher: `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`
- 旧 sideload ビルドの publisher は `CN=Folderly`（別 identity）。
- Visual Studio がこの環境で Publish / Store / Create App Packages を表示しないため、Store 候補は `makeappx` で手動作成。
- Microsoft Store は4桁目が非ゼロの MSIX を拒否する。`1.0.17.0` は OK、`1.0.0.17` は NG。

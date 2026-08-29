# Folderly Design

仕様の正（データモデル・画面仕様・対象/非対象）。変化が遅い。

## Product Summary

Windows のフォルダアイコンを以下の組み合わせでカスタマイズする：

- ユーザー選択のカバー画像
- Windows スタイルのフォルダテンプレート
- カラータグ
- 任意のタグ名・タグアイコンオーバーレイ

エントリポイントはエクスプローラーの右クリックメニュー「Customize with Folderly」および各対応言語相当のラベル。

## Target Platform

- Windows 10 1809 build 17763 以上
- .NET 8 WPF アプリ、self-contained（.NET Desktop Runtime の別途インストール不要）
- MSIX パッケージアプリ
- x64 確認済み
- Microsoft Store 配布対象：Windows 10/11 Desktop

## Tech Stack

| Area | Technology |
|---|---|
| App UI | WPF |
| Editor surface | WebView2 embedded HTML/CSS/JS |
| Image processing | SixLabors ImageSharp |
| Data | SQLite via `Microsoft.Data.Sqlite` |
| Shell integration | Packaged COM `IExplorerCommand` |
| Packaging | Windows Application Packaging Project / MSIX |
| Tests | xUnit |

## Project Layout

| Project | Purpose |
|---|---|
| `Folderly.Core` | 画像合成、ICO 変換、履歴、apply/revert ロジック |
| `Folderly.App` | WPF アプリ、WebView2 エディタ、設定、履歴 UI、out-of-process COM IExplorerCommand |
| `Folderly.Shell` | `SHChangeNotify` とシェルヘルパー |
| `Folderly.Package` | MSIX パッケージプロジェクト |
| `Folderly.Tests` | xUnit テスト |

## Runtime Data Paths

- 中央 ICO: `%LOCALAPPDATA%\Folderly\icons\`
- 管理済みソース画像コピー: `%LOCALAPPDATA%\Folderly\source-images\`
- ログ: `%LOCALAPPDATA%\Folderly\logs\`
- コンテキストメニューログ: `%LOCALAPPDATA%\Folderly\context-menu.log`
- フォルダローカルファイル: `<target folder>\_folderly\cover_<hash8>.ico`

## Explorer Context Menu

- フォルダ右クリックでローカライズ済み Folderly カスタマイズコマンドを表示。
- コマンド選択でそのフォルダの Folderly を起動。
- self-contained `Folderly.exe --com-server` out-of-process packaged COM ハンドラで実装。
- 旧 managed `Folderly.ContextMenu.comhost.dll` は削除済み（.NET COM hosting は self-contained 配布に非対応）。
- コンテキストメニューアイコンは透過アプリ/コンテキストアイコンを使用（Store 専用アイコンは使わない）。

## Localization

- アプリ UI は `src/Folderly.App/Resources/Strings.resx` を英語デフォルトとして、`Strings.ja.resx` / `Strings.es.resx` などのサテライトリソースでローカライズする。
- 設定の言語選択は `LocalizationService.SupportedLanguages` から生成する。
- 保存値は `system` または言語コード（例: `en`, `ja`, `es`）。
- `system` は Windows の UI カルチャを見て、対応済み言語ならその言語、未対応なら英語へフォールバックする。
- 新言語を追加するときは、`.resx` の全キーと `{0}` プレースホルダーが一致していることを `LocalizationResourceTests` で確認する。

## Image Editor

サポート操作：

- ドロップエリアクリックで画像選択（ファイルピッカーを開く）
- ドラッグ＆ドロップ
- 画像リセット（`Reset image`）
- プレビュードラッグで画像移動
- マウスホイールズーム
- スケールスライダー
- X/Y オフセットスライダー
- クロップモード：Center/crop、Fit width、Fit height
- Center/reset ポジションボタン
- Apply/Cancel

画像エントリポイントはドロップエリア1箇所のみ。下部の重複ボタンは削除済み。

## Preview Performance

Detailed implementation lessons and tradeoffs are recorded in [PREVIEW_NOTES.md](PREVIEW_NOTES.md).

プレビューは2つの更新パスを持つ：

- `transformPreview`: ドラッグ/スライダー操作中の高速スロットル更新（50ms）
- `transform`: マウスアップまたはディレイ後のコミット時の正確なレンダリング

ホイールズーム後の遅延正確レンダリング：180ms。

規則：
- `mousemove` ごとに正確レンダリングを実行しない。
- プレビュードラッグは `sliders.offsetX.set()` / `sliders.offsetY.set()` を連続呼び出ししない。X/Y スライダーとプレビュードラッグは独立した状態。
- `scale`・`offsetX`・`offsetY`・`cropMode` は一つのトランスフォーム状態として扱い、`transform`/`transformPreview` メッセージには必ず4値を含める。cropMode を分離して送ると Fit Width/Height 後のドラッグで画像が縮小するリグレッションが起きる。

jank が出た場合は `ApplyWindow.html` の以下を最初に確認する：
`scheduleTransformPreviewPost`、`scheduleTransformPost`、`postTransformNow`、`commitOffsetFromPreview`、`commitScaleFromPreview`、`mousemove` ハンドラ。

## Preview / Final Icon Consistency

WebView プレビューと最終 ICO は同じフォルダテンプレートジオメトリを共有する必要がある。

- `FolderTemplate.GetImageRegionPixelSize()` が画像領域サイズのソース。
- `TemplateRenderer` とプレビューコードはこれに従う。

過去のリグレッション：
- プレビューと最終出力で画像位置がずれた。
- イメージ領域の右/下端に黄色いフォルダ背景が見えた。
- スケール変更後に画像が左上に固定された。
- cropMode を分離して送信したため、Fit Width/Height 後のドラッグで画像が縮小した。
- ユーザー画像の透明パディング部分が黄色フォルダ台座でなく白パネルになった。

## Apply Flow

1. ユーザーがエクスプローラーコンテキストメニューまたはアプリ UI から Folderly を開く。
2. `ApplyWindow` が WebView2 を初期化し、状態を `ApplyWindow.html` に送信。
3. 対象フォルダに既存履歴と `SourceImagePath` があれば `TryRestoreExistingCustomization()` で管理済み画像・設定（cropMode・scale・X/Y offset・tag）を復元。
4. Apply 時、`ApplyWindow` が現在のソース画像を PNG ストリームとして `ApplyService` に送信。
5. `ApplyService` がソース画像バイトを `%LOCALAPPDATA%\Folderly\source-images\<sha256>.png` にコピー。
6. 合成 ICO を `%LOCALAPPDATA%\Folderly\icons\<sha256>.ico` に書き込み。
7. ローカルコピーを `<folder>\_folderly\cover_<hash8>.ico` にも書き込み。
8. `desktop.ini` が相対パス `_folderly\cover_<hash8>.ico` を指す。中央 AppData ICO は履歴プレビュー用に残る。
9. 履歴を管理済みソース画像パスで upsert。
10. シェル通知を送信。
11. 設定が有効なら、対象 Explorer ウィンドウのみ再オープン。

## Managed Source Image Storage

Apply 時に現在のソース画像を以下にコピーする：

```text
%LOCALAPPDATA%\Folderly\source-images\<sha256>.png
```

履歴フィールド `HistoryEntry.SourceImagePath` がこのパスを保持する。これにより元ファイルが削除・移動されても将来のプレビュー復元が可能。

クリーンアップ：フォルダに再 Apply するとき、または Revert するとき、他の履歴エントリに参照されていなければ以前の管理済みソース画像を削除する。

注意：この機能導入前の古いドラッグ＆ドロップ履歴エントリはソースパスが空で、再 Apply するまで復元できない。

## Tag System

現在のタグは固定スロット。ユーザーは表示を編集できるが、任意の新タグは作れない。

サポート：
- 固定タグの選択・名前変更・色変更・アイコン選択
- 生成アイコン上のタグ名表示 ON/OFF
- 生成アイコン上のタグアイコン表示 ON/OFF
- `Add new tag` UI は削除済み（機能自体が存在しないため）

非サポート：
- 新規タグ作成
- 固定タグ削除
- フォルダへの複数タグ付与
- Explorer でのタグソート/グループ化
- Explorer カスタムプロパティ列

## Localization

日本語・英語 UI をサポート。英語モードでは以下を翻訳する：
- エクスプローラーコンテキストメニューラベル（`Customize with Folderly`）
- 画像選択画面（`Select image`、`Reset image`）
- タグエディタ画面
- 設定ラベル（`Show tag name on folder icon` など）

## Apply Output

```text
%LOCALAPPDATA%\Folderly\icons\<sha256>.ico    ← 中央 ICO（履歴プレビュー用）
<target folder>\_folderly\cover_<hash8>.ico   ← フォルダローカルコピー
<target folder>\desktop.ini                    ← 相対パスで上記を参照
```

視覚的更新ごとに `cover_<hash8>.ico` のパスが変わる（Explorer のキャッシュ回避）。

## Revert

- 元のフォルダ属性を復元
- `desktop.ini` を以前の内容に戻す（Folderly が作成した場合は削除）
- `_folderly` を削除
- レガシー `.folderly` があれば削除
- 履歴エントリを削除
- 参照されていない管理済みソース画像を削除
- シェルリフレッシュ通知を送信

## Explorer Refresh

Explorer はフォルダアイコンを積極的にキャッシュする。同じフォルダに画像 A→B と適用してもシェル通知だけでは反映されないケースがある。Folderly は以下を行う：

1. `SHGetSetFolderCustomSettings(..., FCS_FORCEWRITE)` を呼び出す
2. シェル通知を送信
3. 設定が有効なら、対象フォルダまたは親フォルダを表示する Explorer ウィンドウのみ再オープン

通常の apply/revert で Explorer プロセス全体を kill・再起動してはならない。

## Important Files

| File | Purpose |
|---|---|
| `src/Folderly.App/Resources/ApplyWindow.html` | WebView2 エディタ UI と操作ロジック |
| `src/Folderly.App/Views/ApplyWindow.xaml.cs` | WebView ブリッジ、画像ロード、apply、Explorer リフレッシュ |
| `src/Folderly.Core/Application/ApplyService.cs` | Apply パイプライン |
| `src/Folderly.Core/Application/RevertService.cs` | Revert パイプライン |
| `src/Folderly.Core/Application/ManagedSourceImageStore.cs` | 管理済みソース画像クリーンアップ |
| `src/Folderly.Core/Composition/FolderTemplate.cs` | フォルダジオメトリ |
| `src/Folderly.Core/Composition/TemplateRenderer.cs` | 最終アイコン合成 |
| `src/Folderly.App/ContextMenuHandler.cs` | Explorer コマンドハンドラ（self-contained EXE がホスト） |
| `src/Folderly.Package/Package.appxmanifest` | MSIX アイデンティティと COM 登録 |

## Out of Scope

- Explorer でのタグソート/グループ化・カスタム列/プロパティ
- 任意の新規タグ作成・複数タグ
- 複数フォルダへの一括適用
- 設定のクラウド同期
- テレメトリ
- アプリ内での Store 自動更新ロジック
- アイコンパックの組み込みマーケットプレイス

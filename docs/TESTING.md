# 手動テストチェックリスト

Folderly v1.0 の手動テスト手順です。Windows 環境で実施してください。

## 実施記録

### 2026-05-21 / Windows x64 / MSIX 1.0.0.1

確認済み:

- [x] MSIX を手動 `makeappx` で作成できる
- [x] テスト証明書で MSIX に署名できる
- [x] MSIX をサイドロードできる
- [x] `Get-AppxPackage *Folderly*` で `Architecture : X64` / `Status : Ok` を確認
- [x] スタートメニューから Folderly が起動する
- [x] フォルダ右クリックに「Folderly でカスタマイズ」が表示される
- [x] 右クリックメニューから画像選択画面が起動する
- [x] 適用完了メッセージが表示される
- [x] `_folderly\cover_<hash8>.ico` が生成される
- [x] `desktop.ini` が生成され、`IconResource` / `IconFile` / `IconIndex` が設定される
- [x] `IconResource` は `%LOCALAPPDATA%\Folderly\icons\cover_<hash>.ico,0` を指す
- [x] 新規フォルダへ画像Aを適用 → 対象 Explorer ウィンドウが軽く開き直り、数秒以内に反映される
- [x] 同じフォルダへ画像Bを再適用 → 30〜40秒待たず、対象 Explorer ウィンドウの開き直し後に数秒以内で反映される
- [x] 続けて画像Cを再適用 → 同様に数秒以内で反映される
- [x] 適用後の反映処理で Explorer 本体は再起動しない
- [x] タスクバー / スタートメニューの乱れが発生しない
- [x] 「全フォルダを元に戻す」で履歴あり・履歴なしの Folderly 残骸を掃除できる
- [x] OneDrive 配下で A→B→C の反映が成功する
- [x] `dotnet test tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"` が 110 件 pass

現行仕様:

- Windows Explorer のフォルダアイコンキャッシュ都合により、通知だけでは A→B のような同一フォルダ再適用が 30〜40 秒遅れることがある。
- Folderly は安定反映のため、適用成功後に対象フォルダまたは親フォルダを表示している Explorer ウィンドウだけを開き直す。
- `explorer.exe` 本体、タスクバー、スタートメニューは再起動しない。
- Store/説明文では「アイコン更新時に Explorer ウィンドウを開き直します。これは Windows のアイコンキャッシュ更新のための仕様です。」と説明する。

要再確認:

- [ ] 日本語フォルダ名で文字化けなく適用できること
- [ ] `docs/TESTING.md` の全チェック項目を上から実施すること

既知の調整:

- 右クリックメニューは `Folderly.ContextMenu.comhost.dll` を使う Packaged COM SurrogateServer 方式。
- MSIX 起動には `Microsoft.WindowsDesktop.App 8.0.x` が必要。
- SQLite 用に `SQLitePCLRaw.bundle_e_sqlite3` と `e_sqlite3.dll` の同梱が必要。
- Explorer への内部通知は `SHChangeNotify` を PATH/PIDL 両方で送る。ただし通知だけでは同一フォルダ再適用が遅れる環境があるため、最終的に対象 Explorer ウィンドウを開き直す。
- ICO ファイルは `%LOCALAPPDATA%\Folderly\icons\cover_<hash>.ico` に保存し、`desktop.ini` からそこを参照する。OneDrive 配下の `_folderly` が ReparsePoint になる環境でも、Explorer がローカル ICO を読めるようにするため。
- Revert（元に戻す）は desktop.ini / _folderly / 旧 `.folderly` の Hidden+System 属性を先にクリアしてから削除する。`IOException`/`UnauthorizedAccessException` に対して最大 5 回の指数バックオフリトライを行う。
- 「全フォルダを元に戻す」は履歴DBの削除だけでなく、Desktop / Documents / OneDrive 周辺に残った Folderly 管理の `desktop.ini` / `_folderly` / `.folderly` も掃除する。

### 2026-05-20 / Windows x64 / MSIX 1.0.0.1

確認済み:

- [x] MSIX を手動 `makeappx` で作成できる
- [x] テスト証明書で MSIX に署名できる
- [x] MSIX をサイドロードできる
- [x] `Get-AppxPackage *Folderly*` で `Architecture : X64` / `Status : Ok` を確認
- [x] スタートメニューから Folderly が起動する
- [x] フォルダ右クリックに「Folderly でカスタマイズ」が表示される
- [x] 右クリックメニューから画像選択画面が起動する
- [x] 適用完了メッセージが表示される
- [x] `_folderly\cover_<hash8>.ico` が生成される（ハッシュ付き命名で Explorer キャッシュを無効化）
- [x] `desktop.ini` が生成され、`IconResource=_folderly\cover_<hash8>.ico,0` が設定される
- [x] Explorer 再起動後にフォルダアイコンが反映される
- [x] 適用後に Explorer 再起動なしで親ディレクトリのフォルダアイコンが即時反映される（当時の確認。現在は安定化のため対象 Explorer ウィンドウを開き直す仕様）
  - 検証場所: `C:\Users\625so\Desktop\FolderlyTest_A`, `C:\Users\625so\Documents\FolderlyTest_B` の両方で成功
  - 効いた実装: コミット `b5fd369` の `ShellNotifier.ForceIconIndexUpdate`（`SHGetFileInfo` でシステムイメージリストを強制更新 → 取得した icon index に対して `SHCNE_UPDATEIMAGE | SHCNF_DWORD` を発火）

要再確認:

- [x] `元に戻す` が MSIX 版で完全復元すること（その後「全フォルダを元に戻す」も履歴外残骸掃除まで対応済み）
- [x] 一度適用済みフォルダへの再適用でアイコンが更新されること（最終的には対象 Explorer ウィンドウを開き直す仕様で安定化）
- [ ] 日本語フォルダ名で文字化けなく適用できること
- [ ] `docs/TESTING.md` の全チェック項目を上から実施すること

既知の調整:

- 右クリックメニューは `Folderly.ContextMenu.comhost.dll` を使う Packaged COM SurrogateServer 方式。
- MSIX 起動には `Microsoft.WindowsDesktop.App 8.0.x` が必要。
- SQLite 用に `SQLitePCLRaw.bundle_e_sqlite3` と `e_sqlite3.dll` の同梱が必要。
- Explorer の内部更新通知として `SHChangeNotify` は PATH/PIDL 両方で通知し、`SHCNE_RENAMEFOLDER` 自己リネームトリックも使用する。ただし同一フォルダ再適用の安定反映は、対象 Explorer ウィンドウの開き直しで担保する。
- ICO ファイルは現在 `%LOCALAPPDATA%\Folderly\icons\cover_<hash>.ico` に保存し、`desktop.ini` から絶対パス参照する。OneDrive 配下の `_folderly` が ReparsePoint になる環境でも Explorer がローカル ICO を読めるようにするため。
- Revert（元に戻す）は desktop.ini / _folderly ファイルの Hidden+System 属性を先にクリアしてから削除。`IOException`/`UnauthorizedAccessException` に対して最大 5 回の指数バックオフリトライを実装（コミット `d5aa336`）。
- 中身があるフォルダへの適用: Explorer が content-preview サムネイルをキャッシュするため通知だけでは遅延する場合がある。現在は対象 Explorer ウィンドウを開き直して最新状態を読ませる。

## 前提
- Folderly がインストール済みであること（MSIX パッケージ）
- テスト用フォルダを任意の場所に作成しておくこと

---

## 基本動作

- [ ] 通常フォルダを右クリック → 「Folderly でカスタマイズ」が表示される
- [ ] メニュー選択で画像選択画面が起動し、対象フォルダパスが表示される

## 適用テスト

- [x] **新規フォルダ**（一度もカスタマイズしていない）で適用 → 対象 Explorer ウィンドウが開き直り、数秒以内に反映される
- [x] 一度適用済みのフォルダに再適用 → 対象 Explorer ウィンドウが開き直り、数秒以内に反映される
- [x] A→B→C と連続適用 → 30〜40秒待ちが発生せず、それぞれ数秒以内に反映される
- [x] 適用後の開き直しでタスクバー / スタートメニューが乱れない
- [ ] 日本語フォルダ名で適用 → 文字化けなく動作する
- [ ] 長いパス（260文字超）で適用 → 警告が表示され、続行を選択すると適用される
- [ ] PNG 画像で適用 → 正常に適用される
- [ ] JPG 画像で適用 → 正常に適用される
- [ ] WebP 画像で適用 → 正常に適用される

## 画像調整

- [ ] 縦長画像を「中央でクロップ」で適用 → フォルダ枠内に収まる
- [ ] 横長画像を「余白で全体表示」で適用 → 余白付きで全体が見える
- [ ] 拡大率スライダーを動かすとプレビューに即時反映される
- [ ] 拡大率調整した状態で適用 → 実際のアイコンにも反映される
- [ ] プレビュー上で画像をドラッグして位置調整 → 実際のアイコンにも反映される
- [ ] 「中央に戻す」ボタン → 拡大率・位置が初期値に戻る

## タグ機能

- [ ] タグなしで適用 → タグ領域がテンプレート標準色のまま
- [ ] 青タグで適用 → タグ領域が青（#0078D4）になる
- [ ] 緑タグで適用 → タグ領域が緑（#107C10）になる
- [ ] オレンジタグで適用 → タグ領域がオレンジ（#D83B01）になる
- [ ] 紫タグで適用 → タグ領域が紫（#8764B8）になる
- [ ] 赤タグで適用 → タグ領域が赤（#C42B1C）になる
- [ ] グレータグで適用 → タグ領域がグレー（#7A7574）になる

## desktop.ini 関連

- [ ] 既存 desktop.ini があるフォルダで適用 → 他のキーが保持され、IconResource のみ更新される
- [ ] 適用後の desktop.ini が UTF-16 LE BOM になっている（バイナリエディタで確認）

## 元に戻す機能

- [ ] メイン管理画面の「元に戻す」 → 確認ダイアログが表示される
- [ ] 確認後に復元 → フォルダアイコンが元に戻る（エクスプローラーで確認）
- [ ] 元々 desktop.ini がなかったフォルダを復元 → desktop.ini が削除される
- [ ] 元々 desktop.ini があったフォルダを復元 → 元の内容に戻る
- [ ] `_folderly` フォルダが削除される（旧 `.folderly` が残っている場合も合わせて掃除される）
- [ ] 復元後に管理画面の一覧から消える

## 保護機能

- [ ] C:\Windows 配下で適用 → 拒否メッセージが表示される
- [ ] C:\Program Files 配下で適用 → 拒否メッセージが表示される
- [ ] ドライブルート（C:\ 等）で適用 → 拒否メッセージが表示される
- [ ] 書き込み権限のないフォルダで適用 → 「権限がありません」が表示される
- [ ] OneDrive 配下で適用 → 警告ダイアログが表示され、キャンセルまたは続行を選べる

## 管理画面

- [ ] スタートメニューから Folderly が起動する
- [ ] 適用履歴が適用日時の降順で表示される
- [ ] 「フォルダを開く」→ エクスプローラーで対象フォルダが開く
- [ ] 履歴のサムネイルにタグ色が反映されている（48x48）

## 設定画面

- [ ] 言語切替（日本語 ⇔ English）→ UI が即時切り替わる（再起動不要）
- [ ] タグの用途例ラベルが言語切替に追従する
- [ ] 履歴の最大保持件数が変更できる
- [x] 「フォルダアイコン適用後に Explorer ウィンドウを開き直す」設定が表示される
- [ ] バージョン情報が表示される

## ライセンス（試用版）

- [ ] 試用版では「無料試用：あと N 日」が管理画面下部に表示される
- [ ] 購入後に試用バナーが非表示になる

## アンインストール

- [ ] アンインストール後に右クリックメニューから「Folderly でカスタマイズ」が消える

## 一貫性確認

- [ ] プレビューと実際のフォルダアイコンの見た目が一致する
- [ ] 異なる解像度（16/32/48/256px）のアイコンが全てフォルダ形状を持つ

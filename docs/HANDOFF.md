# Folderly Handoff

現状・残タスク・既知の問題。

## Current State

- Store package version: `1.2.0.0`（アプリ表示は `1.2.0`）
- Store identity: `KanekoApps.Folderly`
- Store candidate: `_out\Folderly_1.2.0.0_x64_store.msix`
- SHA-256: `906452A7C1A2B95E3800F76D8C03EA3475AFC0D724A16CCF6BFDB6384943F2F3`
- Free trial: 1 日（Partner Center 設定）
- Tests: `137` passed（filter: `FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied`）
- Source: `1.2.0` としてコミット済み（認定対策の実装一式＋下記の改善を含む）

## 1.2.0 で入れた変更

- タグアイコン変更がタグチップ/編集一覧に反映されない不具合を修正
- タグの色パレット(+6)・タグアイコン(+6)を追加（タグ枠は6つのまま）
- 移動・改名済みフォルダの解除を 3 択化（フォルダを指定して元に戻す／履歴のみ削除／キャンセル）。一括は失敗分を一覧に残す
- 設定の「履歴最大件数」を削除
- ヘルプボタンを GitHub Pages の FAQ(`#faq`) へ遷移、FAQ ページ(日英)を新規作成（公開済み）

## Submission Checklist（1.2.0.0）

完了済み：
- バージョンを `1.2.0.0` に更新（manifest）／アプリ表示 `1.2.0`
- Store candidate MSIX 作成・内容確認（version 1.2.0.0・WebView2Loader.dll・coreclr/hostfxr/hostpolicy/PresentationNative_cor3・e_sqlite3 同梱を確認）
- FAQ ページ(`docs/index.html` の `#faq`)を GitHub へ push 済み
- 試用版を 1 日に設定

未完了（ユーザー操作）：
- `_out\Folderly_1.2.0.0_x64_store.msix` を Partner Center にアップロード（保留中だった `1.0.17.0` を差し替え）
- 提出 → 認定

メモ：
- 試用期限切れ後の起動ブロックは Microsoft Store の自動挙動に委ねる方針（アプリ側の強制ロジックは未実装）。runFullTrust パッケージ済みデスクトップアプリでの自動ブロック挙動は実機で要確認。効かない場合は次バージョンで `IsActive` 判定による Apply 無効化＋購入プロンプトを追加検討。
- 再提出が必要になった場合は [docs/OPERATIONS.md](OPERATIONS.md) の Certification Rejection Playbook に従う。次候補は `1.3.0.0` など 4 桁目 0。

## Known Issues

- 古いドラッグ＆ドロップ履歴エントリ（SourceImagePath が空）は再 Apply するまでプレビュー復元不可。
- 旧パッケージ `Folderly.FolderlyApp 1.0.0.16`（publisher: `CN=Folderly`）がローカルに残っていると新 Store identity のテストに干渉する。テスト前に削除すること。

## Four Things That Must Stay in Sync

以下4つは常に整合している必要がある。一つを変更するときは残り3つへの影響を確認すること：

1. WebView2 エディタプレビュー（`ApplyWindow.html`）
2. WPF/offscreen 正確アイコンレンダラー（`TemplateRenderer.cs`）
3. `ApplyService` 履歴/ソース画像ストレージ
4. Explorer リフレッシュ動作（`ShellNotifier.cs`・`ApplyWindow.xaml.cs`）

# Folderly Handoff

現状・残タスク・既知の問題。

## Current State

- Store package version: `1.3.0.0`（アプリ表示は `1.3.0`）
- Store identity: `KanekoApps.Folderly`
- Store candidate: `_out\Folderly_1.3.0.0_x64_store.msix`
- SHA-256: `7E1F6A4B07402B62690F905686FA7283E478A8740879142304536A4EF7A85E7A`
- Free trial: 1 日（Partner Center 設定）
- Price: JP `¥480` / US `$2.99`（2026-07-19 に調整）
- Tests: `137` passed（filter: `FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied`）
- Source: `1.3.0` Store 提出済み（Store クラッシュ修正＋ヘルプURL更新＋Store画像/ASO更新）

## 1.3.0 で入れた変更

- Store 正常性で出ていた `CLR_EXCEPTION_80131600` クラッシュを修正（多重起動時に所有していない Mutex を解放しない）
- アプリ内ヘルプURL（`MainWindow.xaml.cs` の `FaqUrl`）を `https://folderlyapp.com/privacy/#faq` へ差し替え
- Store candidate MSIX 作成・内容確認済み（version 1.3.0.0・WebView2Loader.dll・coreclr/hostfxr/hostpolicy/PresentationNative_cor3・e_sqlite3 同梱を確認）
- Partner Center に提出済み（2026-07-19）。提出内容: `_out\Folderly_1.3.0.0_x64_store.msix`、Store スクリーンショット更新、ASO テキスト更新、価格 JP `¥480` / US `$2.99`。
- **次の作業: 認定結果を待つ。公開後 2〜4 週間、ページビュー・インストール・売上/PV・市場別/流入別データを確認する**

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
- **`_out\Folderly_1.2.0.0_x64_store.msix` を Partner Center にアップロード済み（1.0.17.0 を差し替え）・提出済み・認定待ち**

Webサイト（2026-06-01 完了）：
- ドメイン `folderlyapp.com` を取得（お名前.com）・Cloudflare 管理へ移行・Active 確認済み
- `folderly-web` リポジトリ（`github.com/sknk-aaa/folderly-web`）を作成・Cloudflare Pages に接続・公開済み
- **JP LP（`/`）と EN LP（`/en/`）を公開**（Claude Design ハンドオフ `Folderly LP.html` を忠実再現）。JPをルートに（主市場が日本のため）、ENを `/en/`。言語切替（English↔日本語）＋hreflang（ja=`/`、en=`/en/`、x-default=`/en/`）整備済み。`/privacy/` 移設済み。
- 画像：JPは `/assets/`、ENは英語UIで撮り直した版を `/assets/en/`（キャプション帯は切り取り）。
- 価格：**JP ¥480 / EN $2.99 買い切り**（2026-07-19 Store提出で調整）。
- **Partner Center の Privacy/Support URL を `https://folderlyapp.com/privacy/`（#support）に更新する更新サブミッションを提出済み**（前回認定完了後に実施・認定待ち）。
- **Google Search Console・Bing Webmaster Tools に `folderlyapp.com` を登録、sitemap 送信済み**（Bing は GSC からインポート）。
- **ブログ記事1本目を公開**：`/blog/windows-folder-icon-change/`「Windows 11でフォルダのアイコンを画像に変更する方法」。手順スクショ・作例画像・比較表・トラブル対処を含む読み物。記事内 CTA は LP（`/`）に一本化（記事→LP→ストアのファネル）。ブログ index・sitemap 登録済み。

未完了（次バージョン以降）：
- `www.folderlyapp.com` → apex への 301 リダイレクト設定（Cloudflare Redirect Rules・任意）
- how-to 記事の拡充（JP pillar 1本公開済み。残：JP★「変更できない時の対処法」・EN版）
- EN 画像の最適化（容量削減）

メモ：
- 試用期限切れ後の起動ブロックは Microsoft Store の自動挙動に委ねる方針（アプリ側の強制ロジックは未実装）。runFullTrust パッケージ済みデスクトップアプリでの自動ブロック挙動は実機で要確認。効かない場合は次バージョンで `IsActive` 判定による Apply 無効化＋購入プロンプトを追加検討。
- 再提出が必要になった場合は [docs/OPERATIONS.md](OPERATIONS.md) の Certification Rejection Playbook に従う。次候補は `1.4.0.0` など 4 桁目 0。

## Known Issues

- 古いドラッグ＆ドロップ履歴エントリ（SourceImagePath が空）は再 Apply するまでプレビュー復元不可。
- 旧パッケージ `Folderly.FolderlyApp 1.0.0.16`（publisher: `CN=Folderly`）がローカルに残っていると新 Store identity のテストに干渉する。テスト前に削除すること。

## Store Crash Triage

- 2026-07-19: Partner Center 正常性で `1.2.0.0` の過去30日クラッシュ 27 件・影響デバイス 9 台を確認。`errors/` にスクショ・WER・minidump を保存。
- 主分類 `CLR_EXCEPTION_80131600` は `System.ApplicationException HResult=0x80131600`。多重起動時に 2 個目のプロセスが所有していない Mutex を `OnExit` で `ReleaseMutex()` していた経路と一致。
- 修正コミット: `f7c5d2b 多重起動時のMutex解放クラッシュを修正`。`App.xaml.cs` で Mutex 所有フラグを保持し、所有時のみ解放する。`1.3.0.0` Store 候補に含めた。

## Four Things That Must Stay in Sync

以下4つは常に整合している必要がある。一つを変更するときは残り3つへの影響を確認すること：

1. WebView2 エディタプレビュー（`ApplyWindow.html`）
2. WPF/offscreen 正確アイコンレンダラー（`TemplateRenderer.cs`）
3. `ApplyService` 履歴/ソース画像ストレージ
4. Explorer リフレッシュ動作（`ShellNotifier.cs`・`ApplyWindow.xaml.cs`）

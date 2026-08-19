# Folderly Handoff

## 2026-08-19 Handoff

### 今日完了したこと

- `folderly-win`
  - v1.6.1.0 のStore候補MSIX作成準備。
  - ヘルプと設定を別ウィンドウではなくメイン画面タブ内表示に整理。
  - ヘルプタブに「サポートに連絡」「よくある質問」の行カードと「Folderlyについて」を配置。
  - 設定タブ下部に控えめなStore評価導線を配置。
  - ローカル確認用 `1.4.2.0` / `1.4.3.0` をインストールして見た目確認。
  - 提出用のソースバージョンを `1.6.1.0`（アプリ表示 `1.6.1`）に更新。
  - 言語切り替えが設定変更直後に反映されるように修正。
  - 問い合わせフォームへ選択中言語を渡すように修正。
  - 青いボタン/クリック対象の配色とhover状態を調整。

### 提出時に見るもの

1. Partner Center にアップロードするMSIX:
   - `_out\Folderly_1.6.1.0_x64_store.msix`
2. 提出後の公開確認:
   - `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=ja-JP&gl=JP`
   - `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=en-US&gl=US`

## 2026-07-30 Handoff

### 今日完了したこと

- `folderly-win`
  - v1.4.0.0 のStore候補MSIXを作成。
  - 新アイコン `icons/icon-v1.4.png` を採用し、アプリ/LPで使うアイコン素材を更新。
  - 日本語のStore 1枚目画像を、ベネフィットが伝わる方向へ改善。
  - manifest/リソース設定を見直し、Store対応言語に日本語が出るように改善。
  - Microsoft Storeの商品ページへ直接飛ぶ導線を追加/改善。
  - レビュー依頼導線を追加/改善。
  - Store更新申請済み。
  - GitHub push済み。最新コミット: `b1011f9 アイコン素材をv1.4版に更新`。
- `folderly-web`
  - 日本語LPのH1を「フォルダアイコンを、好きな画像に変更。」へ変更。
  - 訴求を「フォルダを写真と色タグで見分けやすく」に統一。
  - LPに「Windows標準 vs Folderly」比較を追加。
  - 日本語/英語LPのFAQ、CTA、meta、schemaを改善。
  - `/blog/windows-folder-icon-change/` を本命SEO記事として更新。
  - `/blog/folder-icon-not-changing/` に症状別CTAを追加。
  - `/blog/folderly-how-to/` を新規作成。
  - sitemapとブログ一覧を更新。
  - GitHub push済み。最新コミット: `2e0f605 LPとSEO記事導線を改善`。

### 明日まず見るもの

1. `https://folderlyapp.com/`
   - 日本語LP。ファーストビュー、比較ブロック、FAQ、CTAの流れを見る。
2. `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=ja-JP&gl=JP`
   - 日本語Store。申請反映後、アイコン、1枚目画像、対応言語、価格、説明文を見る。
3. `https://folderlyapp.com/blog/windows-folder-icon-change/`
   - 本命SEO記事。検索意図に合っているか、LPへの導線が自然かを見る。
4. `https://folderlyapp.com/blog/folderly-how-to/`
   - 新規使い方記事。購入前の不安解消になっているかを見る。
5. `https://folderlyapp.com/en/`
   - 英語LP。海外向け訴求が自然かを見る。
6. `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=en-US&gl=US`
   - 英語Store。英語の画像/説明が弱くないかを見る。

### 残タスク

- Store審査通過後、日英StoreページをChromeで目視確認する。
- Partner Centerのレビュー表示問題について、Microsoftサポート回答を確認する。
- GSCで次のURLのインデックス登録をリクエストする。
  - `https://folderlyapp.com/`
  - `https://folderlyapp.com/blog/windows-folder-icon-change/`
  - `https://folderlyapp.com/blog/folder-icon-not-changing/`
  - `https://folderlyapp.com/blog/folderly-how-to/`
- Cloudflare Pagesのデプロイが走っていない場合は手動で再デプロイする。
- note記事、Product Hunt、Reddit、AlternativeToなど外部露出の準備を進める。
- 直販は現時点では保留。まずはStoreでレビューとCVRを作る。

### 未コミット/未追跡で残っているもの

- `folderly-win`
  - `_out/` 配下のMSIX/展開/検証成果物。
  - `0730_GSC/` のGSCデータ。
  - `.claude/` や `errors/` などのローカル作業物。
- `folderly-web`
  - `assets/raw/step1.png`
  - `assets/raw/step2.png`

これらは今回のpushには含めていない。必要なら別途整理する。

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

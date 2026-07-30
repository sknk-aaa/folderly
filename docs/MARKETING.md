# Folderly Marketing

## 2026-07-30 Update: LP/SEO/Store Growth Plan

### 今日の結論

- 日本語流入が現状の主戦場。GSCではクリックの大半が日本から来ているため、まず日本語LPと日本語SEO記事を強くする。
- 訴求の中心は「フォルダを写真と色タグで見分けやすく」。
- LPのH1は「フォルダアイコンを、好きな画像に変更。」へ寄せた。
- 「フォルダアイコン 変更できない」より、「フォルダアイコン 変更方法」「フォルダアイコン 好きな画像」「Windows 11 フォルダアイコン 変更」系の検索意図を優先する。
- Store単体の露出だけでは弱いので、LP/SEO記事で検索流入を作り、Microsoft Storeへ送客する。

### 2026-07-30 に完了した施策

- `folderly-win`
  - v1.4.0.0 の次版MSIXを作成。
  - Store用アイコン素材を `icons/icon-v1.4.png` 系に更新。
  - 日本語Store画像の1枚目を改善。
  - 日本語対応がStore上で認識されるように、manifest/リソース設定を改善。
  - アプリ内のStore導線、購入導線、レビュー導線を改善。
  - Microsoft Store更新申請済み。
- `folderly-web`
  - LPのH1/メタ/FAQ/CTAを「好きな画像に変更」「写真と色タグ」に寄せて改善。
  - 日本語LPに「Windows標準 vs Folderly」の比較ブロックを追加。
  - 英語LPも同じ軸で改善。
  - `/blog/windows-folder-icon-change/` を本命SEO記事として更新。
  - `/blog/folder-icon-not-changing/` に症状別CTAを追加。
  - `/blog/folderly-how-to/` を新規作成。
  - `blog/index.html` と `sitemap.xml` を更新。
  - `folderly-web` は `2e0f605 LPとSEO記事導線を改善` まで push 済み。

### まず見るURL

| 優先 | URL | 見る理由 |
|---|---|---|
| 1 | `https://folderlyapp.com/` | 日本語LPの最重要ページ。H1、比較ブロック、FAQ、CTAが購入につながる流れか確認する。 |
| 2 | `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=ja-JP&gl=JP` | 日本語Storeページ。申請反映後、アイコン、1枚目画像、対応言語、価格、説明文がLPとズレていないか確認する。 |
| 3 | `https://folderlyapp.com/blog/windows-folder-icon-change/` | 本命SEO記事。「変更方法」の検索意図に答えつつ、Folderly導線が自然か確認する。 |
| 4 | `https://folderlyapp.com/blog/folderly-how-to/` | 新規の使い方記事。Store/LPから来たユーザーの不安解消ページとして機能するか確認する。 |
| 5 | `https://folderlyapp.com/en/` | 英語LP。海外展開用に「photos + color tags」「no .ico conversion」が自然に伝わるか確認する。 |
| 6 | `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=en-US&gl=US` | 英語Storeページ。海外向けの訴求と画像品質を確認する。 |
| 7 | `https://folderlyapp.com/blog/folder-icon-not-changing/` | 困っているユーザー向け記事。症状別CTAが自然か確認する。 |
| 8 | `https://folderlyapp.com/blog/` | 新規記事と更新記事が一覧に出ているか確認する。 |

### 次にやるべきこと

1. Store申請の審査通過後、日本語/英語のStore表示をChromeで目視確認する。
2. Search Consoleで更新済みURLと新規URLのインデックス登録をリクエストする。
3. GSCで2-4週間後に、表示回数が増えたクエリへ記事タイトル/本文を寄せ直す。
4. noteに日本語記事を投稿し、`/blog/windows-folder-icon-change/` かLPへ送客する。
5. Product Hunt/Reddit/AlternativeToなど海外露出を準備する。ただし、海外向けは英語Store画像と英語LPの見え方確認後に行う。
6. 直販は保留。まずはStore販売でCVRとレビューを作り、LPの勝ち筋が見えてから検討する。

### レビュー表示の注意

- Partner Centerでは1件の評価/レビューが見えているが、公開Storeでは0件表示になることがある。
- 想定原因は、公開反映の遅延、国/地域別表示、レビュー本文のモデレーション、評価とレビューの集計仕様、Store側キャッシュ。
- Microsoftへサポートリクエスト送信済み。回答待ち。
- 公開Storeでレビュー0件が続く場合、レビュー獲得導線をさらに強化する。初期はレビュー件数そのものがCVRにかなり効く。

マーケティングの正（チャネル方針・ASO・キーワード調査・競合・SEO/LP設計）。施策で得た知見はここに集約する。
ストア掲載の確定コピー（製品名/短い説明/キーワード/説明）は重複を避け [docs/OPERATIONS.md](OPERATIONS.md) の「Store Listing Text」に置く。

## 現状とボトルネック

- Microsoft Store 公開済み（日英）。Store URL: `https://apps.microsoft.com/detail/9N99JH5H91H8`
- 初期アナリティクス（〜2026-05 / 過去1か月）：ページビュー 19・インストール 4・コンバージョン 21%・成功率 100%・地域 US 2 / Poland 2（日本0）。
- **流入はStoreのWindows内検索 100%・外部流入ゼロ。** 最大の課題は「機能/見た目」ではなく **発見されていないこと**。
- コンバージョン21%＝リスティングは悪くない → ボトルネックは露出。

## チャネル方針

- **本命：SEOランディングページ → Microsoft Store へ送客**（外部流入を作る）。
- **直接販売（Store外）は保留**。¥480 単価に対し、コード署名・決済手数料・各国税・ライセンス基盤・サポートの固定費が見合わない。将来やるなら Merchant of Record（Paddle / Lemon Squeezy 等）。LPは「あとで Buy direct を足せる」余地だけ残す。
- **公開基盤**：独自ドメイン **`folderlyapp.com`**（2026-06-01 決定。`folderly.com`＝メール到達性SaaS、`folderly.app`＝他社取得済みのため回避）＋ **Cloudflare Pages**（原価ドメイン・無料プライバシー配慮アナリティクス・エッジ拡張）。**言語別URL＋hreflang** で日英を別ページ最適化。canonical は apex（`folderlyapp.com`）、`www` は apex へリダイレクト。
  - 移行済み：Store のプライバシー/サポートURLとアプリ内ヘルプ(`#faq`)は `folderlyapp.com/privacy/` に統一。旧 `sknk-aaa.github.io/folderly/` は必要に応じて生かす。
- 補助チャネル（コツコツ型）：Reddit（r/Windows11 等・Before/After投稿）、YouTube Shorts（how-to）、日本語記事（note/Qiita）。
- 現実認識：この領域は**ニッチ**。SEOは「細く長く」積む施策で、短期の量は期待しない。

## ASO（Microsoft Store）

- 確定コピーは [docs/OPERATIONS.md](OPERATIONS.md) 参照。方針のみ記載：
  - タイトルに主軸キーワード：英 `Folder Icon Changer` / 日 `フォルダアイコンを画像に変更`（「サムネイル」は需要ゼロのため廃止）。
  - キーワードは下記調査で検証した語に差し替え（素材語/Mac語は外す）。
  - 訴求軸：**「自分の画像が使える（既製アイコンと違う）／.ico変換不要・面倒な設定なし／買い切り・サブスク無」**。
- 試用版：**1日**（7日だとずるずる使われるため短縮）。試用期限切れ後の起動ブロックは Microsoft Store の自動挙動に委ねる（runFullTrust パッケージでの自動ブロックは実機で要確認、効かなければ次版で `IsActive` 判定によるApply無効化＋購入プロンプトを追加）。

### 2026-07-19 Store リローンチ判断

- 1.3.0.0 は Store クラッシュ修正を最優先で提出。あわせて Store スクリーンショット、ASO テキスト、価格を更新して提出済み。
- 直近3か月の Partner Center 取得データは、ページビュー 532・インストール 25・コンバージョン 4.7%・インストール成功率 100%。直近でダウンロードが止まっているため、課題は「価格だけ」ではなく、露出不足と Store ページ訴求の弱さが混在している可能性が高い。
- 画像以外の ASO 要素: 製品名、短いタイトル、短い説明、長い説明、Product features、検索語句、カテゴリ、言語別ローカライズ、価格/試用、評価・レビュー、最新情報、外部流入/キャンペーン計測。
- 製品名は英 `Folderly - Folder Icon Changer` を維持。主検索語 `folder icon changer` を自然に含み、過剰なキーワード詰め込みにならないため。
- 英語の短い説明と長い説明は、冒頭で `change Windows folder icons`、`own images`、`no .ico conversion`、`color tags` を明示する方針。機能名より先に「何の問題を解くか」を見せる。
- 検索語句は最大7枠のため、素材探し・Mac意図・広すぎる語を避け、購入/導入意図に近い語へ寄せる。英: `change folder icon` / `folder icon changer` / `custom folder icon` / `folder color tag` / `folder image` / `folder organizer` / `desktop organization`。日: `フォルダ アイコン 変更` / `フォルダ アイコン 画像` / `Windows11 フォルダ アイコン` / `フォルダ 色分け` / `アイコン カスタマイズ` / `デスクトップ 整理` / `ico 変換`。
- 価格は JP `¥480` / US `$2.99` に調整。恒久的な大幅値下げはしない。500円→300円級の値下げは販売数が約1.67倍以上にならないと売上増にならず、現在の流入量では根拠が足りないため。
- 以後の判断指標はインストール数単体ではなく **売上/PV**。価格・画像・ASO更新後、2〜4週間または最低数百PVを見て、市場別・流入別・CVR別に判断する。

## キーワード調査（2026-05-31）

**方法**：多源オートコンプリート（Google / Bing / YouTube Suggest）＋ Keyword Surfer 推定（**国別**ボリューム）。Google/Microsoft の正式 Keyword Planner 実数は未取得（無料アカウントの壁・後日）。

**読み方の注意**：Keyword Surfer の数値は ①**国別（世界合計でない）** ②**控えめな推定** ③**言い回しごとに分割**（同概念が多表記に割れる）。1行＝市場全体ではなく過小に見える。逆に全変種の単純合計は過大（重複）。真値はその中間。

### 意図クラスタと優先度

**日本語（🇯🇵 Japan・Surfer推定）**
| 語群 | 推定Vol | 意図 | 適合 | 優先 |
|---|---|---|---|---|
| フォルダ(ー)アイコン 変更（多表記） | ~1000/表記 | フォルダアイコン変更 | 高 | **主軸** |
| フォルダーのアイコン | 4400 | 一般/素材寄り | 中 | 補助 |
| windows フォルダ アイコン | 480 | 〃Windows | 高 | LP |
| フォルダ 画像 アイコン / フォルダ画像 | 390〜590 | 画像をアイコンに | 中〜高 | LP |
| フォルダ アイコン 可愛い | 低〜中 | 見た目重視 | 高 | 記事/補助 |
| デスクトップ アイコン 変更（+好きな画像） | 1300〜1600 | デスクトップ(別物) | 中(部分) | **記事で送客** |
| フォルダ カスタマイズ | ~0 | — | — | 捨て |

**英語（🇺🇸 US・Surfer推定）**
| 語群 | 推定Vol | 意図 | 適合 | 優先 |
|---|---|---|---|---|
| change folder icon windows 11 / how to ~ windows | 170〜260 | 変更/how-to Win | 高 | **主軸** |
| custom folder icons (windows 11) / custom folder icon | 140〜320 | アプリ意図 | 高 | LP |
| folder icon changer | （Bingで強） | アプリ直名 | 高 | LP |
| how to change folder icons / icon | 390〜480 | how-to | 高 | 記事 |
| folder color (windows 11) | 210〜1600 | 色 | 部分 | 補助 |
| folder icon(s) / png / free | 880〜8100 | 素材探し | 低 | 記事のみ送客 |
| how to change folder icon **mac** | 2400 | Mac(対象外) | なし | 捨て |

### サイズ感の結論
- 実アドレス可能（Windows×アプリ意図）は**国別で数百〜数千/月のニッチ**。Mac・素材探しを除くと英語はさらに薄い。
- **日本が相対的に最良**（フォルダ語が1000規模＋競合が薄い）。→ **日本語ページを主戦場、英語はおまけ**。
- 拡散した how-to 需要は **チュートリアル記事**で束ねて送客するのが効率的。

## 競合

| 競合 | 価格 | 特徴 | 対 Folderly |
|---|---|---|---|
| FolderIco（Store外・老舗） | $29.99 | ワンクリック色付け中心、**写真カバー非対応** | Folderlyは自分の画像OK・¥480 |
| Folder Marker | 有料 | ワンクリック、優先度色分け | 同上 |
| Folder Icon Changer PRO（MS Store / MakeTone） | ¥980 | 画像→アイコン D&D・**レビュー0** | UI/価格で優位、レビュー薄く割って入れる |
| Folder Icon Change / Custom Folder Icon Changer / FolderIconStudio 等 | 無料〜 | 既製アイコン選択型も混在 | 「自分の画像が使える」で差別化 |

- 市場は同名アプリが密集するが**レビュー定着が薄い**（PRO=0件）。**レビューを数件取るだけで信頼度上位に並べる**。
- Folderly差別化：**自分の画像／¥480買い切り／色タグ・タグ名／プレビュー調整／いつでも元に戻す**。
- 英語側は "free / png / download / crack" 意図が濃い＝価格で勝てない層は捨て、「ラクして綺麗に」で勝つ。

## サイト / LP 設計

URL構成（言語別＋hreflang）：
```
/            JP LP（x-default） title「フォルダアイコンを画像に変更 | Folderly（Windows 10/11）」
                      狙い: フォルダアイコン変更 / フォルダ アイコン 画像 / windows フォルダ アイコン
/en/         EN LP  title「Folder Icon Changer for Windows 10 & 11 | Folderly」
                      狙い: folder icon changer / change folder icon windows 11 / custom folder icons
/blog/...    記事(日英) how-to で拡散需要を回収 → LP へ送客（記事内CTAはLP一本化）
   JP: 「Windows 11でフォルダのアイコンを画像に変更する方法」「フォルダアイコンが変更できない時の対処法」
   EN: 「How to change folder icons on Windows 11」「Custom folder icons not working (fix)」
```
（主市場が日本のため JP をルート `/`、EN を `/en/` に配置。x-default=`/en/`、ja=`/`、en=`/en/`）

ファネル：**記事（how-to・拡散需要回収）→ LP（製品訴求）→ Microsoft Store（購入）**。

**記事内の CTA は LP（`/`）一本化**（ストア直リンクは置かない）。理由：①ストアに送ると以後の計測・改善が一切できないが、LP は計測・AB・再訪導線を自前で持てる ②記事読者は情報収集段階で、購入説得（Before/After・無料試用・価格）は作り込んだ LP に集約した方が転換率が高い ③LP 自体に強いストア CTA が複数あるため遠回りにならない。将来 LP がボトルネックと判明したら部分的にストア直ショートカットを検討（最適化フェーズの判断）。

LP の10セクション骨格：Hero(Before/After＋CTA) → 信頼バー → 課題→解決 → Before/Afterショーケース → 使い方3ステップ → 機能グリッド → 競合比較 → 価格/試用 → FAQ → 最終CTA/フッター。

デザイン方針（モックはChatGPTに生成させる前提）：クリーン/Windows 11 Fluent寄り、アクセント `#0f63c6`＋差し色フォルダ黄 `#FFC72C`、背景 `#f7f8fb`、主役は実物Before/Afterフォルダ（捏造UIは使わない）。

SEO初期設定：title≤60字・description≈120字・H1単一・`hreflang`(en/ja/x-default)・OGP・`sitemap.xml`・`robots.txt`・Search Console/Bing登録。

## SEO戦略（2026-06-01 調査・実装）

### ランキング要因の優先度（調査結論）
1. **コンテンツ × 検索意図の一致**（最重要）。薄い1枚LPは競合語で上がりにくい。
2. **被リンク（権威）**。新規ドメイン＝権威ゼロが最大ハンデ。
3. **Core Web Vitals**（画像がLCPの85%/76%を占める＝画像最適化が単一で最も効く）。
4. title先頭キーワード／見出し階層／構造化データ／内部リンク／コンテンツ鮮度。
- 新規ドメインは効果が出るまで概ね **3〜6か月**。on-page は必要条件だが十分条件ではない（順位を動かすのは②コンテンツ＋③被リンク）。

### SERP実態（target語を実見）
- 「change folder icon windows 11」「フォルダ アイコン 変更」の上位は **Microsoft Support・フォーラム・Q&A・how-to記事** が大半。製品LPはごく一部。
- **悩み系クエリ**（"Custom ICO not working" / "Cannot change folder icon" / フォルダアイコン 変更できない）が上位＝高需要、かつ **Folderly が直接の解決策**。→ 記事の最優先ターゲット＆高CV。

### 実装済み（on-page / 技術）
- title をキーワード先頭に（JP「フォルダアイコンを画像に変更 | Folderly（Windows 10/11）」／EN「Folder Icon Changer for Windows 10 & 11 | Folderly」）。
- description（キーワード入り）・canonical・hreflang(ja/en/x-default)・OGP。
- **`SoftwareApplication` 構造化データ**（OS・価格 JPY¥480/USD$2.99・DLリンク）。
- **Core Web Vitals**：全画像を WebP 化＋最大1300px(ロゴ160px)にリサイズ（合計 ~11MB→~0.5MB、hero 937→57KB）。hero に `preload`＋`fetchpriority=high`、全 `<img>` に width/height（CLS防止）、ヒーロー以外は `loading=lazy`。
- sitemap.xml / robots.txt、Google Search Console・Bing Webmaster Tools 登録・sitemap送信済み。

### コンテンツ計画（本命の集客レバー＝`/blog/`）
SERPが情報系なので、how-to/悩み解決記事で拡散需要を回収→LP/Storeへ送客：
- JP：「Windows 11でフォルダのアイコンを画像に変更する方法」「フォルダアイコンが変更できない時の対処法」「フォルダアイコン 可愛い 画像の作り方」
- EN：「How to change folder icons on Windows 11」「Custom folder icons not working on Windows 11（fix）」「How to put a picture on a folder」
- 記事内の CTA・内部リンクは LP（`/`）へ統一（ストア送客は LP が担当）。比較/代替ページ（"FolderIco alternative" 等）も有効。

### 権威・被リンク（新規ドメイン対策）
- Product Hunt ローンチ、ソフト系ディレクトリ（AlternativeTo / Softpedia / Softonic）登録、Reddit（r/Windows11・作品見せ）。
- リンクされやすい資産：無料フォルダアイコン素材パック、決定版 how-to ガイド。

### 計測して回す
- GSC/Bing で「表示回数の多い語」を数週間後に確認 → title・本文・記事を実データで改善（“当て”でなく観測→改善）。

### 注意
- FAQ構造化データは2023年以降リッチリザルトが政府/医療系に限定 → 付けても星/FAQ表示は基本出ない（優先度低）。
- og:image / JSON-LD image は社会的互換のため PNG 維持（ページ表示は WebP）。

## 残タスク

- [ ] キーワード実数取得（Keyword Surfer 継続 ＋ 余力で Microsoft 広告/Google プランナー）
- [x] `folderlyapp.com` 公開（Cloudflare Pages、リポ `folderly-web`）。**JP LP をルート `/`・EN LP を `/en/` に公開済み**（Claude Design 忠実再現）。privacy/FAQ は `/privacy/`。言語切替（English↔日本語）＋hreflang（ja=`/`、en=`/en/`、x-default=`/en/`）整備済み。
  - 価格：JP ¥480 / EN $2.99（US Store価格）。EN画像はキャプション帯を切り取り、英語UIで撮り直した版を `/assets/en/`。
- [x] LP（日英）制作・公開済み（Claude Design 忠実再現）。
- [x] SEO/CWV基礎：title最適化・SoftwareApplication構造化データ・全画像WebP化＋preload/width-height（~11MB→~0.5MB）・GSC/Bing登録。
- [~] **how-to 記事を `/blog/` に制作**（SEOの本命）。JP pillar「Windows 11でフォルダのアイコンを画像に変更する方法」公開済み（`/blog/windows-folder-icon-change/`・手順スクショ＋作例＋比較・CTAはLP一本）。残：JP★「変更できない時の対処法」・EN版。
- [ ] 被リンク：Product Hunt / AlternativeTo・Softpedia / Reddit など（新規ドメインの権威づけ）
- [ ] スクリーンショット/動画の作り込み（Before/Afterヒーロー＋短尺デモ）
- [ ] レビュー獲得策（初期数件で競合に並ぶ）
- [ ] 公開後：Reddit / YouTube Shorts / note でコツコツ発信

# Folderly Marketing

マーケティングの正（チャネル方針・ASO・キーワード調査・競合・SEO/LP設計）。施策で得た知見はここに集約する。
ストア掲載の確定コピー（製品名/短い説明/キーワード/説明）は重複を避け [docs/OPERATIONS.md](OPERATIONS.md) の「Store Listing Text」に置く。

## 現状とボトルネック

- Microsoft Store 公開済み（日英）。Store URL: `https://apps.microsoft.com/detail/9N99JH5H91H8`
- 初期アナリティクス（〜2026-05 / 過去1か月）：ページビュー 19・インストール 4・コンバージョン 21%・成功率 100%・地域 US 2 / Poland 2（日本0）。
- **流入はStoreのWindows内検索 100%・外部流入ゼロ。** 最大の課題は「機能/見た目」ではなく **発見されていないこと**。
- コンバージョン21%＝リスティングは悪くない → ボトルネックは露出。

## チャネル方針

- **本命：SEOランディングページ → Microsoft Store へ送客**（外部流入を作る）。
- **直接販売（Store外）は保留**。¥500 単価に対し、コード署名・決済手数料・各国税・ライセンス基盤・サポートの固定費が見合わない。将来やるなら Merchant of Record（Paddle / Lemon Squeezy 等）。LPは「あとで Buy direct を足せる」余地だけ残す。
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
| FolderIco（Store外・老舗） | $29.99 | ワンクリック色付け中心、**写真カバー非対応** | Folderlyは自分の画像OK・¥500 |
| Folder Marker | 有料 | ワンクリック、優先度色分け | 同上 |
| Folder Icon Changer PRO（MS Store / MakeTone） | ¥980 | 画像→アイコン D&D・**レビュー0** | UI/価格で優位、レビュー薄く割って入れる |
| Folder Icon Change / Custom Folder Icon Changer / FolderIconStudio 等 | 無料〜 | 既製アイコン選択型も混在 | 「自分の画像が使える」で差別化 |

- 市場は同名アプリが密集するが**レビュー定着が薄い**（PRO=0件）。**レビューを数件取るだけで信頼度上位に並べる**。
- Folderly差別化：**自分の画像／¥500買い切り／色タグ・タグ名／プレビュー調整／いつでも元に戻す**。
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
- **`SoftwareApplication` 構造化データ**（OS・価格 JPY¥500/USD$3.99・DLリンク）。
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
  - 価格：JP ¥500 / EN $3.99（US Store価格）。EN画像はキャプション帯を切り取り、英語UIで撮り直した版を `/assets/en/`。
- [x] LP（日英）制作・公開済み（Claude Design 忠実再現）。
- [x] SEO/CWV基礎：title最適化・SoftwareApplication構造化データ・全画像WebP化＋preload/width-height（~11MB→~0.5MB）・GSC/Bing登録。
- [~] **how-to 記事を `/blog/` に制作**（SEOの本命）。JP pillar「Windows 11でフォルダのアイコンを画像に変更する方法」公開済み（`/blog/windows-folder-icon-change/`・手順スクショ＋作例＋比較・CTAはLP一本）。残：JP★「変更できない時の対処法」・EN版。
- [ ] 被リンク：Product Hunt / AlternativeTo・Softpedia / Reddit など（新規ドメインの権威づけ）
- [ ] スクリーンショット/動画の作り込み（Before/Afterヒーロー＋短尺デモ）
- [ ] レビュー獲得策（初期数件で競合に並ぶ）
- [ ] 公開後：Reddit / YouTube Shorts / note でコツコツ発信

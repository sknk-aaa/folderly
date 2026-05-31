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
- **直接販売（Store外）は保留**。¥300 単価に対し、コード署名・決済手数料・各国税・ライセンス基盤・サポートの固定費が見合わない。将来やるなら Merchant of Record（Paddle / Lemon Squeezy 等）。LPは「あとで Buy direct を足せる」余地だけ残す。
- **公開基盤**：独自ドメイン取得＋ **Cloudflare Pages**（原価ドメイン・無料プライバシー配慮アナリティクス・エッジ拡張）。**言語別URL＋hreflang** で日英を別ページ最適化。
  - 移行注意：現 `sknk-aaa.github.io/folderly/` は Store のプライバシー/サポートURL＋アプリ内ヘルプ(`#faq`)から参照中。新ドメイン移行時は Store URL 更新（再提出不要）＋旧URLはリダイレクトで生かす。公開コンテンツの正本は1ドメインに統一（重複コンテンツ回避）。
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
| FolderIco（Store外・老舗） | $29.99 | ワンクリック色付け中心、**写真カバー非対応** | Folderlyは自分の画像OK・¥300 |
| Folder Marker | 有料 | ワンクリック、優先度色分け | 同上 |
| Folder Icon Changer PRO（MS Store / MakeTone） | ¥980 | 画像→アイコン D&D・**レビュー0** | UI/価格で優位、レビュー薄く割って入れる |
| Folder Icon Change / Custom Folder Icon Changer / FolderIconStudio 等 | 無料〜 | 既製アイコン選択型も混在 | 「自分の画像が使える」で差別化 |

- 市場は同名アプリが密集するが**レビュー定着が薄い**（PRO=0件）。**レビューを数件取るだけで信頼度上位に並べる**。
- Folderly差別化：**自分の画像／¥300買い切り／色タグ・タグ名／プレビュー調整／いつでも元に戻す**。
- 英語側は "free / png / download / crack" 意図が濃い＝価格で勝てない層は捨て、「ラクして綺麗に」で勝つ。

## サイト / LP 設計

URL構成（言語別＋hreflang）：
```
/            EN LP   "Folder Icon Changer for Windows 10 & 11"
                      狙い: change folder icon windows 11 / custom folder icons / folder icon changer
/ja/         JP LP   "フォルダアイコンを画像に変更"
                      狙い: フォルダアイコン変更 / windows フォルダ アイコン / フォルダ画像 アイコン
/blog/...    記事(日英) how-to で拡散需要を回収 → LP/Store へ送客
   JP: 「Windows 11でフォルダのアイコンを“好きな画像”に変える方法」
   EN: 「How to change folder icons on Windows 11 (to any image)」
```

ファネル：**記事（how-to・拡散需要回収）→ LP（製品訴求）→ Microsoft Store（購入）**。

LP の10セクション骨格：Hero(Before/After＋CTA) → 信頼バー → 課題→解決 → Before/Afterショーケース → 使い方3ステップ → 機能グリッド → 競合比較 → 価格/試用 → FAQ → 最終CTA/フッター。

デザイン方針（モックはChatGPTに生成させる前提）：クリーン/Windows 11 Fluent寄り、アクセント `#0f63c6`＋差し色フォルダ黄 `#FFC72C`、背景 `#f7f8fb`、主役は実物Before/Afterフォルダ（捏造UIは使わない）。

SEO初期設定：title≤60字・description≈120字・H1単一・`hreflang`(en/ja/x-default)・OGP・`sitemap.xml`・`robots.txt`・Search Console/Bing登録。

## 残タスク

- [ ] キーワード実数取得（Keyword Surfer 継続 ＋ 余力で Microsoft 広告/Google プランナー）
- [ ] 独自ドメイン取得 → Cloudflare Pages 公開（言語別URL＋hreflang、旧github.ioリダイレクト）
- [ ] LP（日英）＋ how-to 記事（日英）制作。モックは ChatGPT、コピーは本doc/OPERATIONS準拠
- [ ] スクリーンショット/動画の作り込み（Before/Afterヒーロー＋短尺デモ）
- [ ] レビュー獲得策（初期数件で競合に並ぶ）
- [ ] 公開後：Reddit / YouTube Shorts / note でコツコツ発信

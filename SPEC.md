# Folderly v1.0 — Specification

> 本ドキュメントは Folderly v1.0 の実装仕様書です。Claude Code への指示の根拠となるドキュメントであり、本ドキュメントに記載のない機能は実装しないでください。

---

## 1. Overview

### 1.1 製品概要
Folderly は、Windows のフォルダの見た目を「表紙画像 + 色タグ」で変えられる軽量ツールです。フォルダを右クリックして画像と色タグを選ぶだけで、そのフォルダのアイコンが「内容を示す画像 + 種類を示すタグ」の組み合わせで表示されるようになります。

### 1.2 v1.0 の価値提案

> **「フォルダを右クリックして、表紙画像と色タグで見分けやすくする軽量ツール」**

重要な価値:
- **右クリックから使える**: コア体験は右クリックメニュー経由
- **画像でフォルダの内容が分かる**: フォルダ前面に表紙画像を表示
- **左上の色タグで種類が分かる**: 開発/大学/動画/デザイン/重要/保管 を色で識別
- **フォルダらしい見た目を保つ**: 単なる画像置き換えではなく、フォルダ形状を維持
- **Windows 標準の仕組みで安全に適用**: desktop.ini 方式、いつでも復元可能
- **Microsoft Store で販売できる品質**: 署名・自動更新・試用版すべてStore準拠

### 1.3 課題と提供価値
- **課題**: Windows のフォルダは標準では同じ見た目で、内容も種類も文字でしか判別できない
- **価値**: 画像で「中身」を、色タグで「種類」を、視覚的に二段階で識別できる

### 1.4 ターゲットユーザー
- 複数プロジェクトを管理する開発者
- 写真・動画素材を整理するクリエイター
- デザイン作品をフォルダで分類するデザイナー
- デスクトップを整えたい一般ユーザー

### 1.5 配信形態
- Microsoft Store 経由（MSIX パッケージ）
- 価格: ¥1,800 / $14.99 買い切り
- 試用: Store の Time-limited Trial 機能で 7 日間フル機能試用

### 1.6 ブランド
- 製品名: **Folderly**
- カラー: Primary `#0078D4` / Accent `#FFB900` / Surface `#FAFAFA`
- フォント: Segoe UI Variable
- トーン: 丁寧、Windows 標準感、要所で楽しさ

---

## 2. Tech Stack

実装に使う技術は以下に固定します。代替案を採用する場合は事前に確認してください。

| 項目 | 採用技術 | バージョン |
|---|---|---|
| 言語 | C# | 12 |
| ランタイム | .NET | 8 (LTS) |
| UI フレームワーク | WPF | .NET 8 同梱 |
| 画像処理 | SixLabors.ImageSharp | 3.x |
| データベース | Microsoft.Data.Sqlite | 8.x |
| Store API | Windows.Services.Store | (Windows SDK) |
| Win32 API 呼び出し | P/Invoke | 標準 |
| パッケージング | MSIX (Windows Application Packaging Project) | VS2022 内蔵 |
| ターゲット OS | Windows 10 1809 (build 17763) 以上 | x64 / ARM64 |
| IDE | Visual Studio 2022 | 17.10 以上 |
| テストフレームワーク | xUnit | 2.x |
| ロガー | Microsoft.Extensions.Logging | 8.x |

### 2.1 採用しないもの
- WinUI 3（不安定、Storeパッケージング複雑化のため見送り）
- Avalonia（ターゲットがWindows専用のため不要）
- Newtonsoft.Json（System.Text.Json で十分）
- Entity Framework（SQLiteへの簡易アクセスなので過剰）

---

## 3. Features

### 3.1 In Scope（v1.0 で実装する機能）

#### F-01: 右クリックメニュー登録
- フォルダを右クリックすると「Folderlyで表紙を変更」が常時表示される
- メニューアイコンに Folderly のロゴを表示
- 選択するとFolderlyの画像選択画面が起動し、対象フォルダのパスが渡される
- MSIX マニフェストの `desktop:FileExplorerContextMenus` で宣言する
- インストール中は常時有効。有効/無効の設定は提供しない（コア体験を保護するため）
- アンインストール時には MSIX の標準動作により確実に削除される

#### F-02: 画像選択画面（フォルダ型プレビュー付き）
- 対象フォルダのパスを表示
- 画像を選択するUI（ファイルダイアログ + ドラッグ&ドロップ対応）
- **Windows風フォルダテンプレート上で実際の見た目をプレビュー表示**
  - 左上のタブ部分にタグ色を表示
  - フォルダ前面の画像表示領域に選択画像を表示
  - 適用後のアイコンに近い見た目を確認できる
- 対応画像形式: PNG, JPG, JPEG, BMP, WebP
- 画像調整機能（F-04 参照）
- 色タグ選択機能（F-05 参照）
- 「適用」「キャンセル」ボタン

#### F-03: フォルダ表紙テンプレート合成
- Windows 風フォルダ形状テンプレートを内部に持ち、これに画像とタグを合成する
- テンプレートの構成領域:
  - **タグ領域**: フォルダ左上の出っ張り部分（タブ部分）
  - **画像表示領域**: タグ領域を除いたフォルダ前面部分
- 画像は画像表示領域内に収める（はみ出さない）
- タグはタグ領域に塗りつぶし配置（画像の上には重ねない）
- 合成結果を .ico 化する（F-06 と連動）
- プレビュー用と最終ico用、両方で同じ合成ロジックを使う（見た目の一貫性確保）

**テンプレートの基本構造**:
```
┌────────┐
│タグ領域│
├────────┴───────────────────┐
│                            │
│     画像表示領域           │
│                            │
│    (フォルダ前面)          │
│                            │
└────────────────────────────┘
```
- タグ領域はフォルダ左上、全幅の約 30〜40% 程度、高さ約 15〜20% を想定
- 画像表示領域はそれ以外のフォルダ前面エリア
- 具体的な座標・比率は実装時に視覚的に最適化する（最終調整可）

#### F-04: 画像調整ロジック
画像表示領域に合わせて、ユーザーが見える範囲を調整できる。

調整パラメータ:
- **拡大率**: 縮小〜拡大（例: 50% 〜 300%）
- **表示位置**: 上下左右の移動（オフセット）
- **クロップモード**: 「中央でクロップ」「余白付きで全体表示」のどちらか
- **中央に戻す**: ワンクリックで初期位置/初期拡大率にリセット

UIイメージ:
- プレビューのフォルダ画像上で画像をドラッグして位置調整
- スライダーまたはマウスホイールで拡大率調整
- 「中央に戻す」ボタンで初期状態に戻る

#### F-05: 色タグ合成
フォルダ左上のタグ領域に色タグを合成する。

仕様:
- **タグなし** も選択可能（タグ領域はテンプレート標準色のまま）
- **固定プリセット色**（6色 + なし、合計 7 選択肢）:

| プリセット名 | 用途例 | 色コード |
|---|---|---|
| なし | (デフォルト) | テンプレート標準色 |
| 青 | 開発 | `#0078D4` |
| 緑 | 大学 | `#107C10` |
| オレンジ | 動画 | `#D83B01` |
| 紫 | デザイン | `#8764B8` |
| 赤 | 重要 | `#C42B1C` |
| グレー | 保管 | `#7A7574` |

- タグ名（「開発」「大学」等）は**用途例として UI に表示**するが、内部的にはタグ色のみを扱う
- タグはデータ管理機能ではなく、**アイコン生成オプション**として扱う
- 複数タグ・タグ名編集・タグ検索・タグ別一覧などは v1.0 では実装しない
- タグ情報の永続化は履歴レコード内に「適用時のタグ色」として保存するのみ（タグDBは作らない）

UIイメージ:
- 画像選択画面下部に 7 つの色丸ボタンを横並びで配置
- クリックでプレビューに即反映
- タグ名（日本語/英語）はホバーで表示

#### F-06: 画像 → .ico 変換
- F-03 で合成したフォルダ型画像をリサイズし、マルチサイズ .ico として生成
- サイズ: 16, 32, 48, 256 ピクセル（4サイズ統合）
- 小さいサイズ（16/32）ではタグが視認できる程度に強調表示を検討（実装時に調整）
- 生成された .ico は `%LOCALAPPDATA%\Folderly\icons\{hash}.ico` に保管
- 各対象フォルダ内の `.folderly\cover.ico` にコピー（隠し属性）

#### F-07: desktop.ini 生成
- 対象フォルダ内に `desktop.ini` を生成または更新
- 既存の `desktop.ini` がある場合は内容を解析し、必要なキーのみ追加/更新（既存設定を破壊しない）
- 書き込む内容（最小）:
  ```ini
  [.ShellClassInfo]
  IconResource=.folderly\cover.ico,0
  ```
- 文字エンコーディング: UTF-16 LE with BOM（Windows 標準互換）
- 既存内容のバックアップは F-10 の履歴に保存

#### F-08: フォルダ属性の設定
- 対象フォルダに `FILE_ATTRIBUTE_SYSTEM` を付与（desktop.ini を有効化するために必須）
- `desktop.ini` 自体には `FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_SYSTEM` を付与
- `.folderly\` サブフォルダには `FILE_ATTRIBUTE_HIDDEN` を付与

#### F-09: アイコンキャッシュ更新
- `SHChangeNotify(SHCNE_UPDATEIMAGE, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero)` 呼び出し
- 加えて対象フォルダに対して `SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATH, folderPath, IntPtr.Zero)`
- これで再起動なしに反映される（再起動が必要なケースが残った場合はUIで案内）

#### F-10: 履歴管理
- 変更履歴を SQLite に保存（スキーマは Section 5 参照）
- 1フォルダ1レコード（同じフォルダを再変更した場合は上書き）
- 保持される情報:
  - 元のフォルダ属性
  - 元の desktop.ini の有無と内容
  - 適用した画像のパス（元ファイル）
  - 適用日時
  - 生成 .ico のパス
  - 適用時の画像調整パラメータ（拡大率・位置・クロップモード）
  - 適用時のタグ色

#### F-11: 元に戻す機能
- メイン管理画面の履歴一覧から「元に戻す」を選択
- 動作:
  - desktop.ini を元の状態に戻す（元々無ければ削除、あれば内容を復元）
  - フォルダ属性を元に戻す
  - `.folderly\` フォルダを削除
  - SHChangeNotify でキャッシュ更新
  - 履歴レコードを削除（または「復元済み」状態に更新）

#### F-12: 保護機能（適用前チェック）
- 以下のフォルダは適用拒否（理由を表示）:
  - システムフォルダ: `C:\Windows`, `C:\Program Files`, `C:\Program Files (x86)`, `C:\ProgramData` 配下
  - ユーザールート直下の重要フォルダ: `C:\Users\<name>` 直下（Documents, Pictures 等のサブは可）
  - ドライブルート（`C:\`, `D:\` 等）
  - 書き込み権限のないフォルダ
- 以下のフォルダは警告表示後に続行可能:
  - OneDrive 配下（同期される旨を警告）
  - Dropbox 配下
  - ネットワークドライブ
  - パスが長すぎる（260文字超）

#### F-13: メイン管理画面
- スタートメニューから起動可能
- 履歴一覧表示（最近変更したフォルダ）
- 各フォルダに対する「元に戻す」「フォルダを開く」アクション
- 設定画面への遷移
- 試用版の場合は残り日数表示と購入導線

#### F-14: 設定画面
- 言語切替: 日本語 / English / システム言語に従う
- 履歴の最大保持件数（デフォルト 100）
- バージョン情報、ライセンス/試用状態
- サポートリンク（連絡先・ヘルプ）
- 「右クリックメニューの有効/無効」設定は提供しない（F-01 を参照）

#### F-15: ローカライゼーション
- 言語: 日本語 / English
- 実装: `.resx` リソースファイル
- 切替: 設定画面から即時切替可能（再起動不要）
- フォールバック: 未翻訳キーは英語に
- タグの用途例ラベル（「開発」「Development」等）も多言語対応

#### F-16: 試用版判定
- `Windows.Services.Store.StoreContext` でライセンス取得
- 試用中は UI に「無料試用：あと N 日」を表示
- 試用期限切れは Store が自動で起動阻止するため、アプリ内で期限管理は実装不要
- 試用中→購入の変化を `OfflineLicensesChanged` イベントで検知し、UI を更新

#### F-17: ロギング
- 動作ログを `%LOCALAPPDATA%\Folderly\logs\folderly.log` に出力
- ローテーション: 1ファイル 5MB、最大 5世代
- ログレベル: Info（適用・復元）、Warning（保護トリガ）、Error（例外）
- 個人情報・ファイルパスは記録するが、ファイル内容は記録しない

### 3.2 Out of Scope（v1.0 で実装しない機能）

以下は v1.0 では一切実装しないでください。実装が必要そうに見えた場合は確認を求めてください。

- 一括変更（複数フォルダ同時適用）→ v1.1
- プリセット画像集（ストック写真集）→ v1.1
- ダークモード対応 → v1.1
- クラウド同期・設定エクスポート/インポート → v1.1
- ショートカットキー → v1.1
- ドラッグ&ドロップでの複数フォルダ一括登録 → v1.1
- 高度なフォルダ画像エディタ（合成・装飾・ステッカー追加等）→ v2.0
- タグ DB / タグ検索 / タグ別一覧画面 → スコープ外
- 複数タグの付与 → スコープ外
- タグ名の自由編集 → スコープ外
- タグごとのフォルダ管理機能 → スコープ外
- 外部 API 連携 → スコープ外
- アイコンパック販売（アドオン）→ v2.0
- 自動更新機能 → MSIX が自動対応するため不要
- テレメトリ・分析データ送信 → 実装しない（Privacy First）

**タグ機能の境界線（重要）**:
v1.0 で実装するタグは「**アイコン生成時の見た目オプション**」です。データ管理機能ではありません。
- ✅ 入れる: アイコンに色タグを合成する
- ❌ 入れない: タグでフォルダを検索/分類/一覧する機能

### 3.3 非機能要件

- **起動時間**: コールドスタート 1.5 秒以内、ウォームスタート 0.5 秒以内
- **メモリ消費**: 通常時 100MB 以内
- **適用処理時間**: 1フォルダあたり 3 秒以内（合成・変換含む）
- **対応画像最大サイズ**: 入力 8192x8192 まで
- **同時実行**: 単一インスタンス制約（複数起動防止）
- **オフライン動作**: ネット接続不要で完全動作（試用版判定時のみ Store API 通信）

---

## 4. UI Specification

### 4.1 画面1：画像選択画面（右クリックから起動）

**用途**: 右クリックメニューから呼ばれた際のメインウィンドウ

**サイズ**: 560 x 720（リサイズ不可、プレビュー領域を確保するため拡大）

**構成**:
```
┌────────────────────────────────────────┐
│ Folderly                          ─ × │
├────────────────────────────────────────┤
│                                        │
│  📁 C:\Projects\MyApp                  │
│                                        │
│  ┌──────────────────────────────────┐  │
│  │   [フォルダ型プレビュー]          │  │
│  │                                  │  │
│  │   ┌──┐                           │  │
│  │   │🏷│                          │  │
│  │   └──┴──────────────────────┐    │  │
│  │   │                          │    │  │
│  │   │   ここに画像が表示       │    │  │
│  │   │   (ドラッグで位置調整)   │    │  │
│  │   │                          │    │  │
│  │   └──────────────────────────┘    │  │
│  │                                  │  │
│  └──────────────────────────────────┘  │
│                                        │
│  画像: [画像を選択...] [ドロップも可]   │
│                                        │
│  📏 拡大: [─────●─────] 100%           │
│         [中央に戻す]                   │
│                                        │
│  ◉ 中央でクロップ  ○ 余白で全体表示    │
│                                        │
│  🏷 タグ:                              │
│  [なし] [●青] [●緑] [●橙]              │
│  [●紫]  [●赤] [●グレー]                │
│                                        │
│              [キャンセル]  [適用]      │
└────────────────────────────────────────┘
```

**動作**:
- 起動時：コマンドライン引数のフォルダパスをタイトル下に表示
- 画像が未選択の状態：プレビューにプレースホルダー表示、「適用」ボタンは無効
- 画像選択：ファイルダイアログ or ドラッグ&ドロップ
- 画像選択後：プレビューのフォルダ型テンプレート上に画像が合成表示される
- プレビュー上で画像をマウスドラッグ → 表示位置調整
- スライダー or マウスホイール → 拡大率調整
- タグボタンクリック → タグ色が即時プレビュー反映
- 「適用」押下：処理中インジケータ表示 → 完了トースト → ウィンドウ自動クローズ
- 「キャンセル」：何もせずクローズ

**エラー時**:
- 保護対象フォルダ：起動時に警告ダイアログ表示してクローズ
- 画像読み込み失敗：プレビューエリアにエラーメッセージ
- 適用失敗：エラーダイアログ表示、再試行可能

### 4.2 画面2：メイン管理画面

**用途**: スタートメニューから起動する管理画面

**サイズ**: 800 x 600（リサイズ可、最小 600 x 400）

**構成**:
```
┌────────────────────────────────────────┐
│ Folderly                        ─ □ × │
├────────────────────────────────────────┤
│ [履歴] [設定] [ヘルプ]                 │
├────────────────────────────────────────┤
│  最近変更したフォルダ                  │
│  ┌──────────────────────────────────┐  │
│  │ [📷🏷] C:\Projects\MyApp         │  │
│  │      2026/05/19  [開く][元に戻す]│  │
│  ├──────────────────────────────────┤  │
│  │ [📷🏷] D:\Photos\Travel          │  │
│  │      2026/05/18  [開く][元に戻す]│  │
│  └──────────────────────────────────┘  │
│                                        │
│  [履歴をすべてクリア]                  │
├────────────────────────────────────────┤
│ 🎁 無料試用：あと 5日                  │
│ [Microsoft Storeで購入する]            │
└────────────────────────────────────────┘
```

**動作**:
- 履歴一覧は適用日時の降順
- 各行のサムネイル: 適用フォルダアイコンの縮小版（48x48、タグ込み）
- 「開く」: エクスプローラで対象フォルダを開く
- 「元に戻す」: 確認ダイアログ後に復元処理 → 一覧から削除
- 試用版バナーは購入後に非表示

### 4.3 画面3：設定画面

**用途**: アプリ設定変更

**構成**:
```
┌────────────────────────────────────────┐
│ 設定                              ─ × │
├────────────────────────────────────────┤
│                                        │
│  ▼ 言語                                │
│    ◉ システム言語に従う                │
│    ○ 日本語                            │
│    ○ English                           │
│                                        │
│  ▼ 履歴                                │
│    最大保持件数: [100      ▼]          │
│    [履歴をすべて削除]                  │
│                                        │
│  ▼ Folderly について                   │
│    バージョン: 1.0.0                   │
│    ライセンス: 無料試用（あと5日）      │
│    [サポートに連絡] [ライセンス情報]   │
│                                        │
│                          [閉じる]      │
└────────────────────────────────────────┘
```

設定画面からは「右クリックメニューの有効/無効」を意図的に省いています（F-01 の方針）。

### 4.4 画面4：エラー/警告ダイアログ

#### 4.4.1 保護対象フォルダ（適用拒否）
```
[アイコン] このフォルダは変更できません

  C:\Windows はシステムフォルダのため、
  Folderly で変更できません。

  別のフォルダを選択してください。

                              [OK]
```

#### 4.4.2 OneDrive 警告（続行可能）
```
[アイコン] OneDrive のフォルダです

  このフォルダは OneDrive で同期されています。
  Folderly の変更は他のデバイスにも同期される
  可能性があります。

  続行しますか？

                    [キャンセル] [続行]
```

#### 4.4.3 復元確認
```
[アイコン] 元の状態に戻しますか？

  C:\Projects\MyApp の表紙画像とタグを解除し、
  元のフォルダの見た目に戻します。

                    [キャンセル] [元に戻す]
```

### 4.5 ブランド適用ルール

- **メインカラー**: ボタンの primary, アクティブ状態, リンク
- **アクセントカラー** (`#FFB900`): 完了トースト, 試用バナーの星アイコン, 「楽しい」演出のみ
- **エラー色** (`#C42B1C`): エラーダイアログとエラーテキスト
- **角丸**: 4px (Fluent Design 準拠)
- **影**: 控えめに使用（Material より弱め）
- **アニメーション**: 0.2-0.3秒、ease-out。多用しない

### 4.6 フォルダ型プレビューの仕様詳細

プレビューに使うフォルダテンプレートは、Windows 標準フォルダアイコンに近い形状で自作するか、SVG として内蔵します。

**領域定義**:
- **タグ領域**: フォルダ左上の出っ張り（タブ）部分
  - 横幅: フォルダ全体の約 35%
  - 縦位置: フォルダ上端から少し下まで（タブ形状）
  - 用途: タグ色の塗りつぶし表示
- **画像表示領域**: タグ領域を除いたフォルダ前面の大半
  - クリッピング: この領域の外には画像がはみ出さない
  - 用途: ユーザーの選択画像を表示

**プレビューサイズ**:
- 画面上のプレビューは 320x320 〜 400x400 程度（実画面サイズに合わせて調整）
- 実際に生成される .ico の最大サイズは 256x256

**最終アイコンとプレビューの一貫性**:
- プレビューと実 .ico は**同じ合成ロジック**を使う
- 異なるのはレンダリング解像度のみ
- ユーザーが見たプレビュー = 実際のフォルダアイコン、が原則

---

## 5. Data Model

### 5.1 SQLite スキーマ

データベース: `%LOCALAPPDATA%\Folderly\folderly.db`

```sql
-- 履歴テーブル
CREATE TABLE folder_history (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    folder_path     TEXT NOT NULL UNIQUE,
    original_attributes INTEGER NOT NULL,        -- 元のフォルダ属性
    had_desktop_ini BOOLEAN NOT NULL,            -- 元々desktop.iniがあったか
    original_desktop_ini BLOB,                   -- 元のdesktop.ini内容（had=trueの時のみ）
    original_desktop_ini_attrs INTEGER,          -- 元のdesktop.iniの属性
    source_image_path TEXT NOT NULL,             -- ユーザーが選んだ画像のパス
    icon_hash TEXT NOT NULL,                     -- 生成icoのハッシュ
    icon_storage_path TEXT NOT NULL,             -- 中央保管icoのパス
    crop_mode TEXT NOT NULL,                     -- 'center' or 'pad'
    image_scale REAL NOT NULL DEFAULT 1.0,       -- 拡大率
    image_offset_x REAL NOT NULL DEFAULT 0.0,    -- X方向オフセット
    image_offset_y REAL NOT NULL DEFAULT 0.0,    -- Y方向オフセット
    tag_color TEXT,                              -- 'none' or '#0078D4' 等
    applied_at DATETIME NOT NULL,
    schema_version INTEGER NOT NULL DEFAULT 1
);

CREATE INDEX idx_history_applied_at ON folder_history(applied_at DESC);

-- 設定テーブル
CREATE TABLE settings (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);

-- スキーマバージョン管理
CREATE TABLE schema_info (
    version INTEGER PRIMARY KEY,
    applied_at DATETIME NOT NULL
);
INSERT INTO schema_info (version, applied_at) VALUES (1, datetime('now'));
```

### 5.2 設定キー一覧

| キー | デフォルト値 | 説明 |
|---|---|---|
| `language` | `system` | `system` / `ja` / `en` |
| `history_max_count` | `100` | 履歴最大保持件数 |
| `first_launch` | `true` | 初回起動フラグ |

`context_menu_enabled` キーは廃止（F-01 の方針により設定不要）。
`uninstall_behavior` キーも v1.0 では廃止（実装簡略化のため、デフォルトの「適用済みフォルダはそのまま残す」のみ）。

### 5.3 ファイル配置

```
%LOCALAPPDATA%\Folderly\
├── folderly.db             # SQLite履歴
├── icons\                  # 中央集約icoバックアップ
│   └── {sha256}.ico
├── logs\
│   └── folderly.log
└── temp\                   # 一時ファイル

<対象フォルダ>\
├── desktop.ini             # Folderlyが書き込む（hidden+system）
└── .folderly\              # hidden
    └── cover.ico
```

### 5.4 icon_hash の生成

- ハッシュ: 入力画像のSHA-256 + 全合成オプション（crop_mode, scale, offset, tag_color）
- 同じ画像＋同じ合成オプションなら同じハッシュ → 中央保管icoは再利用
- これにより複数フォルダで同じ表紙を使った場合のストレージ節約

---

## 6. Project Structure

```
Folderly/
├── src/
│   ├── Folderly.Core/                  # ビジネスロジック層（テスタブル）
│   │   ├── Composition/
│   │   │   ├── FolderTemplate.cs       # フォルダテンプレート（領域定義）
│   │   │   ├── TemplateRenderer.cs     # テンプレート + 画像 + タグ合成
│   │   │   ├── ImageAdjuster.cs        # 拡大率・位置・クロップ適用
│   │   │   └── TagColors.cs            # 固定プリセット色定義
│   │   ├── Conversion/
│   │   │   └── IcoConverter.cs         # 合成済み画像→マルチサイズ .ico
│   │   ├── Folder/
│   │   │   ├── DesktopIniManager.cs    # desktop.ini読み書き
│   │   │   ├── FolderAttributesService.cs
│   │   │   └── FolderProtection.cs     # 保護判定
│   │   ├── History/
│   │   │   ├── HistoryRepository.cs    # SQLite I/O
│   │   │   └── HistoryEntry.cs
│   │   ├── Application/
│   │   │   ├── ApplyService.cs         # 適用ユースケース
│   │   │   └── RevertService.cs        # 復元ユースケース
│   │   └── Folderly.Core.csproj
│   │
│   ├── Folderly.Shell/                 # Windows API ラッパー
│   │   ├── ShellNotifier.cs            # SHChangeNotify
│   │   ├── NativeMethods.cs            # P/Invoke 集約
│   │   └── Folderly.Shell.csproj
│   │
│   ├── Folderly.App/                   # WPF GUI
│   │   ├── Views/
│   │   │   ├── ApplyWindow.xaml        # 画面1
│   │   │   ├── MainWindow.xaml         # 画面2
│   │   │   ├── SettingsWindow.xaml     # 画面3
│   │   │   ├── Controls/
│   │   │   │   └── FolderPreview.xaml  # フォルダ型プレビューコントロール
│   │   │   └── Dialogs/
│   │   ├── ViewModels/
│   │   ├── Services/
│   │   │   ├── StoreLicenseService.cs  # Store試用判定
│   │   │   └── LocalizationService.cs
│   │   ├── Resources/
│   │   │   ├── Strings.resx            # 英語(既定)
│   │   │   ├── Strings.ja.resx         # 日本語
│   │   │   ├── FolderTemplate.svg      # フォルダテンプレート画像
│   │   │   └── Brand.xaml              # 色・フォント定数
│   │   ├── App.xaml
│   │   └── Folderly.App.csproj
│   │
│   └── Folderly.Package/               # MSIX設定
│       ├── Package.appxmanifest
│       ├── Images/                     # Store用アイコン
│       └── Folderly.Package.wapproj
│
├── tests/
│   └── Folderly.Tests/
│       ├── Composition/
│       ├── Conversion/
│       ├── Folder/
│       ├── History/
│       └── Folderly.Tests.csproj
│
├── docs/
│   ├── SPEC.md                         # 本ドキュメント
│   ├── ARCHITECTURE.md                 # アーキテクチャ図
│   ├── BRAND.md                        # ブランドガイド
│   └── TESTING.md                      # 手動テスト手順
│
├── .gitignore
├── Folderly.sln
├── README.md
└── LICENSE
```

### 6.1 レイヤー依存ルール

```
Folderly.App  →  Folderly.Core  →  Folderly.Shell
              ↘                  ↗
                  (Folderly.Shell 直接呼び出しも可)
```

- `Folderly.Core` は Windows 固有 API に依存しない（テスト容易性のため）
- Windows 固有処理は `Folderly.Shell` に隔離
- `Folderly.App` は両方を参照可
- フォルダテンプレート合成（`Composition/`）は Core 内で完結（OS非依存）

---

## 7. Implementation Order

以下の順序で実装してください。各ステップ完了時にユーザーへ報告し、確認を得てから次へ進んでください。

### Step 1: プロジェクトスケルトン（0.5日）
- ソリューション・プロジェクト作成
- NuGet パッケージ追加
- ディレクトリ構造作成
- README, .gitignore, LICENSE 雛形

### Step 2: Core 層 - フォルダテンプレート定義（1日）
- `FolderTemplate`: テンプレート画像 + タグ領域・画像表示領域の座標定義
- `TagColors`: 固定プリセット色の定義（7種：なし + 6色）
- フォルダテンプレートSVGまたはPNGの作成（リソースとして同梱）
- xUnit テスト（領域座標が正しく取得できるか）

### Step 3: Core 層 - 画像調整ロジック（1.5日）
- `ImageAdjuster`: 拡大率・オフセット・クロップモードの適用
- 画像を画像表示領域にフィットさせるロジック
- xUnit テスト（最低 8 ケース：拡大、移動、クロップ、余白、エッジケース）

### Step 4: Core 層 - テンプレート合成（1.5日）
- `TemplateRenderer`: 
  - 入力: フォルダテンプレート + 画像（調整済み）+ タグ色
  - 出力: 合成済みフォルダ画像（PNG/Bitmap）
- タグ領域にタグ色を描画
- 画像表示領域に画像を描画（クリッピング込み）
- プレビュー用と最終ico用の両方で使える
- xUnit テスト（最低 10 ケース：各タグ色、タグなし、各クロップモード）

### Step 5: Core 層 - 画像 → .ico 変換（1日）
- `IcoConverter`: 合成済み画像をマルチサイズ .ico に変換
- サイズ: 16, 32, 48, 256
- 小サイズでのタグ視認性確保
- xUnit テスト（最低 6 ケース）

### Step 6: Core 層 - desktop.ini 操作（2日）
- `DesktopIniManager`: 読み込み、マージ、書き込み
- `FolderAttributesService`: 属性付与/解除
- UTF-16 LE BOM の正確な扱い
- xUnit テスト（最低 10 ケース）

### Step 7: Shell 層 - キャッシュ更新(1日)
- `ShellNotifier`: SHChangeNotify ラッパー
- P/Invoke 定義
- 統合テスト（実フォルダで反映確認）

### Step 8: Core 層 - 履歴管理（1.5日）
- `HistoryRepository`: SQLite CRUD
- 合成パラメータ（scale, offset, tag_color）の永続化
- マイグレーション機構
- xUnit テスト（最低 8 ケース）

### Step 9: Core 層 - 保護機能（1日）
- `FolderProtection`: 保護判定ロジック
- システムフォルダ・OneDrive・権限チェック
- xUnit テスト（最低 10 ケース）

### Step 10: Core 層 - ユースケース（1日）
- `ApplyService`: 適用フロー全体（合成→変換→属性→ini→キャッシュ→履歴）
- `RevertService`: 復元フロー全体
- 統合テスト

### Step 11: WPF GUI - フォルダプレビューコントロール（2日）
- `FolderPreview` カスタムコントロール
  - フォルダテンプレート表示
  - 画像表示領域に画像を表示
  - タグ領域にタグ色を表示
  - 画像のドラッグ移動対応
  - 拡大率反映
- バインディング可能なプロパティ（Image, Scale, Offset, TagColor, CropMode）

### Step 12: WPF GUI - 画像選択画面（2日）
- `ApplyWindow.xaml` + ViewModel
- フォルダプレビューコントロールの組み込み
- 画像選択（ファイルダイアログ + ドラッグ&ドロップ）
- 拡大率スライダー
- クロップモード切替
- タグ色選択（7ボタン）
- 「中央に戻す」ボタン
- コマンドライン引数からのフォルダパス受け取り

### Step 13: WPF GUI - メイン管理画面（2日）
- `MainWindow.xaml` + ViewModel
- 履歴一覧表示、サムネイル（タグ込み）
- 元に戻す機能の UI

### Step 14: WPF GUI - 設定画面（0.5日）
- `SettingsWindow.xaml`
- 言語、履歴設定、バージョン情報
- 設定の永続化

### Step 15: ローカライゼーション（1日）
- `.resx` リソース整備
- 言語切替機構
- 全 UI 文字列のリソース化
- タグの用途例ラベル（日本語/英語）

### Step 16: Store ライセンス判定（1日）
- `StoreLicenseService`
- 試用版表示、購入後の動的解除

### Step 17: MSIX パッケージング（2日）
- `Package.appxmanifest` 作成
- 右クリックメニュー宣言（`desktop:FileExplorerContextMenus`）
- アイコン・スプラッシュスクリーン設定
- ビルド検証

### Step 18: 統合テスト・調整（2日）
- 全機能の統合動作確認
- パフォーマンス計測
- バグ修正

**想定合計**: 約 24 営業日（Claude Code 実装の場合は短縮可能）

---

## 8. Constraints & Safety Rules

実装中に必ず守ってください。

### 8.1 絶対禁止事項

- **DLL インジェクション・グローバルフック**は使用しない
- **エクスプローラへの不正な拡張**は実装しない（公式の MSIX 拡張のみ）
- **レジストリの広範囲改変**は行わない（右クリック登録は MSIX マニフェスト経由）
- **管理者権限の昇格**を要求しない（v1.0 では不要、ユーザー権限で完結）
- **ユーザー選択フォルダ外への書き込み**は AppData 以外行わない
- **テレメトリ・分析データ送信**は実装しない
- **試用版判定をローカルで完結させない**（Store API を使う）

### 8.2 必須遵守事項

- 全ての破壊的変更（desktop.ini 上書き、属性変更）は履歴に保存してから実施
- 例外は必ずキャッチし、ユーザーに分かりやすいメッセージを表示
- ファイル書き込みは原子的に（一時ファイル経由 → Move）
- パスは常に正規化（`Path.GetFullPath`）
- 文字エンコーディングは明示（UTF-16 LE BOM for desktop.ini, UTF-8 for その他）
- 長いパス対応（`\\?\` プレフィクス、または .NET の長パスサポート有効化）
- マルチスレッドアクセスする SQLite は同期処理（または別途接続）

### 8.3 保護判定の詳細

以下は **絶対に適用を拒否**:
- `Environment.GetFolderPath(SpecialFolder.Windows)` 配下
- `Environment.GetFolderPath(SpecialFolder.ProgramFiles)` 配下
- `Environment.GetFolderPath(SpecialFolder.ProgramFilesX86)` 配下
- `Environment.GetFolderPath(SpecialFolder.CommonProgramFiles)` 配下
- ドライブルート (`Path.GetPathRoot(path) == path`)
- ユーザープロファイルルート（`UserProfile` 直下、ただしサブフォルダは許可）

以下は **警告して続行可能**:
- OneDrive 同期フォルダ（環境変数 `OneDrive`, `OneDriveCommercial` から判定）
- ネットワークパス (`Path.IsPathFullyQualified` + UNC 判定)
- 260 文字超のパス

### 8.4 エラーハンドリング

| 状況 | 対応 |
|---|---|
| 画像読み込み失敗 | UI でプレビューエリアにエラー、再選択を促す |
| 書き込み権限なし | ダイアログで「権限がありません」表示、適用中止 |
| ディスク容量不足 | ダイアログ表示、適用中止 |
| desktop.ini ロック中 | リトライ 3 回、それでも失敗ならエラー表示 |
| SHChangeNotify 失敗 | 再起動 or サインアウトを促すメッセージ |
| SQLite アクセス失敗 | 履歴なしで適用は続行、ログに記録 |
| Store API 失敗 | 試用版として扱う（フェイルセーフ） |
| テンプレート合成失敗 | ログ記録、エラーダイアログ表示、適用中止 |

---

## 9. Testing Plan

### 9.1 単体テスト（Claude Code が書く）

各 Core クラスに最低限のテストを書きます。フレームワーク: xUnit。

#### TemplateRenderer
- タグなしで合成
- 各タグ色（6色）で合成
- 画像が画像表示領域内に収まること
- 縦長・横長画像のクロップ動作
- 余白モードでの全体表示
- タグ色がタグ領域内に塗られること

#### ImageAdjuster
- 拡大率100%で原寸
- 拡大率200%で2倍
- オフセット適用で位置移動
- 中央クロップで正方形化
- 余白モードでフィット
- エッジケース（極小画像、極大画像）

#### IcoConverter
- 4サイズ統合 ico 生成
- 小サイズでのタグ視認性
- 破損ファイルのエラーハンドリング

#### DesktopIniManager
- 新規 desktop.ini 生成
- 既存 desktop.ini への IconResource 追加
- 既存 IconResource の更新
- 他の `[.ShellClassInfo]` キーの保持
- UTF-16 LE BOM の正しい出力

#### FolderProtection
- C:\Windows 拒否
- C:\Program Files 拒否
- ドライブルート拒否
- ユーザーフォルダ直下拒否、サブは許可
- OneDrive パス判定
- 長いパス判定
- 通常フォルダ許可

#### HistoryRepository
- レコード追加・取得（合成パラメータ込み）
- 同一フォルダの上書き
- 履歴件数上限超過時の古いレコード削除
- 復元用クエリ

### 9.2 統合テスト（Claude Code が書く）

- ApplyService の E2E（仮想ファイルシステム or 一時ディレクトリ）
- RevertService で完全に元の状態に戻ることを確認
- 「プレビューと最終 .ico が同じ見た目」の回帰テスト

### 9.3 手動テストチェックリスト（あなたがやる）

`docs/TESTING.md` に以下のチェックリストを生成してください:

- [ ] 通常フォルダで適用 → 即時反映
- [ ] 日本語フォルダ名で適用
- [ ] 長いパス（260文字超）で適用
- [ ] PNG / JPG / WebP それぞれで適用
- [ ] 縦長・横長画像の適用（クロップモード両方）
- [ ] 拡大率の調整が反映される
- [ ] 画像位置のドラッグ調整が反映される
- [ ] 「中央に戻す」で初期状態に戻る
- [ ] タグなしで適用 → タグ領域がテンプレート色
- [ ] 各タグ色で適用 → タグ領域に色が反映
- [ ] 既存 desktop.ini ありフォルダで適用
- [ ] 元に戻す → 完全復元確認
- [ ] OneDrive 配下で警告表示
- [ ] C:\Windows で拒否表示
- [ ] 権限なしフォルダで拒否表示
- [ ] 言語切替（日本語 ⇔ English）
- [ ] タグ用途例ラベルが言語切替に追従
- [ ] 試用版表示・残り日数
- [ ] アンインストール後の右クリックメニュー削除確認
- [ ] プレビューと実フォルダアイコンの見た目が一致

---

## 10. Microsoft Store 提出時の項目

### 10.1 Package Manifest（Package.appxmanifest）

主要項目:
```xml
<Identity Name="Folderly.FolderlyApp"
          Publisher="CN=<Publisher>"
          Version="1.0.0.0" />

<Properties>
  <DisplayName>Folderly</DisplayName>
  <PublisherDisplayName>Folderly</PublisherDisplayName>
</Properties>

<Dependencies>
  <TargetDeviceFamily Name="Windows.Desktop"
                      MinVersion="10.0.17763.0"
                      MaxVersionTested="10.0.26100.0" />
</Dependencies>

<Capabilities>
  <rescap:Capability Name="runFullTrust" />
</Capabilities>

<!-- 右クリックメニュー登録（desktop4 拡張） -->
<Extensions>
  <desktop4:Extension Category="windows.fileExplorerContextMenus">
    <desktop4:FileExplorerContextMenus>
      <desktop4:ItemType Type="Directory">
        <desktop4:Verb Id="Folderly" Clsid="{...GUID...}" />
      </desktop4:ItemType>
    </desktop4:FileExplorerContextMenus>
  </desktop4:Extension>
</Extensions>
```

### 10.2 Store 提出に必要な素材

実装完了後に別途用意（Claude Code には作成不要）:
- アプリアイコン（複数サイズ）
- Store 用スクリーンショット（5枚 × 日英、タグ機能を強調）
- 短い説明・長い説明（日英、「画像 + 色タグ」の価値を訴求）
- Privacy Policy URL
- サポート連絡先

---

## 11. Glossary

- **desktop.ini**: Windows が定義するフォルダカスタマイズ用設定ファイル
- **SHChangeNotify**: シェルに変更を通知する Win32 API
- **MSIX**: Windows モダンアプリパッケージ形式
- **StoreContext**: Microsoft Store のライセンス情報を取得する API
- **マルチサイズ ico**: 1ファイルに複数解像度を含む ico 形式（Windows 標準）
- **フォルダテンプレート**: Folderly が内蔵する Windows 風フォルダ形状の画像
- **タグ領域**: フォルダ左上のタブ部分、タグ色を表示する領域
- **画像表示領域**: タグ領域を除いたフォルダ前面、ユーザー画像を表示する領域

---

## 12. Out of Spec — Future Versions

参考までに、将来バージョンの構想を記載します。**v1.0 では実装しないでください**。

### v1.1（公開後 2-3 ヶ月）
- 一括変更（複数フォルダ選択）
- プリセット画像集（10-20 種類のテンプレート画像）
- ダークモード対応
- 設定エクスポート/インポート
- ショートカットキー

### v2.0（公開後 6 ヶ月）
- 高度なフォルダ画像エディタ（合成・装飾・ステッカー追加）
- 複数タグ対応 / タグ名カスタマイズ
- タグ別フォルダ一覧 / タグ検索
- アイコンパック販売（アドオン）

---

## 13. Communication Protocol（Claude Code 向け）

実装中の報告ルール:

1. **各 Step 完了時**: 実装したファイル一覧、テスト結果、判断した点、詰まった点を報告
2. **仕様外の実装が必要だと判断した時**: 必ず確認を求める。勝手に追加しない
3. **ライブラリの選定で迷った時**: Section 2 を確認、それでも判断できない場合は確認を求める
4. **エラーで進めない時**: 該当エラーメッセージと試した対応を報告
5. **コミット**: 各 Step 完了ごとに 1 コミット、メッセージは英語、`feat:` `fix:` `test:` のプレフィクス付き

実装完了の定義:
- すべての In Scope 機能が動作する
- 単体テストが pass
- 手動テストチェックリストが docs/TESTING.md に存在する
- README に開発環境セットアップ手順が記載されている
- MSIX パッケージが Visual Studio でビルドできる
- プレビューと実際のフォルダアイコンの見た目が一致する

---

## Document Info

- Version: 1.1
- Last Updated: 2026-05-19
- Owner: Folderly Project
- Changelog:
  - v1.1 (2026-05-19): タグ・フォルダテンプレート合成を v1.0 In Scope に追加。画像プレビューをフォルダ型に変更。右クリックメニュー有効/無効設定を削除。
  - v1.0 (2026-05-19): 初版。

# Microsoft Store 提出準備メモ

Folderly を Microsoft Store に出す前に確認する項目です。

## 現状

- アプリ形式: MSIX / WPF / .NET 8 / x64
- パッケージ名: `Folderly.FolderlyApp`
- バージョン: `1.0.0.1`
- 最小OS: Windows 10 1809 (`10.0.17763.0`)
- 右クリックメニュー: Packaged COM SurrogateServer
- 制限付き capability: `runFullTrust`
- ローカルMSIXビルド: 確認済み
- ローカルMSIX署名: テスト証明書で確認済み
- サイドロードインストール: 確認済み

## Store提出前に必ず差し替えるもの

- `src/Folderly.Package/Package.appxmanifest`
  - `Identity Publisher="CN=Folderly"` を Partner Center の正式な Publisher に変更する
  - `PublisherDisplayName` を実際の公開者名に合わせる
  - `DisplayName` はインストール後の表示名なので、基本は `Folderly` のままにする
  - `Version` を提出用バージョンに確定する
- `src/Folderly.Package/Images/`
  - `FolderlyContext.ico`
  - `Square44x44Logo.png`
  - `Square150x150Logo.png`
  - `StoreLogo.png`
  - `Wide310x150Logo.png`
  - `SplashScreen.png`

## 現在の画像サイズ

| ファイル | サイズ | 用途 |
| --- | --- | --- |
| `FolderlyContext.ico` | 16 / 20 / 24 / 32 / 48 / 64 / 128 / 256 を含むICO | 右クリックメニュー |
| `Square44x44Logo.png` | 44 x 44 | アプリ小アイコン |
| `Square150x150Logo.png` | 150 x 150 | スタートメニュー等 |
| `StoreLogo.png` | 50 x 50 | Store/パッケージロゴ |
| `Wide310x150Logo.png` | 310 x 150 | ワイドタイル |
| `SplashScreen.png` | 620 x 300 | スプラッシュ |

アイコンは、`icons/` 配下の元画像から各サイズへ書き出す運用にする。

- `icons/ストア用アイコン.png`: Store/スタートメニュー等のPNG生成元
- `icons/透過アイコン.png`: 右クリックメニュー用ICO生成元

## Partner Center 側で必要なもの

- アプリ名の予約
  - 日本語: `Folderly - フォルダのサムネイル変更`
  - 英語: `Folderly - Folder Thumbnail Changer`
  - インストール後のアプリ表示名は `Folderly` にして、Store掲載名だけ機能説明付きにする方針
- 価格
- 試用版の有無と期間
- 対象市場
- 年齢区分
- カテゴリ
- プライバシーポリシーURL
- サポートURLまたは問い合わせ先
- Store掲載文
  - 短い説明
  - 長い説明
  - 主な機能
  - 検索キーワード
- スクリーンショット
  - 最低1枚は必要
  - 実運用では5枚程度用意する
  - 日本語/英語を出すなら掲載文もスクショも両方準備する

下書き:

- 掲載文: [STORE_LISTING_DRAFT.md](STORE_LISTING_DRAFT.md)
- プライバシーポリシー: [PRIVACY_POLICY_DRAFT.md](PRIVACY_POLICY_DRAFT.md)

## 掲載文に入れるべき注意書き

Folderly は Windows Explorer のアイコンキャッシュ仕様に合わせて、適用後に対象の Explorer ウィンドウを開き直します。

掲載文またはFAQには以下の趣旨を入れる:

> アイコン更新時に Explorer ウィンドウを開き直す場合があります。これは Windows のアイコンキャッシュ更新のための仕様です。

## 提出用パッケージ

Store提出では、通常は `.msix` 単体より `.msixupload` / `.appxupload` の提出が推奨される。

現在の手動 `makeappx` はサイドロード確認には使えるが、Store提出前には Visual Studio の「Create App Packages」または同等の手順で Store提出用パッケージを作る。

## ローカル確認

- [x] Release x64 build
- [x] MSIX作成
- [x] テスト証明書で署名
- [x] サイドロードインストール
- [x] 右クリックメニュー表示
- [x] 右クリックから適用画面起動
- [x] A -> B -> C の再適用反映
- [x] 全フォルダを元に戻す
- [x] 解除後の通常フォルダプレビュー復元
- [ ] 日本語フォルダ名
- [ ] 長いパス
- [ ] 新アイコン反映後の画像確認
- [ ] Store提出用パッケージ作成
- [ ] Partner Center upload

## 参考

- Microsoft Store配布のMSIXは、提出後にMicrosoft Store側で署名される
- Partner Center の Packages ページで `.msix`, `.msixupload`, `.msixbundle`, `.appxupload` などをアップロードできる
- Store認証は提出後に自動で行われる

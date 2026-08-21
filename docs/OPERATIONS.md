# Folderly Operations

## 2026-08-19 Store Submission Notes

### Current Store Submission

- Store candidate version: `1.6.7.0`
- Store upload candidate: `_out\Folderly_1.6.7.0_x64_store.msix`
- Previous submitted version: `1.6.1.0`
- Local verification builds used: `_out\Folderly_1.4.2.0_x64_local_sideload.msix`, `_out\Folderly_1.4.3.0_x64_local_sideload.msix`
- Main changes:
  - Help and Settings now open as main-window tabs instead of separate modal windows.
  - Help tab now has compact Contact Support / FAQ rows and an About Folderly section.
  - Settings tab now keeps only a subtle inline Store rating prompt at the bottom.
  - Support form and FAQ links remain language-aware.
  - v1.6 fixes immediate language switching, passes the selected language to the contact form, and improves clickable blue UI states.
  - Prior v1.4 icon, Japanese Store screenshot, language, purchase link, and review request improvements are retained.

### Store Review Count Issue

Observed:

- Partner Center shows 1 rating/review.
- Public Microsoft Store page can still show 0 reviews.

Likely causes:

- Store catalog cache delay.
- Region-specific review aggregation.
- Review text moderation delay.
- Difference between Partner Center analytics and public Store rating display.
- Rating exists but is not yet eligible for public display.

Action taken:

- Microsoft support request submitted from Engage Center.
- Product: `Windows Developer Center`
- Issue type/subtype used: `App Management`

Follow-up:

1. Wait for Microsoft response.
2. After certification/publication, check both:
   - `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=ja-JP&gl=JP`
   - `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=en-US&gl=US`
3. If public rating remains 0, reply to the support case with:
   - Store ID: `9N99JH5H91H8`
   - App name: `Folderly - Folder Icon Changer`
   - Partner Center screenshot showing 1 review
   - Public Store screenshots showing 0 reviews for JP/US
   - Review date shown in Partner Center: `2026-07-05 UTC`

### Post-Certification Visual Check

After the Store update is live, check:

- App icon is the new v1.4 icon.
- Japanese first screenshot communicates the benefits clearly.
- Supported language includes Japanese on the public catalog.
- Price is JP `¥480` and US `$2.99`.
- Description mentions:
  - own photos/images
  - color tags
  - no `.ico` conversion
  - local/private operation
  - one-time purchase / trial
- Public rating/review count is no longer unexpectedly hidden.

環境固定・パッケージ ID・ビルド・Store 提出・テスト。変化が遅い。

## Store Identity

| Item | Value |
|---|---|
| Package identity name | `KanekoApps.Folderly` |
| Publisher | `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E` |
| Publisher display name | `Kaneko Apps` |
| Package family name | `KanekoApps.Folderly_q8156m1pgwn5a` |
| Store ID | `9N99JH5H91H8` |
| Store URL | `https://apps.microsoft.com/detail/9N99JH5H91H8` |
| Current version | `1.6.7.0`（アプリ表示 `1.6.7`） |
| Min OS | Windows 10 1809 (`10.0.17763.0`) |

バージョンルール：Microsoft Store は4桁目が非ゼロの MSIX を拒否する。`1.6.7.0` は OK、`1.0.0.17` は NG。バージョンは Package.appxmanifest の `Version` と `Folderly.App.csproj` の `<Version>` の両方を更新する。次リリースも 4 桁目 0。

## Key URLs

| Purpose | URL |
|---|---|
| Privacy Policy | `https://folderlyapp.com/privacy/` |
| Partner Center Support | `https://folderlyapp.com/privacy/#support` |
| App Settings Contact Support | JP `https://tally.so/r/q48Bqk` / EN `https://tally.so/r/PdZEN0` |

プライバシーポリシー/FAQ/サポートの公開ページは `folderly-web` の `/privacy/`。アプリ内の問い合わせ導線は Tally フォーム、FAQ 導線は言語別に `/privacy/#faq-ja` または `/privacy/#faq`。

## Build

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  .\src\Folderly.Package\Folderly.Package.wapproj `
  /t:Restore,Build `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:RuntimeIdentifier=win-x64 `
  /p:SelfContained=true
```

出力先：`src\Folderly.Package\bin\x64\Release\`

注意：`WebView2Loader.dll` はパッケージ出力ルートに存在する必要がある（`runtimes\win-x64\native` だけでは不足、`0x8007007E` で失敗する）。

## Store MSIX 作成

Visual Studio がこの環境で Publish / Store / Create App Packages を表示しないため、`makeappx.exe` で手動作成する。

```powershell
$ErrorActionPreference = 'Stop'
$version = '1.6.7.0'
$root = (Resolve-Path .).Path
$outDir = Join-Path $root '_out'
$stage = Join-Path $outDir "store_msix_stage_$version"
$msix = Join-Path $outDir "Folderly_$($version)_x64_store.msix"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $stage | Out-Null

Copy-Item -Path (Join-Path $root 'src\Folderly.Package\bin\x64\Release\*') -Destination $stage -Recurse -Force
Copy-Item -Path (Join-Path $root 'src\Folderly.Package\Package.appxmanifest') -Destination (Join-Path $stage 'AppxManifest.xml') -Force
Copy-Item -Path (Join-Path $root 'src\Folderly.Package\Images') -Destination (Join-Path $stage 'Images') -Recurse -Force

$makeappx = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe'
& $makeappx pack /d $stage /p $msix /overwrite
```

生成ファイル：`_out\Folderly_<version>_x64_store.msix` → Partner Center にアップロード。`_out` の MSIX は生成物（git 管理外）。

MSIX 内容確認：

```powershell
$msix = '_out\Folderly_1.6.7.0_x64_store.msix'
$verify = '_out\verify_store_msix_manifest_1.6.7.0'
$makeappx = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe'
Remove-Item -LiteralPath $verify -Recurse -Force -ErrorAction SilentlyContinue
& $makeappx unpack /p $msix /d $verify
Get-Content (Join-Path $verify 'AppxManifest.xml')
```

確認ポイント：`WebView2Loader.dll`・`coreclr.dll`・`hostfxr.dll`・`PresentationNative_cor3.dll` がルートに含まれること。

## Local Sideload Verification

Store 用パッケージ（Partner Center へアップロードするもの）はローカルに直接インストールできない。ローカルテスト用にはコピーに署名した sideload パッケージを使う。

- Store upload: `_out\Folderly_1.6.7.0_x64_store.msix`（署名なし）
- Local install: `_out\Folderly_1.6.7.0_x64_sideload.msix`（署名済み）

```powershell
$ErrorActionPreference = 'Stop'
$publisher = 'CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E'
$root = (Resolve-Path .).Path
$storeMsix = Join-Path $root '_out\Folderly_1.6.7.0_x64_store.msix'
$sideloadMsix = Join-Path $root '_out\Folderly_1.6.7.0_x64_sideload.msix'
$certPath = Join-Path $root '_out\Folderly_LocalSideload.cer'

$cert = Get-ChildItem Cert:\CurrentUser\My |
  Where-Object { $_.Subject -eq $publisher } |
  Sort-Object NotAfter -Descending |
  Select-Object -First 1

if (-not $cert) {
  $cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $publisher `
    -CertStoreLocation Cert:\CurrentUser\My `
    -KeyExportPolicy Exportable `
    -KeyUsage DigitalSignature `
    -HashAlgorithm SHA256
}

Export-Certificate -Cert $cert -FilePath $certPath -Force | Out-Null
Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null

$elevated = "Import-Certificate -FilePath '$certPath' -CertStoreLocation Cert:\LocalMachine\Root | Out-Null; " +
            "Import-Certificate -FilePath '$certPath' -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null"
$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($elevated))
Start-Process powershell.exe `
  -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-EncodedCommand',$encoded) `
  -Verb RunAs `
  -Wait

Copy-Item -LiteralPath $storeMsix -Destination $sideloadMsix -Force
$signtool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
& $signtool sign /fd SHA256 /sha1 $cert.Thumbprint $sideloadMsix
& $signtool verify /pa /v $sideloadMsix

Get-Process Folderly -ErrorAction SilentlyContinue | Stop-Process -Force
Get-AppxPackage | Where-Object {
  $_.Name -eq 'Folderly.FolderlyApp' -or
  $_.Name -eq 'KanekoApps.Folderly'
} | ForEach-Object {
  Remove-AppxPackage -Package $_.PackageFullName
}

Add-AppxPackage -Path $sideloadMsix
Get-AppxPackage -Name KanekoApps.Folderly
```

既知の失敗パターン：

- `signtool verify` は通るが `Add-AppxPackage` が `0x800B0109` → 証明書を `LocalMachine\Root` と `LocalMachine\TrustedPeople` にインポートしていない。
- 旧パッケージ `Folderly.FolderlyApp 1.0.0.16`（publisher: `CN=Folderly`）が残っているとコンテキストメニューが旧バージョンを起動する。先に削除すること。
- Folderly は single-instance IPC を持つ。旧プロセスが動いているとリクエストが旧インスタンスに転送される。再インストール前に停止すること。

## Tests

自動テスト：

```powershell
dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"
```

除外テスト（`CheckPath_NoWriteAccess_IsDenied`）は Windows のファイルシステム権限挙動に依存するため通常ローカルフローでは除外する。

Store 提出候補作成時は実 Shell 統合テストも実行する（標準ユーザーのターミナルから）：

```powershell
dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj `
  --filter "FullyQualifiedName~ExplorerIconIntegrationTests" `
  --logger "console;verbosity=detailed"
```

手動テストチェックリストは [docs/TESTING.md](TESTING.md) を参照。

## Partner Center Properties

| Item | Value |
|---|---|
| Primary category | `Utilities & tools` |
| Device family | Windows 10/11 Desktop のみ |
| Privacy Policy URL | `https://folderlyapp.com/privacy/` |
| Support URL | `https://folderlyapp.com/privacy/#support` |
| Price | JP `¥480` / US `$2.99` |
| Trial | 1 day |

Age rating：ローカルデスクトップユーティリティ。ユーザー生成コンテンツ共有・ギャンブル・成人向けコンテンツなし。

`runFullTrust` 説明文（Partner Center 入力欄）：

```text
Folderly is a packaged WPF desktop app for customizing user-selected folder icons. It requires runFullTrust to integrate with File Explorer through a folder context-menu handler, write desktop.ini and generated .ico files in selected folders, and notify Windows Shell so updated icons are displayed.

Folderly only operates on folders explicitly selected by the user. It does not require administrator privileges for normal use and does not collect or transmit user files or personal data.
```

## Store Listing Text

> ASO で確定したコピー（2026-07-19 更新）。Partner Center に貼る元データ。

### Japanese

**App Name**: Folderly - フォルダアイコン変更

**Short Description**: Windowsのフォルダアイコンを好きな画像に変更。色タグとプレビューで、仕事・写真・資料フォルダを見つけやすく整理できます。

**Long Description**:

Folderly は、Windows 10 / 11 のフォルダアイコンを好きな画像・写真・ロゴ・スクリーンショットに変更できるアプリです。.ico 変換は不要です。

重要なフォルダを見た目で判別できるようにし、色タグで仕事・写真・資料・学習・制作ファイルを整理できます。右クリックから開き、プレビューを見ながら拡大率・位置・表示モードを調整して、そのまま適用できます。

主な機能:
- フォルダを右クリックして「Folderlyでカスタマイズ」
- 好きな画像（PNG / JPG）をドラッグするだけ。.ico 変換は不要
- 拡大率・位置・表示モードをプレビューで調整
- 色タグやタグ名でフォルダを分類（仕事・学習・写真・制作など）
- 買い切り。サブスクなし・広告なし・PC内だけで完結
- いつでも元のフォルダに戻せる

注意: アイコン適用後、対象の Explorer ウィンドウが一瞬開き直すことがあります。新しいアイコンをすぐ反映させるための動作です。

**Keywords**（検索用語・7枠）: `フォルダ アイコン 変更` / `フォルダ アイコン 画像` / `Windows11 フォルダ アイコン` / `フォルダ 色分け` / `アイコン カスタマイズ` / `デスクトップ 整理` / `ico 変換`

### English

**App Name**: Folderly - Folder Icon Changer

**Short Description**: Change Windows folder icons to your own images. Add color tags, preview the result, and organize folders at a glance — no .ico conversion needed.

**Long Description**:

Folderly helps you change Windows folder icons to your own images, photos, logos, or screenshots — without converting files to .ico.

Turn important folders into visual covers, add color tags, and find the right folder faster in File Explorer. It is made for people who organize work files, photos, design assets, study materials, projects, and archives on a Windows PC.

What you can do:
- Change a folder icon from the right-click menu in File Explorer
- Use PNG or JPG images as folder covers
- Preview the folder icon before applying
- Adjust zoom, position, and fit mode
- Add color tags and optional tag labels
- Edit tag names, colors, and icons
- Revert a folder back to normal anytime
- Run locally on your PC with no subscription and no ads

How it works:
1. Right-click a folder and choose "Customize with Folderly"
2. Select or drag in an image
3. Adjust the preview and choose a color tag
4. Apply the icon

Folderly may briefly reopen the target File Explorer window after applying an icon. This refreshes Windows so the new folder icon appears right away.

**Keywords**（search terms・7 slots）: `change folder icon` / `folder icon changer` / `custom folder icon` / `folder color tag` / `folder image` / `folder organizer` / `desktop organization`

**Product features**:
- Use your own images
- No .ico conversion needed
- Right-click from File Explorer
- Live folder icon preview
- Zoom and position controls
- Fit width and fit height modes
- Color tags and labels
- Edit tag colors and icons
- Revert folders anytime
- One-time purchase, no ads

**What's new in this version**:

```text
Added direct Microsoft Store links for purchase and reviews.

Added Japanese language metadata to the app package.

Improved review discovery after users successfully apply folder icons.
```

**Screenshot Plan**:
1. Explorer 右クリックメニューに Folderly コマンドが表示されている画面
2. 画像エディタ＋フォルダプレビュー画面
3. カスタマイズされた複数フォルダが並ぶ Explorer
4. タグエディタ画面
5. 履歴画面（元に戻すアクション）

## Certification Rejection Playbook

再不合格時の対応手順。

### 不合格通知を受けたら最初にすること

コードを変更する前に以下を保存する：

1. Certification report 全体のスクリーンショットまたは PDF
2. Technical requirement policy 番号、審査コメント全文、影響を受けた URL
3. 審査 OS build・デバイス名・ネットワーク条件
4. 不合格になった提出バージョンと提出日時
5. 提出した MSIX のファイル名と SHA-256

```powershell
Get-FileHash .\_out\Folderly_1.6.7.0_x64_store.msix -Algorithm SHA256
```

### 指摘別の切り分け

| 指摘内容 | 最初に確認するもの | 主な関連ファイル |
|---|---|---|
| アイコンが変わらない | 同じフォルダで A→B 表示・`desktop.ini`・Shell 統合テスト | `ApplyService.cs`, `DesktopIniManager.cs`, `ShellNotifier.cs` |
| Runtime 要求される | MSIX 内の `coreclr.dll`/`hostfxr.dll`/WPF DLL | `Folderly.App.csproj`, `Folderly.Package.wapproj` |
| URL が開かない | Partner Center URL とアプリ内リンクの HTTP 応答 | `SettingsWindow.xaml.cs`, `docs/index.html` |
| `runFullTrust` 説明不足 | 提出欄の文章と実装の理由が一致しているか | `Package.appxmanifest` |
| 右クリックメニューなし | 旧 package 残存・COM server 登録・対象 OS | `Package.appxmanifest`, `ContextMenuHandler.cs` |

### 修正して再提出するとき

1. 修正前に report と提出 MSIX のハッシュを保存。
2. コード修正後、自動テストと手動確認（[docs/TESTING.md](TESTING.md)）を実施。
3. MSIX を展開して self-contained runtime と manifest を確認。
4. バージョンを上げる（例：`1.0.18.0`）。
5. `*_sideload.msix` ではなく `*_store.msix` を Partner Center にアップロード。

再提出前チェックリスト：

- [ ] 審査 report と提出 MSIX の SHA-256 を保存した
- [ ] バージョンを増やした
- [ ] standard local folder で画像 A → B の再適用を確認した
- [ ] .NET Desktop Runtime 未導入環境で起動確認した
- [ ] Settings の `Contact Support` と Partner Center の URL を確認した
- [ ] `runFullTrust` 理由が現在の実装と一致している
- [ ] Store 用 MSIX を展開し、runtime DLL と manifest を確認した
- [ ] Partner Center へ Store 用 MSIX のみアップロードした

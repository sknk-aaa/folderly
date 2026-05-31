# Folderly Operations

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
| Current version | `1.0.17.0` |
| Min OS | Windows 10 1809 (`10.0.17763.0`) |

バージョンルール：Microsoft Store は4桁目が非ゼロの MSIX を拒否する。`1.0.17.0` は OK、`1.0.0.17` は NG。次リリースは `1.0.18.0` とする。

## Key URLs

| Purpose | URL |
|---|---|
| Privacy Policy / GitHub Pages | `https://sknk-aaa.github.io/folderly/` |
| Partner Center Support | `https://sknk-aaa.github.io/folderly/#support` |
| App Settings Contact Support | `https://github.com/sknk-aaa/folderly/issues` |

プライバシーポリシーの公開ページは `docs/index.html`（GitHub Pages 公開済み）。

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
$version = '1.0.17.0'
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
$msix = '_out\Folderly_1.0.17.0_x64_store.msix'
$verify = '_out\verify_store_msix_manifest_1.0.17.0'
$makeappx = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe'
Remove-Item -LiteralPath $verify -Recurse -Force -ErrorAction SilentlyContinue
& $makeappx unpack /p $msix /d $verify
Get-Content (Join-Path $verify 'AppxManifest.xml')
```

確認ポイント：`WebView2Loader.dll`・`coreclr.dll`・`hostfxr.dll`・`PresentationNative_cor3.dll` がルートに含まれること。

## Local Sideload Verification

Store 用パッケージ（Partner Center へアップロードするもの）はローカルに直接インストールできない。ローカルテスト用にはコピーに署名した sideload パッケージを使う。

- Store upload: `_out\Folderly_1.0.17.0_x64_store.msix`（署名なし）
- Local install: `_out\Folderly_1.0.17.0_x64_sideload.msix`（署名済み）

```powershell
$ErrorActionPreference = 'Stop'
$publisher = 'CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E'
$root = (Resolve-Path .).Path
$storeMsix = Join-Path $root '_out\Folderly_1.0.17.0_x64_store.msix'
$sideloadMsix = Join-Path $root '_out\Folderly_1.0.17.0_x64_sideload.msix'
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
| Privacy Policy URL | `https://sknk-aaa.github.io/folderly/` |
| Support URL | `https://sknk-aaa.github.io/folderly/#support` |
| Price | 300 JPY |
| Trial | 7 days |

Age rating：ローカルデスクトップユーティリティ。ユーザー生成コンテンツ共有・ギャンブル・成人向けコンテンツなし。

`runFullTrust` 説明文（Partner Center 入力欄）：

```text
Folderly is a packaged WPF desktop app for customizing user-selected folder icons. It requires runFullTrust to integrate with File Explorer through a folder context-menu handler, write desktop.ini and generated .ico files in selected folders, and notify Windows Shell so updated icons are displayed.

Folderly only operates on folders explicitly selected by the user. It does not require administrator privileges for normal use and does not collect or transmit user files or personal data.
```

## Store Listing Text

### Japanese

**App Name**: Folderly - フォルダのサムネイル変更

**Short Description**: フォルダの見た目を、好きな画像と色タグでカスタマイズできます。

**Long Description**:

Folderly は、Windows のフォルダアイコンを好きな画像でカスタマイズできるデスクトップアプリです。

写真、イラスト、ロゴ、スクリーンショットなどを選んで、フォルダの表紙のように表示できます。色タグやタグ名も使えるので、仕事、学習、制作、保管用など、フォルダの用途を見た目で分けやすくなります。

主な機能:
- フォルダを右クリックしてすぐにカスタマイズ
- 好きな画像をフォルダアイコンに適用
- 余白なし、横幅最大、縦幅最大の表示モード
- 拡大率と X/Y 位置の調整
- 色タグの選択・タグ名の編集・タグアイコンの選択
- アイコン上のタグ名・タグアイコン表示 ON/OFF
- 適用履歴の確認・元に戻す

注意: アイコン更新時に、対象の Explorer ウィンドウを開き直す場合があります。Windows のアイコンキャッシュを更新するための動作です。

**Keywords**: フォルダ, アイコン, カスタマイズ, サムネイル, 整理, タグ, Windows, ファイル管理, デスクトップ

### English

**App Name**: Folderly - Folder Thumbnail Changer

**Short Description**: Customize Windows folders with your own images and color tags.

**Long Description**:

Folderly lets you customize Windows folder icons with your own images.

Choose a photo, illustration, logo, or screenshot and use it like a visual cover for a folder. You can also choose color tags and tag labels, making it easier to visually separate folders for work, study, creative projects, archives, and more.

Key features:
- Customize a folder directly from the right-click menu
- Apply your own image to a folder icon
- Choose from no-margin, fit-width, and fit-height display modes
- Adjust zoom and X/Y position
- Choose color tags, edit tag names, choose tag icons
- Show or hide tag names/icons on icons
- View application history and revert customized folders

Note: Folderly may reopen the target Explorer window after applying an icon to refresh the Windows icon cache.

**Keywords**: folder, icon, customize, folder icon, organization, productivity, windows, tag, file management, thumbnail

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
Get-FileHash .\_out\Folderly_1.0.17.0_x64_store.msix -Algorithm SHA256
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

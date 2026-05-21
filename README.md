# Folderly

Windows フォルダの見た目を「表紙画像 + 色タグ」で変えられる軽量ツール。

## 開発環境セットアップ

### 必要なもの

- .NET 8 SDK (8.0.421+)
- Visual Studio 2022 (17.10+) / Visual Studio 2026  
  必須ワークロード: **Windows アプリケーション開発**（MSIX パッケージングツール含む）
- Windows 10 1809 (build 17763) 以上（実行・テスト環境）

### Core 層のビルド・テスト（クロスプラットフォーム）

```bash
dotnet restore
dotnet build src/Folderly.Core/Folderly.Core.csproj
dotnet test tests/Folderly.Tests/Folderly.Tests.csproj --logger "console;verbosity=normal"
```

### VS2022 で WPF アプリをビルドする（Windows 必須）

1. `Folderly.sln` を Visual Studio 2022 で開く
2. スタートアッププロジェクトを `Folderly.App` に設定
3. `Debug | x64` を選択して **F5** で起動

### MSIX パッケージのビルド（サイドロードテスト用）

Visual Studio の WAP ビルドで `.msix` が生成されない環境があるため、現時点では
`Folderly.Package` の Release 出力を `makeappx` で手動パックします。

```powershell
cd $env:USERPROFILE\dev\folderly

$msbuild = "${env:ProgramFiles}\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"

& $msbuild .\src\Folderly.App\Folderly.App.csproj /t:Restore,Build /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:SelfContained=false
& $msbuild .\src\Folderly.ContextMenu\Folderly.ContextMenu.csproj /t:Restore,Build /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:SelfContained=false
& $msbuild .\src\Folderly.Package\Folderly.Package.wapproj /t:Restore,Build /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:SelfContained=false

$stage = ".\src\Folderly.Package\obj\x64\Release\MsixStage"
$out = ".\src\Folderly.Package\AppPackages\Folderly_1.0.0.1_x64.msix"

Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $stage | Out-Null
New-Item -ItemType Directory -Force ".\src\Folderly.Package\AppPackages" | Out-Null

Copy-Item ".\src\Folderly.Package\bin\x64\Release\*" $stage -Recurse -Force
Copy-Item ".\src\Folderly.Package\Package.appxmanifest" "$stage\AppxManifest.xml" -Force
Copy-Item ".\src\Folderly.Package\Images" "$stage\Images" -Recurse -Force

$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter makeappx.exe |
  Where-Object { $_.FullName -like "*\x64\makeappx.exe" } |
  Sort-Object FullName -Descending |
  Select-Object -First 1 -ExpandProperty FullName

& $makeappx pack /d $stage /p $out /overwrite
```

**ローカルサイドロード手順（開発者テスト）**:

初回のみ、`CN=Folderly` のテスト証明書を作成して `LocalMachine\Root` に信頼登録します。
証明書登録は管理者 PowerShell で実行してください。

```powershell
$cert = New-SelfSignedCertificate `
  -Type Custom `
  -Subject "CN=Folderly" `
  -KeyUsage DigitalSignature `
  -FriendlyName "Folderly Temporary MSIX Certificate" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")

Export-Certificate -Cert $cert -FilePath "$env:TEMP\FolderlyTemporary.cer"
Import-Certificate -FilePath "$env:TEMP\FolderlyTemporary.cer" -CertStoreLocation Cert:\LocalMachine\Root
```

署名とインストール:

```powershell
$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter signtool.exe |
  Where-Object { $_.FullName -like "*\x64\signtool.exe" } |
  Sort-Object FullName -Descending |
  Select-Object -First 1 -ExpandProperty FullName

& $signtool sign /fd SHA256 /sha1 <証明書のThumbprint> $out

Get-AppxPackage *Folderly* | Remove-AppxPackage
Stop-Process -Name dllhost -Force -ErrorAction SilentlyContinue
Add-AppxPackage $out

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class ShellWindowApi {
  [DllImport("user32.dll")] public static extern IntPtr GetShellWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
"@
$shellWindow = [ShellWindowApi]::GetShellWindow()
[uint32]$shellProcessId = 0
[void][ShellWindowApi]::GetWindowThreadProcessId($shellWindow, [ref]$shellProcessId)
if ($shellProcessId -ne 0) {
  Stop-Process -Id $shellProcessId -Force -ErrorAction SilentlyContinue
} else {
  Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue
}
Start-Process explorer.exe
```

`Stop-Process -Name explorer -Force` だけを単独で実行すると、開いている Explorer もまとめて終了して PC 操作が重く止まりやすいため、上記のようにシェル本体の Explorer だけを再起動します。

> **注意**: 右クリックメニューは MSIX インストール後にのみ機能します。  
> `Folderly.App` を直接起動しても COM ハンドラは登録されません。

### プロジェクト構成

| プロジェクト | TFM | 用途 |
|---|---|---|
| `Folderly.Core` | net8.0 | ビジネスロジック（OS 非依存） |
| `Folderly.Shell` | net8.0-windows | Windows API ラッパー（P/Invoke） |
| `Folderly.ContextMenu` | net8.0-windows | File Explorer 右クリックメニュー COM ハンドラ |
| `Folderly.App` | net8.0-windows | WPF GUI（Windows 専用） |
| `Folderly.Package` | — | MSIX パッケージング（WAP プロジェクト） |
| `Folderly.Tests` | net8.0 | xUnit テスト（Core 層対象） |

### 手動テスト

`docs/TESTING.md` の手動テストチェックリストを参照してください。  
MSIX インストール後に Windows 実機でのみ実施可能です。

## ライセンス

Copyright (c) 2026 Folderly. All rights reserved.

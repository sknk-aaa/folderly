# Folderly

Windows フォルダの見た目を「表紙画像 + 色タグ」で変えられる軽量ツール。

## 開発環境セットアップ

### 必要なもの

- .NET 8 SDK (8.0.421+)
- Visual Studio 2022 (17.10+)  
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

1. `Folderly.sln` を Visual Studio 2022 で開く
2. スタートアッププロジェクトを `Folderly.Package` に設定
3. `Release | x64` を選択してビルド
4. 出力先: `src/Folderly.Package/bin/x64/Release/` に `.msix` が生成される

**ローカルサイドロード手順（開発者テスト）**:
```powershell
# 開発者モードを有効化（設定 → 開発者向け → 開発者モード）
# または PowerShell (管理者) で:
Add-AppxPackage -Path "src\Folderly.Package\bin\x64\Release\Folderly.Package_1.0.0.0_x64_Debug.msix"
```

> **注意**: 右クリックメニューは MSIX インストール後にのみ機能します。  
> `Folderly.App` を直接起動しても COM ハンドラは登録されません。

### プロジェクト構成

| プロジェクト | TFM | 用途 |
|---|---|---|
| `Folderly.Core` | net8.0 | ビジネスロジック（OS 非依存） |
| `Folderly.Shell` | net8.0-windows | Windows API ラッパー（P/Invoke） |
| `Folderly.App` | net8.0-windows | WPF GUI（Windows 専用） |
| `Folderly.Package` | — | MSIX パッケージング（WAP プロジェクト） |
| `Folderly.Tests` | net8.0 | xUnit テスト（Core 層対象） |

### 手動テスト

`docs/TESTING.md` の手動テストチェックリストを参照してください。  
MSIX インストール後に Windows 実機でのみ実施可能です。

## ライセンス

Copyright (c) 2026 Folderly. All rights reserved.

# Folderly

Windows フォルダの見た目を「表紙画像 + 色タグ」で変えられる軽量ツール。

## 開発環境セットアップ

### 必要なもの
- .NET 8 SDK (8.0.421+)
- Visual Studio 2022 (17.10+) または VS Code + C# Dev Kit
- Windows 10 1809 (build 17763) 以上（実行環境）

### ビルド

```bash
dotnet restore
dotnet build
```

### テスト（Core 層のみ、クロスプラットフォーム）

```bash
dotnet test tests/Folderly.Tests/Folderly.Tests.csproj --logger "console;verbosity=normal"
```

### プロジェクト構成

| プロジェクト | TFM | 用途 |
|---|---|---|
| `Folderly.Core` | net8.0 | ビジネスロジック（OS 非依存） |
| `Folderly.Shell` | net8.0-windows | Windows API ラッパー（P/Invoke） |
| `Folderly.App` | net8.0-windows | WPF GUI（Windows 専用） |
| `Folderly.Tests` | net8.0 | xUnit テスト（Core 層対象） |

## ライセンス

Copyright (c) 2026 Folderly. All rights reserved.

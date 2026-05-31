# Folderly Handoff

現状・残タスク・既知の問題。

## Current State

- Store package version: `1.0.17.0`
- Store identity: `KanekoApps.Folderly`
- Store candidate: `_out\Folderly_1.0.17.0_x64_store.msix`
- SHA-256: `BC3B2F6E8AFDABBCC7D580BF279860215FECB3FE608BC2A5F3D19B512913F6AE`
- Submission: 2026-05-25 に再提出済み、certification pending
- Tests: `137` passed（filter: `FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied`）
- Sideload: `1.0.17.0` / `KanekoApps.Folderly` でローカル検証済み

## Submission Checklist（1.0.17.0）

完了済み：
- Partner Center identity 確認・manifest 更新
- バージョンを `1.0.17.0` に修正（Store は4桁目が非ゼロを拒否）
- Store candidate MSIX 再生成・内容確認（runtime DLL・WebView2Loader.dll 含む）
- Device family: `Windows 10/11 Desktop` 設定
- ローカル sideload 検証（apply/reapply・self-contained 起動・Support リンク）
- `runFullTrust` 説明文提出
- Package を Partner Center に再提出

保留中：
- Microsoft 審査結果待ち

審査中は提出済み MSIX・SHA-256 を保存しておく。Microsoft が変更を要求しない限り pending package を差し替えない。

再提出が必要になった場合は [docs/OPERATIONS.md](OPERATIONS.md) の Certification Rejection Playbook に従う。

## Known Issues

- 古いドラッグ＆ドロップ履歴エントリ（SourceImagePath が空）は再 Apply するまでプレビュー復元不可。
- 旧パッケージ `Folderly.FolderlyApp 1.0.0.16`（publisher: `CN=Folderly`）がローカルに残っていると新 Store identity のテストに干渉する。テスト前に削除すること。

## Four Things That Must Stay in Sync

以下4つは常に整合している必要がある。一つを変更するときは残り3つへの影響を確認すること：

1. WebView2 エディタプレビュー（`ApplyWindow.html`）
2. WPF/offscreen 正確アイコンレンダラー（`TemplateRenderer.cs`）
3. `ApplyService` 履歴/ソース画像ストレージ
4. Explorer リフレッシュ動作（`ShellNotifier.cs`・`ApplyWindow.xaml.cs`）

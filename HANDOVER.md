# Folderly Handover

This is the main handover document for Claude Code, Codex, or any future agent continuing Folderly development.

## Current Local State

- Last sideloaded package before Store identity change: `Folderly.FolderlyApp 1.0.0.16`
- Store package identity: `KanekoApps.Folderly`
- Store publisher: `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`
- Publisher display name: `Kaneko Apps`
- Current Store package version: `1.0.16.0`
- Latest Store candidate MSIX: `_out\Folderly_1.0.16.0_x64_store.msix`
- Partner Center package upload: previous candidate completed; upload the latest regenerated Store package after fixes
- Tests last run: `133` passed with filter `FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied`
- Package manifest: `src/Folderly.Package/Package.appxmanifest`

## What Matters Most

Folderly is no longer just a basic icon generator. The current UX depends on four things staying in sync:

1. WebView2 editor preview
2. WPF/offscreen exact icon renderer
3. `ApplyService` history/source-image storage
4. Explorer refresh behavior

Most regressions so far happened when one of those was changed without the others.

## Current Apply Flow

Main files:

- `src/Folderly.App/Views/ApplyWindow.xaml.cs`
- `src/Folderly.App/Resources/ApplyWindow.html`
- `src/Folderly.Core/Application/ApplyService.cs`
- `src/Folderly.Core/Application/ManagedSourceImageStore.cs`
- `src/Folderly.Core/Composition/TemplateRenderer.cs`
- `src/Folderly.Core/Composition/FolderTemplate.cs`

Flow:

1. User opens Folderly from the Explorer context menu or app UI.
2. `ApplyWindow` initializes WebView2 and sends state to `ApplyWindow.html`.
3. If the target folder already has history and `SourceImagePath` exists, `TryRestoreExistingCustomization()` loads that managed image into the preview and restores crop mode, scale, X/Y offset, and tag.
4. On apply, `ApplyWindow` sends the current source image as a PNG stream to `ApplyService`.
5. `ApplyService` copies the source image bytes to `%LOCALAPPDATA%\Folderly\source-images\<sha256>.png`.
6. The composed ICO is written to `%LOCALAPPDATA%\Folderly\icons\<sha256>.ico`.
7. A local copy is also written to `<folder>\_folderly\cover_<hash8>.ico`.
8. `desktop.ini` points to the central AppData ICO path.
9. History is upserted with the managed source-image path.
10. Shell notifications are sent.
11. If enabled, the target Explorer window is reopened to force cache refresh.

## Managed Source Images

This was added so drag-and-drop images can be restored later.

- Stored in: `%LOCALAPPDATA%\Folderly\source-images\`
- Filename: SHA256 of the PNG bytes, `.png`
- History field: `HistoryEntry.SourceImagePath`
- Cleanup: unreferenced managed source images are deleted on reapply/revert.

Important caveat:

Old history entries created before this feature may have an empty source path, especially if the image was added by drag and drop. Those cannot be restored retroactively. They become restorable after the user reapplies an image with the current version.

## Preview And Performance Contracts

Main file: `src/Folderly.App/Resources/ApplyWindow.html`

The editor preview has two update paths:

- `transformPreview`: fast, throttled preview update while dragging/sliding.
- `transform`: exact WPF/offscreen render committed on mouseup or after a short delay.

Keep these rules:

- Do not run exact render on every `mousemove`.
- Preview drag should not call `sliders.offsetX.set()` or `sliders.offsetY.set()` continuously.
- X/Y sliders are independent controls; moving the image by holding the preview should not move the slider thumbs live.
- Preview drag should only update `appState.offsetX/Y` and send throttled preview messages.
- Exact rendering happens on mouseup through `postTransformNow()`.
- Wheel zoom updates the scale slider because the scale value itself is user-visible and cheap enough, but exact render is still delayed.
- Treat `scale`, `offsetX`, `offsetY`, and `cropMode` as one transform state. `transform` and `transformPreview` messages must include all four values. A regression occurred when display mode was sent separately; after choosing Fit Width/Fit Height and dragging, the preview image shrank because the crop mode state was not preserved through the drag update.

Current timings:

- Preview throttle: `50ms`
- Delayed exact render after wheel scale: `180ms`

If the editor becomes janky, inspect these functions first:

- `scheduleTransformPreviewPost`
- `scheduleTransformPost`
- `postTransformNow`
- `commitOffsetFromPreview`
- `commitScaleFromPreview`
- preview `mousemove` handler

## Preview Position Accuracy

The WebView preview and final icon must use the same template geometry.

Relevant files:

- `FolderTemplate.GetImageRegionPixelSize()`
- `TemplateRenderer.Render(...)`
- `FolderPreview.xaml/.cs`
- `ApplyWindow.html`

Past bugs:

- The image appeared shifted between preview and final output.
- The final icon showed yellow folder background on the right/bottom edge.
- Changing scale could pin the preview image to the upper-left.
- Selecting a display mode and then dragging the preview could make the image shrink because `cropMode` was not included in transform updates.
- Transparent user-image padding showed the white folder body instead of the yellow folder base.

Current expectation:

- Preview and generated ICO should match.
- The user image should cover the image region without unwanted right/bottom gaps unless the selected crop mode intentionally leaves empty space.
- Scale, X/Y sliders, wheel zoom, and preview drag should all operate on the same coordinate model.

## Image Selection UI

Current UI:

- Main drop area can be clicked to select an image.
- Drag and drop is supported.
- The lower duplicate image-select button was removed.
- `Reset image` clears the image and returns the editor to the empty state.

Do not reintroduce a second image-select button.

## Tag Editor

Current tag functionality:

- Existing fixed tags can be renamed.
- Tag colors can be changed from swatches.
- Tag icons can be selected.
- A setting controls whether tag names are rendered on folder icons.
- A setting controls whether tag icons are rendered on folder icons.

Not supported:

- Creating new tags
- Deleting fixed tags
- Sorting folders by Folderly tag in Explorer
- Explorer custom columns for Folderly tags

The disabled `Add new tag` UI was removed because the feature does not exist.

## Localization

English mode should translate the context menu, image selection screen, tag editor screen, and settings labels. Recent regressions included Japanese text remaining in these areas.

Important English strings include:

- `Customize with Folderly`
- `Select image`
- `Reset image`
- `Show tag name on folder icon`

## Explorer Refresh

Explorer aggressively caches folder icons. Shell notifications alone were not reliable, especially when applying image A then image B to the same folder.

Current behavior:

- Folderly sends shell notifications.
- Then, if the setting is enabled, it finds Explorer windows showing the target folder or parent folder and reopens only those windows.
- It does not kill the shell process or restart taskbar/start menu.

Main file:

- `src/Folderly.App/Views/ApplyWindow.xaml.cs`
- Search for `ReopenExplorerWindowsAsync`.

User-facing explanation:

Explorer windows may briefly reopen after applying an icon. This is intentional to refresh Windows icon cache.

## Context Menu

Main files:

- `src/Folderly.ContextMenu/FolderlyContextMenuHandler.cs`
- `src/Folderly.Package/Package.appxmanifest`
- `src/Folderly.Package/Images/FolderlyContext.ico`

Important:

- Context menu is a Packaged COM `IExplorerCommand`.
- It uses `Folderly.ContextMenu.comhost.dll`.
- Wrong `IExplorerCommand` IID can make the menu appear while `Invoke` does not run.
- Context-menu icon should use the transparent app/context icon, not the Store-only icon.

## Icons And Assets

Current policy:

- Store icon is separate and may be prepared manually.
- App/window/context-menu icons use the transparent Folderly icon assets.
- Do not overwrite Store-specific assets unless the user explicitly asks.

## Build Notes

Use Visual Studio MSBuild for the package project:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  .\src\Folderly.Package\Folderly.Package.wapproj `
  /t:Restore,Build `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:RuntimeIdentifier=win-x64 `
  /p:SelfContained=false
```

For Store submission:

- Visual Studio did not expose `Publish` / `Store` / `Create App Packages` for `Folderly.Package` in this environment.
- The accepted package was created manually with `makeappx`.
- Microsoft Store rejects non-zero MSIX revision numbers; use `1.0.16.0`, not `1.0.0.16`.
- See [docs/STORE_SUBMISSION.md](docs/STORE_SUBMISSION.md) for the exact Partner Center state and remaining submission steps.

Do not manually restart Explorer as a normal packaging step.

## Local Sideload Notes

Use this when validating the packaged context menu locally.

Important distinctions:

- Store upload file: `_out\Folderly_1.0.16.0_x64_store.msix`
- Local install file: `_out\Folderly_1.0.16.0_x64_sideload.msix`
- Store upload package should not be signed locally.
- Local install package must be signed with a certificate whose subject exactly matches the manifest publisher:
  `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`.

The old package `Folderly.FolderlyApp 1.0.0.16` used publisher `CN=Folderly` and is a different package identity from `KanekoApps.Folderly`. Remove it before testing the Store identity, otherwise Explorer context menu tests can accidentally hit the old package.

Sideload gotchas:

- Stop all `Folderly.exe` processes before reinstalling. Folderly has single-instance IPC, so an old running instance can receive requests from a new launch.
- Import the sideload certificate into `Cert:\LocalMachine\Root` and `Cert:\LocalMachine\TrustedPeople`. Current-user trust can allow `signtool verify` to pass while `Add-AppxPackage` still fails with `0x800B0109`.
- If the context menu still looks stale after reinstalling, sign out/in or restart Explorer windows. Do not kill `explorer.exe` as a normal app workflow, but it is acceptable as a local development troubleshooting step when shell extension registration is stale.

## Tests

Standard command:

```powershell
dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"
```

The skipped test checks no-write-access behavior and can be environment-sensitive on Windows.

## Documentation Roles

- `README.md`: build/run overview
- `SPEC.md`: current product and technical spec
- `HANDOVER.md`: practical implementation handover
- `CLAUDE.md`: short implementation contracts for agent behavior
- `docs/TESTING.md`: manual and automated verification checklist
- `docs/STORE_SUBMISSION.md`: Store submission state and Partner Center handover

Old commit-by-commit logs were removed because they made the docs harder to read and no longer helped implementation.

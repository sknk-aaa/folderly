# Folderly Agent Notes

This file is for implementation agents. Keep it short, current, and practical.

## Current State

- Current local app/package version: `1.0.0.13`
- Main app: WPF + WebView2 editor
- Context menu: MSIX Packaged COM `IExplorerCommand`
- Core rendering: ImageSharp -> folder template -> ICO -> `desktop.ini`
- Tests: `dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"`

## Non-Negotiable Implementation Contracts

### Preview Performance

The editor preview must remain smooth.

- Do not run exact WPF/offscreen rendering on every pointer move.
- Use `transformPreview` for throttled, lightweight updates while dragging/sliding.
- Use `transform` only on commit, mouseup, or delayed settle.
- Preview drag must not continuously move the X/Y slider thumbs.
- If jank appears, inspect `ApplyWindow.html` first:
  - `scheduleTransformPreviewPost`
  - `scheduleTransformPost`
  - `postTransformNow`
  - `commitOffsetFromPreview`
  - preview drag handlers

### Preview/Final Icon Consistency

The WebView preview and generated icon must share the same folder template geometry.

- `FolderTemplate.GetImageRegionPixelSize()` is the source for the image region size.
- `TemplateRenderer` and preview code must agree on the visible image area.
- Avoid fixes that make the preview look right but final ICO differ, or the reverse.

### Source Image Restoration

Applied images are copied into Folderly-managed storage.

- Directory: `%LOCALAPPDATA%\Folderly\source-images\`
- History stores the managed copy path.
- Drag-and-drop images are restorable only after being applied with the current storage behavior.
- Old drag-and-drop history entries with empty source paths cannot be recovered retroactively.
- Unreferenced managed source images are cleaned up on reapply/revert.

### Explorer Refresh

Explorer may show stale folder icons even when `desktop.ini` is correct.

- Keep shell notifications.
- Keep the targeted Explorer-window reopen behavior.
- Do not kill or restart the whole Explorer shell as part of normal apply/install behavior.

### UI Scope

- There is one image entry point: the drag/drop area, which also opens the file picker.
- `画像をリセット` clears the current image.
- Do not re-add the lower duplicate image-select button.
- Do not show `新規タグを追加` until actual custom tag creation exists.
- Folder sorting by Folderly tag in Explorer is out of scope.

## Important Files

- `src/Folderly.App/Resources/ApplyWindow.html`: editor UI and interaction logic
- `src/Folderly.App/Views/ApplyWindow.xaml.cs`: WebView bridge, image loading, apply, Explorer refresh
- `src/Folderly.Core/Application/ApplyService.cs`: apply pipeline
- `src/Folderly.Core/Application/RevertService.cs`: revert pipeline
- `src/Folderly.Core/Application/ManagedSourceImageStore.cs`: managed source image cleanup
- `src/Folderly.Core/Composition/FolderTemplate.cs`: template geometry
- `src/Folderly.Core/Composition/TemplateRenderer.cs`: final icon rendering
- `src/Folderly.ContextMenu/FolderlyContextMenuHandler.cs`: Explorer context menu
- `src/Folderly.Package/Package.appxmanifest`: MSIX identity, COM registration, version

## Packaging Notes

- `WebView2Loader.dll` must be copied to the package output root. `runtimes\win-x64\native` alone is not enough.
- Local signing certificate subject: `CN=Folderly`.
- Current output packages are under `_out\Folderly_<version>_x64.msix`.
- `_out` MSIX files are generated artifacts.

## Documentation

For fuller context, read:

- `HANDOVER.md`
- `SPEC.md`
- `docs/TESTING.md`

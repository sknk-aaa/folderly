# Folderly Current Specification

This document describes the current implemented behavior of Folderly. It intentionally omits old investigation logs and obsolete alternatives.

Current Store package version: `1.0.16.0`

## Product Summary

Folderly customizes Windows folder icons by combining:

- A user-selected cover image
- A Windows-style folder template
- A color tag
- Optional tag name/icon overlays

The main entry point is the File Explorer context menu item `Customize with Folderly` in English and the localized Japanese equivalent in Japanese.

## Target Platform

- Windows 10 1809 build 17763 or later
- .NET 8 desktop runtime
- MSIX packaged app
- x64 currently verified
- Microsoft Store distribution target: Windows 10/11 Desktop

## Tech Stack

| Area | Technology |
|---|---|
| App UI | WPF |
| Editor surface | WebView2 embedded HTML/CSS/JS |
| Image processing | SixLabors ImageSharp |
| Data | SQLite via `Microsoft.Data.Sqlite` |
| Shell integration | Packaged COM `IExplorerCommand` |
| Packaging | Windows Application Packaging Project / MSIX |
| Tests | xUnit |

## Core Features

### Explorer Context Menu

- Folder right-click shows the localized Folderly customize command.
- Selecting it launches Folderly for the selected folder.
- The menu is implemented by `Folderly.ContextMenu` as a packaged COM handler.
- The menu icon uses the transparent Folderly context/app icon.

### Image Editor

The editor supports:

- Click drop area to select an image
- Drag and drop image
- Image reset
- Preview drag to move image
- Mouse wheel zoom
- Scale slider
- X/Y offset sliders
- Crop modes:
  - Center/crop
  - Fit width
  - Fit height
- Center/reset position button
- Apply/cancel

The editor should show the same visual result as the generated ICO.

### Preview Performance

The preview uses two update modes:

- Fast preview while the user is interacting
- Exact render on commit

Dragging the preview image must not continuously update slider thumbs. This keeps the editor responsive during long drag sessions.

### Preview/Final Icon Position

The WebView preview and final renderer must share the same folder template geometry. Scale, X/Y sliders, wheel zoom, and preview drag should all operate on the same coordinate model.

Past regressions:

- Image position differed between preview and final output.
- Yellow folder background appeared on the right/bottom edge of the image region.
- Changing scale could pin the image to the upper-left.

### Existing Customization Restore

When the editor opens for an already customized folder:

- The previous image is loaded into the preview if available.
- Crop mode, scale, X/Y offset, and tag are restored.
- The restore source is Folderly-managed image storage, not the original user file.

### Managed Source Image Storage

On apply, the current source image is copied to:

```text
%LOCALAPPDATA%\Folderly\source-images\<sha256>.png
```

History stores that managed path. This makes future preview restoration work even if the original image was deleted or moved.

Cleanup:

- Reapplying a folder deletes the previous managed source image if no other history entry references it.
- Reverting deletes the managed source image if no other history entry references it.

Caveat:

- Old drag-and-drop entries created before this behavior may have empty source paths and cannot be restored retroactively.

### Tag System

Current tags are fixed slots. Users can edit their presentation, not create arbitrary new tags.

Supported:

- Select fixed tag
- Rename fixed tag
- Change tag color
- Select tag icon
- Show/hide tag name on generated icon
- Show/hide tag icon on generated icon

Not supported:

- Creating new tags
- Deleting fixed tags
- Multiple tags per folder
- Explorer sorting/grouping by Folderly tag
- Explorer custom property columns

### Localization

The app supports Japanese and English UI. English mode should translate:

- Explorer context-menu label
- Image-selection screen
- Tag-editor screen
- Settings labels, including `Show tag name on folder icon`

### Apply Output

Apply writes:

- Central ICO:

```text
%LOCALAPPDATA%\Folderly\icons\<sha256>.ico
```

- Local folder copy:

```text
<target folder>\_folderly\cover_<hash8>.ico
```

- `desktop.ini` in the target folder.

`desktop.ini` references the central AppData ICO path. This is intentional because OneDrive can dehydrate or alter per-folder hidden content.

### Revert

Revert restores the folder to its previous state:

- Restores original folder attributes
- Restores original `desktop.ini` content if it existed
- Deletes `desktop.ini` if Folderly created it
- Removes `_folderly`
- Removes legacy `.folderly`
- Deletes the history entry
- Cleans up unreferenced managed source images
- Sends shell refresh notifications

### Explorer Refresh

Explorer can keep stale icon thumbnails even after `desktop.ini` changes. Folderly therefore:

- Sends shell notifications
- Optionally reopens only Explorer windows showing the target folder or its parent

Folderly must not kill the whole Explorer shell during normal apply/revert behavior.

## Store Submission State

- Store identity: `KanekoApps.Folderly`
- Publisher: `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`
- Publisher display name: `Kaneko Apps`
- Current Store package: `_out/Folderly_1.0.16.0_x64_store.msix`
- Partner Center package upload: completed
- Store MSIX versions must use revision `0`; `1.0.16.0` is valid, `1.0.0.16` is not.

See `docs/STORE_SUBMISSION.md` for the current Partner Center checklist.

## Out Of Scope

- Folder sorting/grouping by Folderly tag in Explorer
- Custom Explorer columns/properties
- Arbitrary new tag creation
- Bulk apply to many folders
- Cloud sync for settings
- Telemetry
- Store auto-update logic inside the app
- Built-in marketplace for icon packs

## Important Files

| File | Purpose |
|---|---|
| `src/Folderly.App/Resources/ApplyWindow.html` | WebView2 editor UI and interaction logic |
| `src/Folderly.App/Views/ApplyWindow.xaml.cs` | WebView bridge, load/apply/reset/restore |
| `src/Folderly.Core/Application/ApplyService.cs` | Apply pipeline |
| `src/Folderly.Core/Application/RevertService.cs` | Revert pipeline |
| `src/Folderly.Core/Application/ManagedSourceImageStore.cs` | Managed source image cleanup |
| `src/Folderly.Core/Composition/FolderTemplate.cs` | Folder geometry |
| `src/Folderly.Core/Composition/TemplateRenderer.cs` | Final icon composition |
| `src/Folderly.ContextMenu/FolderlyContextMenuHandler.cs` | Explorer command handler |
| `src/Folderly.Package/Package.appxmanifest` | MSIX identity and COM registration |

## Verification

Use `docs/TESTING.md` for the current verification checklist.
Use `docs/STORE_SUBMISSION.md` for the current Microsoft Store submission state.

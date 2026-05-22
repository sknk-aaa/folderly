# Folderly

Folderly is a Windows desktop app that customizes folder icons with a cover image and a color tag. It is built with C#/.NET 8, WPF, WebView2, ImageSharp, SQLite, and an MSIX packaged context-menu extension.

Current local package version: `1.0.0.13`

## What It Does

- Adds a File Explorer context menu item: `Folderlyでカスタマイズ`.
- Opens an editor for the selected folder.
- Lets the user choose or drag and drop an image.
- Shows a folder-shaped preview that matches the generated icon.
- Supports scale, X/Y position, crop mode, and reset.
- Supports color tags, editable tag names, tag icons, and optional tag label/icon rendering on the folder icon.
- Applies the generated `.ico` through `desktop.ini`.
- Can revert a folder to its previous state.

## Project Layout

| Project | Purpose |
|---|---|
| `Folderly.Core` | Image composition, ICO conversion, history, apply/revert logic |
| `Folderly.App` | WPF app, WebView2 editor, settings, history UI |
| `Folderly.Shell` | `SHChangeNotify` and shell helpers |
| `Folderly.ContextMenu` | Packaged COM `IExplorerCommand` context-menu handler |
| `Folderly.Package` | MSIX packaging project |
| `Folderly.Tests` | xUnit tests for core behavior |

## Key Runtime Data

- Generated central icons: `%LOCALAPPDATA%\Folderly\icons\`
- Managed source-image copies: `%LOCALAPPDATA%\Folderly\source-images\`
- Logs: `%LOCALAPPDATA%\Folderly\logs\`
- Context menu log: `%LOCALAPPDATA%\Folderly\context-menu.log`
- Per-folder local files: `<target folder>\_folderly\cover_<hash8>.ico`

The history DB stores the managed source-image path, not the original user-selected path. This allows future preview restoration even if the original image is deleted or moved.

## Build And Test

Run core tests:

```powershell
dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"
```

The excluded test depends on Windows filesystem permission behavior and is intentionally skipped in this local flow.

Build the Release x64 package output:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
  .\src\Folderly.Package\Folderly.Package.wapproj `
  /t:Restore,Build `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:RuntimeIdentifier=win-x64 `
  /p:SelfContained=false
```

## Create, Sign, And Install MSIX

This repo currently uses a local development certificate with subject `CN=Folderly`.

```powershell
$ErrorActionPreference = 'Stop'
$version = '1.0.0.13'
$root = (Resolve-Path .).Path
$outDir = Join-Path $root '_out'
$stage = Join-Path $outDir "msix_stage_$version"
$msix = Join-Path $outDir "Folderly_$($version)_x64.msix"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $stage | Out-Null

Copy-Item -Path (Join-Path $root 'src\Folderly.Package\bin\x64\Release\*') -Destination $stage -Recurse -Force
Copy-Item -Path (Join-Path $root 'src\Folderly.Package\Package.appxmanifest') -Destination (Join-Path $stage 'AppxManifest.xml') -Force
Copy-Item -Path (Join-Path $root 'src\Folderly.Package\Images') -Destination (Join-Path $stage 'Images') -Recurse -Force

$makeappx = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe'
& $makeappx pack /d $stage /p $msix /overwrite

$signtool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
$cert = Get-ChildItem Cert:\CurrentUser\My |
  Where-Object { $_.Subject -eq 'CN=Folderly' -and $_.HasPrivateKey } |
  Sort-Object NotAfter -Descending |
  Select-Object -First 1

& $signtool sign /fd SHA256 /sha1 $cert.Thumbprint $msix

Stop-Process -Name Folderly -Force -ErrorAction SilentlyContinue
Add-AppxPackage -Path $msix
Get-AppxPackage -Name Folderly.FolderlyApp | Select-Object Name,Version,InstallLocation
```

Do not kill `explorer.exe` as a normal install step. Folderly refreshes affected Explorer windows after apply/revert when the setting is enabled.

## Important Notes For Future Agents

- The WebView2 editor is in `src/Folderly.App/Resources/ApplyWindow.html`.
- Keep preview drag/wheel operations lightweight. Pointer movement should send throttled `transformPreview` updates and commit exact rendering only on mouseup or delayed settle.
- Do not update X/Y sliders during preview drag. The drag state and slider state are intentionally independent to avoid layout churn and jank.
- The lower duplicate image-select button was removed. The remaining image entry point is the drag/drop area; image reset is handled by `resetImage`.
- The tag editor intentionally does not support creating new tags. Do not re-add the disabled `新規タグを追加` UI unless the feature itself is implemented.
- `WebView2Loader.dll` must be present at the package output root as well as under `runtimes\win-x64\native`; otherwise WebView2 can fail with `0x8007007E`.

## Documentation

- Current handover for agents: [HANDOVER.md](HANDOVER.md)
- Implementation notes and contracts: [CLAUDE.md](CLAUDE.md)
- Manual verification checklist: [docs/TESTING.md](docs/TESTING.md)
- Current product/technical spec: [SPEC.md](SPEC.md)

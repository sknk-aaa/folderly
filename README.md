# Folderly

Folderly is a Windows desktop app that customizes folder icons with a cover image and a color tag. It is built with C#/.NET 8, WPF, WebView2, ImageSharp, SQLite, and an MSIX packaged context-menu extension.

Current Store package version: `1.0.16.0`

## What It Does

- Adds a File Explorer context menu item: `Customize with Folderly`.
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
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  .\src\Folderly.Package\Folderly.Package.wapproj `
  /t:Restore,Build `
  /p:Configuration=Release `
  /p:Platform=x64 `
  /p:RuntimeIdentifier=win-x64 `
  /p:SelfContained=false
```

## Store MSIX Creation

Store identity is `KanekoApps.Folderly` / `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`.
Older local sideload builds used a development certificate with subject `CN=Folderly`. Store MSIX versions must use revision `0` (for example, `1.0.16.0`, not `1.0.0.16`).

Visual Studio did not expose `Publish`, `Store`, or `Create App Packages` for `Folderly.Package` in the current environment, so the accepted Store candidate was created with `makeappx` and uploaded directly to Partner Center:

```text
_out/Folderly_1.0.16.0_x64_store.msix
```

Manual Store package flow:

```powershell
$ErrorActionPreference = 'Stop'
$version = '1.0.16.0'
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

Upload the generated `.msix` in Partner Center. For local sideload testing, use a certificate whose subject matches the active package publisher.

## Local Sideload Verification

Do not install `_out/Folderly_1.0.16.0_x64_store.msix` directly for local testing. Store packages are uploaded unsigned and Microsoft signs them during Store ingestion.

For local confirmation, copy the Store package, sign the copy, and install the signed sideload package:

```powershell
$ErrorActionPreference = 'Stop'
$publisher = 'CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E'
$root = (Resolve-Path .).Path
$storeMsix = Join-Path $root '_out\Folderly_1.0.16.0_x64_store.msix'
$sideloadMsix = Join-Path $root '_out\Folderly_1.0.16.0_x64_sideload.msix'
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
```

If Windows shows a certificate trust warning or UAC prompt during the `LocalMachine` certificate import, approve it for local testing. Without LocalMachine trust, `signtool verify` may pass while `Add-AppxPackage` still fails with `0x800B0109`.

Always remove the old `Folderly.FolderlyApp 1.0.0.16` package before testing the Store identity. It uses a different publisher (`CN=Folderly`), and its context-menu registration can make it look like the new build did not install.

Do not kill `explorer.exe` as a normal install step. Folderly refreshes affected Explorer windows after apply/revert when the setting is enabled.

## Important Notes For Future Agents

- The WebView2 editor is in `src/Folderly.App/Resources/ApplyWindow.html`.
- Keep preview drag/wheel operations lightweight. Pointer movement should send throttled `transformPreview` updates and commit exact rendering only on mouseup or delayed settle.
- Transform messages must include `scale`, `offsetX`, `offsetY`, and `cropMode` together. Sending crop mode separately can make the image shrink or jump after choosing Fit Width/Fit Height and then dragging.
- Do not update X/Y sliders during preview drag. The drag state and slider state are intentionally independent to avoid layout churn and jank.
- The lower duplicate image-select button was removed. The remaining image entry point is the drag/drop area; image reset is handled by `resetImage`.
- The tag editor intentionally does not support creating new tags. Do not re-add the disabled `Add new tag` UI unless the feature itself is implemented.
- `WebView2Loader.dll` must be present at the package output root as well as under `runtimes\win-x64\native`; otherwise WebView2 can fail with `0x8007007E`.

## Documentation

- Current handover for agents: [HANDOVER.md](HANDOVER.md)
- Implementation notes and contracts: [CLAUDE.md](CLAUDE.md)
- Manual verification checklist: [docs/TESTING.md](docs/TESTING.md)
- Current product/technical spec: [SPEC.md](SPEC.md)
- Microsoft Store submission notes: [docs/STORE_SUBMISSION.md](docs/STORE_SUBMISSION.md)

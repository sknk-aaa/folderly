# Folderly Testing Checklist

Use this checklist when validating a new Folderly build on Windows.

Current verified baseline:

- Date: 2026-05-23
- Store package identity: `KanekoApps.Folderly`
- Store package version: `1.0.16.0`
- Store candidate package: `_out/Folderly_1.0.16.0_x64_store.msix`
- Partner Center package upload: previous candidate completed; upload the latest regenerated Store package after fixes
- Architecture: x64
- Automated tests: `133` passed
- Final manual QA: completed before Store submission prep

Historical note:

- Last sideload package before Store identity change: `Folderly.FolderlyApp 1.0.0.16`
- Store rejected `1.0.0.16` because Store MSIX revisions must be `0`; use `1.0.16.0`.

## Automated Tests

Run:

```powershell
dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"
```

Expected:

- All included tests pass.
- The excluded permission test is intentionally skipped in the normal local flow.

## Package Verification

- [ ] Release x64 build succeeds.
- [ ] Store MSIX is created with `makeappx`.
- [ ] MSIX payload includes `WebView2Loader.dll` at the package root.
- [ ] `Package.appxmanifest` has Store identity `KanekoApps.Folderly`.
- [ ] `Package.appxmanifest` has Publisher `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`.
- [ ] `Package.appxmanifest` version uses revision `0`, for example `1.0.16.0`.
- [ ] Partner Center accepts the uploaded package.
- [ ] If sideload testing locally, the signing certificate subject matches the active package publisher.
- [ ] If sideload testing locally, install `_out/Folderly_1.0.16.0_x64_sideload.msix`, not the unsigned Store package.
- [ ] The sideload certificate is trusted in LocalMachine Root/TrustedPeople if `Add-AppxPackage` reports `0x800B0109`.
- [ ] Old `Folderly.FolderlyApp 1.0.0.16` is uninstalled before testing the Store identity.
- [ ] App launches from Start menu.
- [ ] Explorer context menu shows the localized Folderly customize command.
- [ ] Context menu has the Folderly icon.
- [ ] Context menu opens the editor for the selected folder.

## Basic Apply

- [ ] Select a normal folder and open Folderly from the context menu.
- [ ] Click the drop area and select a PNG image.
- [ ] Apply succeeds.
- [ ] `<folder>\desktop.ini` exists.
- [ ] `<folder>\_folderly\cover_<hash8>.ico` exists.
- [ ] `%LOCALAPPDATA%\Folderly\icons\<hash>.ico` exists.
- [ ] `desktop.ini` references the central AppData ICO.
- [ ] Explorer shows the customized folder icon after the target window refresh.
- [ ] Applying a different image to the same folder updates the visible icon within a few seconds after refresh.

## Image Selection And Reset

- [ ] Clicking the drop area opens the file picker.
- [ ] Drag and drop accepts image files.
- [ ] There is no second lower image-select button.
- [ ] `Reset image` clears the selected image and returns the editor to the empty state.
- [ ] Apply is disabled when no image is selected.

## Existing Customization Restore

- [ ] Apply an image to a folder.
- [ ] Close the editor.
- [ ] Open the Folderly customize command again for the same folder.
- [ ] The previously applied image appears in the preview.
- [ ] Scale, X/Y position, crop mode, and selected tag are restored.
- [ ] Delete or move the original user-selected image.
- [ ] Reopen the editor and confirm the preview still restores from `%LOCALAPPDATA%\Folderly\source-images`.

Known caveat:

- Older drag-and-drop entries created before managed source-image storage may have empty source paths. Those cannot restore until the folder is reapplied.

## Preview Interaction

This area has regressed before. Test it carefully.

- [ ] Dragging the image in the preview moves the image smoothly.
- [ ] Dragging the image does not move the X/Y slider thumbs live.
- [ ] Releasing the mouse commits the exact render.
- [ ] Repeated drag operations do not make the editor progressively slower.
- [ ] Moving X/Y sliders updates the preview.
- [ ] Moving X/Y sliders commits the final icon position.
- [ ] Mouse wheel over the preview zooms in/out smoothly.
- [ ] Scale slider updates when using mouse wheel zoom.
- [ ] Scale percentage label does not overlap the slider bar.
- [ ] Select each display mode, then drag the preview image. The image must keep that mode's sizing and must not shrink or jump.
- [ ] `Reset position` resets scale and X/Y offset.
- [ ] After many mixed operations, the editor remains responsive.

## Preview/Final Icon Match

- [ ] Center crop mode preview matches the generated folder icon.
- [ ] Fit width mode preview matches the generated folder icon.
- [ ] Fit height mode preview matches the generated folder icon.
- [ ] Transparent or empty areas in the user image reveal the yellow folder base, not a white panel.
- [ ] No unintended right/bottom gaps appear unless the selected crop mode intentionally leaves empty space.
- [ ] Long/tall images are clipped or fitted consistently between preview and actual ICO.
- [ ] Small icon sizes still resemble the customized folder.

## Tags

- [ ] Applying with no tag keeps the normal tag area.
- [ ] Applying each fixed tag color changes the tag area color.
- [ ] Tag names can be edited and saved.
- [ ] Tag color swatches can be changed and saved.
- [ ] Tag icon selection can be changed and saved.
- [ ] Tag label visibility setting is saved.
- [ ] Tag icon visibility setting is saved.
- [ ] When tag label rendering is ON, preview and final icon show the tag label.
- [ ] When tag label rendering is OFF, final icon does not show the tag label.
- [ ] Long tag names do not spill outside the tag area.
- [ ] The tag editor does not show a disabled `Add new tag` control.

## Localization

- [ ] English setting changes the context-menu label text.
- [ ] English setting changes the image-selection screen text.
- [ ] English setting changes the tag-editor screen text.
- [ ] English setting changes the `Show tag name on folder icon` setting text.
- [ ] Japanese setting still shows natural Japanese for the same screens.

## Revert

- [ ] Revert from the main history UI asks for confirmation.
- [ ] Revert removes `desktop.ini` if the folder did not originally have one.
- [ ] Revert restores previous `desktop.ini` content if it existed.
- [ ] Revert removes `_folderly`.
- [ ] Revert also removes legacy `.folderly` if present.
- [ ] Revert removes the history row.
- [ ] Revert deletes the managed source image when it is no longer referenced by any history entry.
- [ ] Normal Explorer folder preview/content view returns after revert.

## Protection And Warning Cases

- [ ] Applying to `C:\` is denied.
- [ ] Applying under `C:\Windows` is denied.
- [ ] Applying under `C:\Program Files` is denied.
- [ ] Applying to OneDrive shows the warning and allows explicit continuation.
- [ ] Very long paths show the expected warning path.
- [ ] Invalid/corrupt image files show an error without crashing.

## Explorer Refresh

- [ ] With Explorer refresh setting ON, only the affected Explorer window is reopened.
- [ ] The taskbar and Start menu are not restarted.
- [ ] With Explorer refresh setting OFF, shell notifications are still sent.
- [ ] Reapplying image A -> B -> C to the same folder eventually shows the latest icon.

User-facing behavior:

Explorer windows may briefly reopen after applying. This is intentional and refreshes Windows icon cache.

## Store Submission

- [ ] GitHub Pages privacy/support URL is public: `https://sknk-aaa.github.io/folderly/`.
- [ ] Support section is public: `https://sknk-aaa.github.io/folderly/#support`.
- [ ] Partner Center category is set to `Utilities & tools`.
- [ ] Device family is `Windows 10/11 Desktop` only.
- [ ] Price/trial/market settings are completed.
- [ ] Age rating questionnaire is completed.
- [ ] `runFullTrust` justification is filled.
- [ ] Submission is sent for certification.

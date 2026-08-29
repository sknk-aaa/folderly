# Folderly Preview Notes

Last updated: 2026-08-29

This document records what was learned from recent preview performance and UX work. Use it before changing `ApplyWindow.html`, `ApplyWindow.xaml.cs`, `ImageAdjuster`, `FolderTemplate`, or `TemplateRenderer`.

## Current Architecture

The apply window preview has two visual paths.

- HTML live preview in `src/Folderly.App/Resources/ApplyWindow.html`
  - Used while the user is dragging the preview image or moving controls.
  - Updates cheaply in the WebView with CSS transforms.
  - Keeps the UI responsive.
- C# rendered preview PNG in `src/Folderly.App/Views/ApplyWindow.xaml.cs`
  - Uses the same rendering path as the final icon through `TemplateRenderer`.
  - Used for the exact preview after mouseup, slider release, reset, mode change, image load, and similar committed states.
  - Is more accurate but heavier than the HTML live layer.

The small visual movement after releasing a drag is currently understood as the swap from the lightweight HTML live preview to the exact C# rendered PNG. It is noticeable in some image/folder positions, but it is much cheaper than exact-rendering every interaction frame.

## Current Release Decision

For the 2.3 release candidate, keep the lightweight live-preview model.

Accepted state:

- Dragging should feel smooth.
- Mouseup may cause a small visual correction when the exact PNG replaces the live layer.
- The exact PNG should still match the final generated icon closely.
- Loading UI appears only before the first preview image is shown.
- Clicking the empty/loading preview area must not open Explorer while the first preview is still loading.

Rejected state:

- Removing or bypassing the HTML live layer during normal preview operations.
- Exact-rendering on every drag movement.
- Any change that removes the small mouseup correction but makes drag, wheel, or slider interaction noticeably heavy.

## What Worked

### Keep WebView2 warm for right-click launch

The right-click first launch improvement is worth keeping. It reduces the cold path from Explorer context menu to apply window without changing preview rendering behavior.

Related commit:

- `4dfebcf` - shortened right-click first launch wait time.

### Keep startup timing debug-only

`StartupTrace` is useful for diagnosis but should not write product logs in Release builds. The current approach keeps the calls in place and compiles logging only for Debug through `[Conditional("DEBUG")]`.

This avoids product log churn while preserving a quick way to measure startup later.

### Show loading only before the first preview image

A loading spinner is useful before any preview exists. It should not reappear after every drag release or exact preview commit, because that makes normal editing feel interrupted.

Current rule:

- Show loading when an exact render is happening and no preview image has been sent yet.
- Clear loading when `folderlySetPreview` or `folderlyClearPreview` runs.
- Do not show loading for post-drag exact refresh after an image is already visible.

### Block clicks while preview is loading

The preview card normally opens the image picker only when no image is selected. During the initial loading state, clicks should be ignored. Otherwise an empty-looking preview can accidentally act like an active click target.

Current rule:

- `preview-card.loading` uses progress cursor.
- Click handler returns early while the card has `loading`.

## What Did Not Work

### Disabling the HTML live preview layer

An attempted fix disabled the HTML live preview during preview operations so the UI waited for exact C# rendered previews. This reduced the visible movement after mouseup, but it made the editor too heavy for release.

Related bad direction:

- `2c0456a` - disabled the HTML live layer during preview operations. This made editing too heavy.

Do not reintroduce this approach as the main interaction path.

### Making live preview geometry chase exact rendering too aggressively

An attempted fix changed the HTML live preview geometry to match the exact rendered PNG more closely. It did not reliably remove the visible yellow-folder/image movement and increased the risk of subtle geometry mismatches.

Related risky direction:

- `f323096` - changed live-preview positioning to chase exact rendering.
- `a1174c1` - tried to keep the live preview while reducing yellow-region movement.

If this area is revisited, treat it as a focused geometry project with screenshots and performance checks, not a quick CSS tweak.

### Showing loading on every exact preview refresh

Showing a loader after mouseup is technically truthful but feels bad. The user is still editing an already visible preview, so replacing the image with a loading state makes the UI feel broken or slow.

Keep loading limited to first preview image availability.

## Transform Contract

`scale`, `offsetX`, `offsetY`, and `cropMode` are one transform state.

When sending `transform` or `transformPreview`, include all four values together. Sending `cropMode` separately or letting JS/C# hold partial transform state has caused regressions before, especially after `FitWidth` or `FitHeight` followed by dragging.

Keep the revision value with transform messages. It prevents stale exact renders from replacing a newer interactive state.

Important JS functions:

- `markTransformChanged`
- `createTransformMessage`
- `scheduleTransformPreviewPost`
- `scheduleTransformPost`
- `postTransformNow`
- `commitOffsetFromPreview`
- `commitScaleFromPreview`
- preview `mousemove` and `mouseup` handlers

Important C# state:

- `_previewRenderVersion`
- `_previewRenderActive`
- `_previewRenderPending`
- `_previewRenderPendingExact`
- `_latestTransformRevision`
- `_previewRenderPendingTransformRevision`
- `_hasSentPreviewImage`

## Performance Rules

Do:

- Keep drag movement on the lightweight HTML live layer.
- Commit exact rendering on mouseup or explicit control release.
- Use a short delayed exact render for wheel/scale changes when needed.
- Keep slider thumbs independent from preview dragging during mousemove.
- Cache source image work where possible.
- Verify that repeated dragging does not progressively slow the editor.

Do not:

- Render exact PNGs on every `mousemove`.
- Update X/Y slider thumbs on every preview drag movement.
- Replace the visible preview with a loader after each drag release.
- Add cross-boundary JS-to-C# calls inside the drag loop unless measured and proven cheap.
- Change preview geometry without checking final ICO consistency.

## Preview And Final Icon Consistency

The exact preview and final icon must stay aligned.

- `FolderTemplate.GetImageRegionPixelSize()` is the source for image-region geometry.
- `TemplateRenderer` is shared by exact preview and final icon generation.
- `ImageAdjuster` affects both preview and final ICO generation. Do not treat it as preview-only.

If the HTML live layer is adjusted, verify both:

- Interaction still feels smooth.
- Exact preview and final generated folder icon still match.

## Known Tradeoff

The current lightweight model has a tradeoff:

- Pro: drag, wheel, and slider interaction stay responsive.
- Con: after mouseup, the exact PNG may visibly correct the live preview by a small amount.

For Folderly, responsiveness is currently more important than eliminating this small correction at the cost of heavy editing. A future fix should aim to reduce the correction without removing the live layer or exact-rendering every frame.

## Future Improvement Ideas

Possible safe directions:

- Measure the pixel delta between HTML live preview and exact preview using screenshots before changing code.
- Tune only the live-preview geometry constants, then compare before/after with the same image and transform values.
- Add a debug-only overlay that draws the expected image region, tab, and folder body boundaries.
- Add a small debounce for exact commits if multiple control updates fire together.
- Investigate whether the live layer can reuse exact preview dimensions without forcing expensive C# rendering.

Risky directions:

- Any fix that blocks UI interaction while C# preview rendering finishes.
- Any fix that hides the live layer during normal drag.
- Any fix that makes image movement feel accurate but delayed.
- Any fix that changes `ImageAdjuster` without final icon tests.

## Manual Checks For Preview Changes

Use at least one wide, one tall, and one square image.

- Select an image and confirm the initial loading indicator appears only before the first preview.
- Click the preview while loading and confirm it does not open Explorer or the file picker unexpectedly.
- Drag the preview image several times and confirm movement stays smooth.
- Release the drag and check whether any visual correction is acceptable.
- Confirm no loader appears after drag release.
- Move X/Y sliders and confirm preview updates.
- Use mouse wheel zoom and confirm scale label and slider remain coherent.
- Test Center, Fit Width, and Fit Height, then drag in each mode.
- Apply the icon and compare Explorer's actual icon with the exact preview.
- Reopen the same folder and confirm source image, scale, offset, crop mode, and tag restore correctly.

## Restore Points

Useful refs from the 2026-08-29 work:

- `before-startup-timing-20260829`
- `before-startup-optimization-20260829`
- `before-startuptrace-license-previewloading-20260829`
- `before-rollback-preview-regression-20260829`
- `df9ef46` - reverted preview fixes back to the stable path.
- `e91d2a0` - created the 2.3 release candidate.

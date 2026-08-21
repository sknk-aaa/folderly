# Folderly Handoff

## 2026-08-21 Update

- Latest source version: `1.6.12.0` (app display `1.6.12`).
- Next Store candidate: `_out\Folderly_1.6.12.0_x64_store.msix`.
- Empty preview card now opens the image picker when clicked. After an image is selected, the same card remains dedicated to drag position adjustment.
- Image picker now starts in the target folder when that folder exists.
- Review prompt is now shown on the 2nd successful apply, 3 seconds after the Explorer reopen flow, with a foreground owner so it does not appear behind Explorer.
- Review prompt now uses a branded Folderly dialog with the app icon and clear Store rating / Later actions instead of the default system MessageBox.
- Review prompt primary button now uses a lighter blue background so black system button text remains readable.
- Review prompt can now be dragged by the card surface while keeping button clicks unaffected.

## 2026-08-20 Handoff

### Latest Web/Marketing Update

- `folderly-web` was updated and pushed.
  - Latest pushed commit: `c8ff440 検索流入向けLPとメタ情報を改善`
- GSC showed the highest-click pages are:
  - `/blog/folder-icon-not-changing/`
  - `/en/blog/folder-icon-not-changing/`
  - `/`
  - `/en/`
  - `/blog/windows-folder-icon-change/`
- The top two "folder icon not changing" pages were improved from ordinary articles into search-intent landing pages:
  - clear symptom-first answer near the top
  - symptom shortcut links
  - stronger explanation of cache, `desktop.ini`, `.ico`, OneDrive, and network folders
  - Store CTA at the end
  - FAQPage JSON-LD added
- Other major pages had title/description/OG/JSON-LD copy adjusted toward search intent:
  - JP/EN LP
  - JP/EN blog index
  - JP/EN "how to change folder icons" articles
  - JP/EN "how to use Folderly" articles
- Validation done for the web update:
  - `git diff --check` passed.
  - JSON-LD parsed successfully across all 11 `index.html` files.
  - Chrome/Playwright checked the top two pages on desktop and mobile.
  - Mobile horizontal overflow was found in long command snippets and fixed.
- `folderly-web` still has local uncommitted image diffs that were intentionally not included:
  - `assets/raw/step1.png`
  - `assets/raw/step2.png`

### URLs To Check For Web Update

- JP top search landing page:
  - `https://folderlyapp.com/blog/folder-icon-not-changing/?v=c8ff440`
  - Check because it is currently the largest GSC click source and should now route high-intent visitors toward Folderly/Store.
- EN top search landing page:
  - `https://folderlyapp.com/en/blog/folder-icon-not-changing/?v=c8ff440`
  - Check because it is the strongest English search page and a likely overseas entry point.
- JP LP:
  - `https://folderlyapp.com/?v=c8ff440`
  - Check because title/description now target "folder icon changer using photos/images" and sale price copy.
- EN LP:
  - `https://folderlyapp.com/en/?v=c8ff440`
  - Check because English metadata now targets "Folder Icon Changer for Windows 11" and "$1.49 until Sep 2".
- JP how-to article:
  - `https://folderlyapp.com/blog/windows-folder-icon-change/?v=c8ff440`
  - Check because title/description now target "フォルダアイコンの変更方法" rather than a narrower "できない" intent.

### Expected Impact

- The update is mainly for improving conversion from existing high-intent search traffic, not for immediate large exposure growth.
- Expected improvements:
  - better search-result CTR from clearer title/description
  - better Store click-through from the two highest-intent article pages
  - more query coverage around "反映されない", "元に戻る", `desktop.ini`, icon cache, `.ico`, OneDrive, and network folders
- Measure after 2-6 weeks in GSC:
  - CTR on the top two article URLs
  - impressions/clicks by query
  - Store CTA clicks if analytics can capture them
  - Store page views and purchases during the sale period

### Current State

- Latest source version: `1.6.3.0`（アプリ表示 `1.6.3`）
- Previous Store submission: `1.6.1.0` was submitted by the user.
- Previous built candidate: `_out\Folderly_1.6.2.0_x64_store.msix`
- Next Store candidate should be built as `_out\Folderly_1.6.3.0_x64_store.msix` if this network-folder copy update is submitted.

### Done Today

- Network folder handling was adjusted without changing the normal local-folder apply flow.
- OneDrive, Dropbox, and long paths no longer show pre-apply warning dialogs.
- UNC network paths such as `\\server\share\folder` still show a network-specific warning.
- Mapped network drives such as `Z:\folder` are now detected via `DriveInfo.DriveType.Network`.
- Network warning is shown only once after the user continues. The app stores this in `network_folder_warning_seen`.
- Apply-result diagnostics were added for cases where files are saved but Explorer does not show the icon.
- Network-folder UI copy now says "limited support" instead of implying full support.
- Help/FAQ copy now explains that local folders are recommended and network drives, NAS, and shared folders may not display custom icons depending on Windows or server settings.
- Network-specific verification failure message now includes diagnostic details:
  - path type
  - whether `_folderly` exists
  - whether the expected icon file exists
  - whether `desktop.ini` exists
  - whether `desktop.ini` references the icon
  - `desktop.ini` Hidden/System attributes
  - folder ReadOnly/System attributes
  - Explorer verification result

### Validation

- `dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"`
  - Passed: 138 tests.
- `dotnet build .\Folderly.sln`
  - Passed.

### Follow-Up

- Build and install `1.6.3.0` locally before submitting if the user wants to verify the new network-folder messages.
- If the customer replies with `\\server\share` vs mapped drive and server/NAS type, use the diagnostics to decide whether this is Windows trust policy, server attribute support, or Explorer verification only.
- Do not add registry/Group Policy/Trusted Sites automation. Those are security-sensitive and should remain manual guidance only.

## 2026-08-19 Handoff

### Current State

- Repository: `C:\src\folderly-win`
- Branch: `main`
- GitHub: pushed to `origin/main`
- Current Store submission version: `1.6.1.0`
- App display version: `1.6.1`
- Store upload package: `_out\Folderly_1.6.1.0_x64_store.msix`
- Local verification package: `_out\Folderly_1.6.1.0_x64_sideload.msix`
- Installed locally: `KanekoApps.Folderly 1.6.1.0`
- Store submission: already submitted by user

### Done Today

- Help and support flow
  - Help no longer opens a separate dialog.
  - Help is shown as a main-window tab, like History.
  - Help tab has two simple actions:
    - Contact support
    - FAQ
  - "About Folderly" was moved to the bottom of the Help tab.
  - Contact form URL now switches by language:
    - Japanese: `https://tally.so/r/q48Bqk`
    - English: `https://tally.so/r/PdZEN0`

- Settings and review flow
  - Store review request remains in Settings, but made calmer than the first oversized version.
  - Language switching now applies immediately without moving to another tab.
  - Japanese language now opens the Japanese Tally form instead of the English form.

- Tag edit dialog
  - Removed the non-functional bottom `Delete` button.
  - The footer now only has `Cancel` and `Save`.
  - The remaining primary button color was lightened because dark blue with dark text looked hard to read.

- Icon apply reliability
  - Added apply-result verification after saving folder icons.
  - When Windows still does not report the new icon, Folderly shows a support-oriented warning instead of silently looking successful.
  - This was added carefully without changing the core apply pipeline more than necessary.

- Store package
  - Updated package/app version to `1.6.1.0` / `1.6.1`.
  - Built Store MSIX and sideload MSIX.
  - Installed sideload build locally for visual confirmation.
  - User submitted `_out\Folderly_1.6.1.0_x64_store.msix` to Partner Center.

- Repository
  - Pushed all committed `folderly-win` changes to GitHub.
  - Latest pushed commit: `8b1753e`

### Validation

- `dotnet build .\Folderly.sln`
  - Passed.
- `dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"`
  - Passed: 138 tests.
- MSIX creation:
  - Store package created: `_out\Folderly_1.6.1.0_x64_store.msix`
  - Local sideload package created and installed: `_out\Folderly_1.6.1.0_x64_sideload.msix`

### Important URLs

- Japanese Store:
  - `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=ja-JP&gl=JP`
- English Store:
  - `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=en-US&gl=US`
- Japanese LP:
  - `https://folderlyapp.com/`
- English LP:
  - `https://folderlyapp.com/en/`
- Japanese support form:
  - `https://tally.so/r/q48Bqk`
- English support form:
  - `https://tally.so/r/PdZEN0`

### What To Check After Store Approval

1. Store listing reflects version `1.6.1.0`.
2. Japanese Store page still displays Japanese listing content and support language properly.
3. English Store page displays English listing content properly.
4. Public rating/review count issue is still under Microsoft support investigation.
5. Review replies are visible if Microsoft exposes them.
6. In-app Help opens the Help tab, not an external page or dialog.
7. Japanese language opens `https://tally.so/r/q48Bqk`.
8. English language opens `https://tally.so/r/PdZEN0`.
9. Tag edit dialog no longer shows a disabled Delete button.
10. Primary button background is light enough to read comfortably.

### Microsoft Support Status

- Support case:
  - `2607300030003368`
- Issue:
  - Partner Center shows rating/review data, but public Microsoft Store shows `0` ratings/reviews.
- Microsoft confirmed:
  - They also see no review on the public Store page.
  - The Japan-region review is not showing even in the Japan market.
  - They escalated the issue to the US-side related team.
- Suggested by Microsoft:
  - Replying to the review may affect public display.
  - Store-published replies cannot be edited later.

### Pricing / Growth State

- Store sale pricing was configured by the user:
  - Default: `$1.49`
  - Japan: `¥300`
  - End date: `2026-09-02`
- `folderly-web` LP was updated and pushed separately to reflect sale pricing.
- Growth hypothesis:
  - Short term: lower price to increase downloads and collect ratings.
  - Medium term: strengthen LP and SEO pages.
  - Overseas exposure still needs more non-Japanese acquisition work.

### Remaining Tasks

- Wait for Store certification result for `1.6.1.0`.
- After approval, check live Store pages in Japanese and English.
- Reply to Microsoft support if review visibility changes after review replies or Store update.
- Watch acquisition/review metrics for the sale period through `2026-09-02`.
- If ratings still do not show publicly, follow up with Microsoft using support case `2607300030003368`.
- Continue external exposure work:
  - note article
  - Reddit post
  - Product Hunt preparation
  - AlternativeTo or similar directory listing
  - more English SEO pages

### Local Files Not Included In Git

These remain local and were intentionally not committed/pushed:

- `_out/` generated MSIX packages, staging folders, and verification outputs
- `0730_GSC/` Search Console export data
- `.claude/` local Codex/Claude settings and skills
- `errors/` local crash/debug materials
- root `AGENTS.md` if untracked in this repo

Do not delete these casually. They may be useful locally, but they are not part of the committed product source.

## Engineering Notes

- Store MSIX version fourth component must be `0`.
  - Good: `1.6.1.0`
  - Bad: `1.0.0.17`
- If rebuilding a same-version MSIX with different content, Windows may reject local reinstall with `0x80073CFB`.
  - For local visual confirmation, bump the package version.
- Do not remove Explorer refresh behavior:
  - shell notification
  - target Explorer window reopen behavior
  - no global Explorer process kill in normal apply/install flows
- Preview and final icon rendering must stay aligned:
  - `FolderTemplate.GetImageRegionPixelSize()` is the geometry source.
  - Keep `TemplateRenderer` and preview behavior in sync.

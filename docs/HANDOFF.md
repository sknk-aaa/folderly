# Folderly Handoff

## 2026-08-29 Update - 2.3 Store Candidate

- Current release candidate version: `2.3.0.0` (app display `2.3`).
- Store upload package:
  - `_out\Folderly_2.3.0.0_x64_store.msix`
  - SHA-256: `4DD1B62E774234D55C667FFE82C7840A3E059E3B16985AF99035667D24B23B34`
  - Size: `94,014,728` bytes
  - Built at: `2026-08-29 19:11:05`
  - Signature: `NotSigned` (intended for Partner Center upload)
- Local install package:
  - `_out\Folderly_2.3.0.0_x64_sideload.msix`
  - SHA-256: `0724122439B80FBD04DF201BD38259AEB0706497DD72FB76BBB7294303EF1613`
  - Size: `94,033,648` bytes
  - Signature: `Valid`
- Installed locally on this PC:
  - `KanekoApps.Folderly 2.3.0.0`
  - `KanekoApps.Folderly_2.3.0.0_x64__q8156m1pgwn5a`

### Done Since 2.2

- Right-click first launch was shortened while keeping the existing 5-minute background keep-alive behavior.
- `StartupTrace` remains available for Debug builds, but product Release builds no longer write startup timing logs.
- The apply preview now blocks clicks while the first preview image is still loading, so an empty preview card cannot accidentally open Explorer.
- The loading indicator is limited to the initial preview image load. It is not shown again during normal preview dragging after an image is visible.
- Preview changes that made the app heavy were reverted. The current preview behavior is back on the stable lightweight live-preview path, with only the initial loading/click guard retained.

### Validation Done

- `dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"`
  - Result: `149 passed`
- Release MSBuild for `Folderly.Package.wapproj`
  - Result: passed
  - Warning: known `NU1702` only
- Store MSIX was packed with `makeappx`.
- Store MSIX was unpacked and checked:
  - Identity: `KanekoApps.Folderly`
  - Publisher: `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`
  - Version: `2.3.0.0`
  - Required root files present: `WebView2Loader.dll`, `coreclr.dll`, `hostfxr.dll`, `PresentationNative_cor3.dll`, `Folderly.exe`
- Local sideload MSIX was signed, verified, installed locally, and version-confirmed.

### Manual Check Notes

- User confirmed earlier that the loading behavior no longer appears after releasing the preview drag.
- User observed that the small movement of the yellow folder/image after releasing drag still appears. It is likely part of the pre-existing lightweight preview-to-exact-preview swap, not caused by the retained initial loading/click guard changes.
- Do not reintroduce the exact-render-on-preview-operation approach from the reverted preview attempts; it removed the visible movement but made the app too heavy for release.
- Preview lessons from the 2026-08-29 work are summarized in [PREVIEW_NOTES.md](PREVIEW_NOTES.md).
- Before Store submission, do a quick smoke check from Explorer right-click:
  - first launch opens correctly
  - second launch within 5 minutes is fast
  - empty/loading preview card does not open Explorer
  - apply/revert still work on an ordinary local folder

### Current English Store Listing / ASO Test 1

- Source of truth: the current Partner Center English (United States) screen and [OPERATIONS.md](OPERATIONS.md) -> `Store Listing Text`.
- Do not treat the local `listingData-*.csv` export as authoritative if it conflicts with the Partner Center screen; it may be stale.
- Product name: `Folderly - Folder Icon Changer`
- Short title: `Folderly`
- Short description: `Change Windows folder icons with your own photos and images, then spot them instantly on the desktop and in File Explorer.`
- Description opening: `Change plain Windows folder icons into visual shortcuts for your desktop and File Explorer.`
- Keywords:
  - `folder icons`
  - `change folder icon`
  - `folder icon maker`
  - `windows folder icon`
  - `custom folder icon`
  - `desktop folder icons`
  - `photo folder icon`
- What's new:
  - `Improved preview performance when moving or scaling images.`
  - `Reduced preview jumps and made the preview closer to the final folder icon.`
  - `Refined tag icons and added more icon choices.`
  - `Made startup initialization lighter.`
- ASO priority from 2026-08-29 discussion: English ranking is the main priority, followed by non-Japanese localizations.
- ASO Test 1 changed only English listing text/search terms; do not mix screenshot, price, category, or product-title changes into the same measurement window unless explicitly deciding to abandon clean attribution.
- Baseline Store search positions before this change, measured 2026-08-29 with the unofficial Store search JSON endpoint (`market=US`, `locale=en-US`, `deviceFamily=windows.desktop`):
  - `folder icon changer`: 15
  - `folder icons`: 14
  - `change folder icon`: 11
  - `custom folder icons`: 20
  - `custom folder icon`: 15
  - `desktop folder icons`: 12
  - `photo folder icon`: 12
  - `folder cover`: 11
  - `customize folder`: 12
  - `folder icon maker`: not returned
  - `windows folder icon`: not returned
  - `windows folder icons`: not returned
  - `folder color`: not returned
  - `change folder color`: 10
  - `folder color changer`: not returned
- Recheck the same queries after the submission is live, then again after 24 hours, 72 hours, and 7 days if possible. Store rankings can vary by market, account state, and time, so compare against the same endpoint/market/locale when possible.
- Important reasoning rule for future ASO work: separate Microsoft-documented facts, Store search observations, and hypotheses. Do not present keyword or price guesses as certain ranking causes.

### Current Japanese Store Listing

- Corrected latest values are in [OPERATIONS.md](OPERATIONS.md) -> `Store Listing Text`.
- 2026-08-30 pricing update: Japan-only base price was changed to `0`, with paid pricing scheduled to resume on 2026-09-30. This is a temporary acquisition/review-threshold experiment, not a permanent free strategy.
- Listing-copy status: finalized by the user on 2026-08-30. The copy openly says the free period is to let more people try Folderly and support future improvements; it also asks for reviews/ratings only if the user likes it. Treat the exact current copy in [OPERATIONS.md](OPERATIONS.md) as the source of truth.

- Source of truth: the current Partner Center Japanese screen and [OPERATIONS.md](OPERATIONS.md) -> `Store Listing Text`.
- Updated by the user on 2026-08-30 to make the Japanese Store copy closer to the English listing strategy.
- Product name: `Folderly - フォルダアイコン変更ツール`
- Short title: `Folderly`
- Short description: `Windowsのフォルダアイコンを好きな写真や画像に変更。デスクトップやエクスプローラーで、目的のフォルダをすぐ見分けられます。`
- Description opening:
  - `デスクトップやエクスプローラーのフォルダを好きな写真や画像で見分けやすくできます。`
  - `Folderlyは、Windowsのフォルダアイコンをかんたんに変更できるアプリです。写真、イラスト、スクリーンショットなどを選ぶだけでフォルダの表紙のように表示できます。`
- Keywords currently entered:
  - `フォルダ アイコン 変更`
  - `フォルダ アイコン カスタマイズ`
  - `アイコン 変更 ツール`
  - `フォルダー デザイン 変更`
  - `デスクトップ フォルダ アイコン`
  - `フォルダ画像変更`
- Note: the current Japanese Partner Center entry has 6 keyword chips, not 7.
- What's new:
  - `画像の移動・拡大縮小時のプレビュー動作を軽くしました。`
  - `プレビュー表示を実際のフォルダアイコンにより近づけました。`
  - `タグアイコンを調整し、選べるアイコンを増やしました。`
  - `初回起動時の初期化を軽くしました。`

### End-of-Day ASO / Pricing Notes

- User changed the English Partner Center listing to the ASO Test 1 values above.
- Current priority:
  - 1: English ranking and English acquisition.
  - 2: non-Japanese localized listings, with pt-BR currently looking weakest from Store search checks, then zh-CN, then es-MX/es-ES.
  - JP is lower priority for now because the exact query `フォルダ アイコン 変更` ranked 1st in the same style of Store search check.
- Pricing question discussed: only compare `$2.49 -> $1.49` vs `$3.49 -> $1.49`; do not introduce `$2.99` unless the user explicitly asks.
- Recommendation recorded: prefer `$2.49 -> $1.49` for the current stage because the goal is English acquisition/review/ranking growth and a lower post-sale regular price should reduce purchase friction. `$3.49 -> $1.49` has a stronger discount display but is less attractive after the sale and should wait until Folderly has stronger English proof, reviews, and ranking.
- Measurement caveat: changing price during the ASO Test 1 measurement window can confound attribution. If price is changed anyway, record the exact live date/time and compare Partner Center page views, acquisition, conversion, and ratings alongside Store search positions.

### Free Promotion Discussion For Reviews

- User clarified: the goal is not permanent free distribution; Folderly's purpose remains making money. Any free pricing should be a short, controlled experiment.
- User also clarified that `$0.99` is not useful for this question because the biggest behavioral difference is free vs paid, not a small discount.
- Review-focused free experiment logic:
  - US short-term free can increase acquisition volume in the largest confirmed market and may help review count if users have a good first experience.
  - Japan short-term free is also worth considering because Japan already has about `3` reviews; this may make it easier to test whether Microsoft Store's public review display threshold is global or market-specific.
  - If Japan crosses public review visibility while US does not, that suggests market-specific behavior. If both markets start showing reviews after total review count rises, that suggests a global threshold or mixed logic. This is not definitive because Store caching/moderation can still delay display.
- Guardrails:
  - Do not ask for reviews in exchange for free access or imply a review is required.
  - Record exact market, start/end date, price state, review count before/after, visible public rating state for JP/US, installs, first launches, and support/issues.
  - Free pricing may confound ASO Test 1, so label it as a separate review-threshold/acquisition experiment if used.
  - Check the app behavior after a free-market install: current review prompt appears only when `AppServices.License.IsActive && !AppServices.License.IsTrial`, so verify whether a free acquisition is treated as non-trial and still triggers the review prompt after successful apply.

## 2026-08-30 Store Analytics Snapshot

- Source image: local `a.png`, Microsoft Partner Center -> Analytics -> Acquisitions, date range shown as past 3 months.
- Visible totals in the screenshot:
  - Page views: about `2K` in the summary card; detailed/source charts show `1.71K`.
  - Installs: `49` in the summary/detailed install chart.
  - Acquisition graph total: `64`.
  - Conversion: `2.87%` page-view-to-install.
  - Install success rate: `100%`.
  - Total revenue: `$62.36`.
  - Acquisition channel chart:
    - Page views: `1.71K`
    - Install attempts: `65`
    - Successful installs: `50`
    - Initial launches from Microsoft Store: `30`
  - User-initiated cancellations: `15`.
  - Install errors: no data visible.
  - Custom campaign performance: no campaign installs/page views/conversions visible for the listed campaigns.
  - Visible install geography page:
    - United States: `14` (`28%`)
    - Japan: `10` (`20%`)
    - Germany: `4` (`8%`)
    - Mexico: `3` (`6%`)
    - United Kingdom: `3` (`6%`)
    - Canada: `3` (`6%`)
    - France: `3` (`6%`)
- Update timeline provided by the user:
  - 2026-08-23 update:
    - improved description and short description
    - replaced Store screenshots from the second image onward; first screenshot stayed as the original casual one
    - user observed page views increased about 4x for two days
    - changed price from `$2.99` to `$2.49`
  - 2026-08-25 update:
    - Folderly 2.0
    - improved preview quality, first-run guide, image reset, Help/Settings, purchase guidance, and review-request timing
    - improved Fit Width/Fit Height image positioning behavior
    - improved guidance for folders where Windows may not immediately show icon changes
  - 2026-08-26 update:
    - added 3 localized Store listings
    - localized Store screenshots were still English
  - 2026-08-27 update:
    - replaced Store screenshots for the 3 localized listings
    - made preview real-time and lightweight
    - improved tag icons and added more choices
    - enabled 40% discount in the 3 localized markets
  - 2026-08-29 update:
    - English Store listing only
    - improved Store screenshots
    - added desktop-related copy
    - made first startup lighter
    - added first-preview loading indicator
    - blocked preview clicks before preview is displayed
    - known issue: preview can visually jump after releasing drag; not fixed without causing unacceptable performance cost
- Working interpretation:
  - Strongest observed effect so far is the page-view jump after Store listing/screenshot work around 2026-08-23 to 2026-08-29.
  - Installs did not rise proportionally to page views in the visible charts; this makes product-page conversion, pricing/trial clarity, screenshots, review count, and search intent match the next measurement focus.
  - Install reliability does not look like the current bottleneck because install success rate is visible as `100%` and install errors show no data.
  - United States is the largest visible install market, supporting the decision to prioritize English ASO and acquisition.
  - The visible geography page does not show China or Brazil; use the next geography pages or CSV export before drawing firm conclusions about zh-CN/pt-BR performance.

### 2026-08-30 Market Page View Notes

- User-provided screenshots showed market-filtered page views:
  - Mexico + China + Brazil combined: `61` page views over the past 3 months.
  - Japan only: `302` page views over the past 3 months.
  - United States only:
    - Page views: `309`
    - Installs: `14`
    - Conversion: `4.53%`
    - Install success rate: `100%`
- User noted that Mexico/China/Brazil have had about 3 days since localization and still show `0` downloads.
- User noted that Germany/Italy/France and similar countries are probably long-tail markets, around `50` page views each.
- Visible pattern:
  - Mexico/China/Brazil were mostly near zero before the late-August localization/screenshot work, then began showing several page views per day near the end of August.
  - Japan had a steadier baseline across June-August and also rose in late August.
  - United States had `309` page views and `14` installs, making it the strongest confirmed market by both installs and conversion in the screenshots.
- Statistical caution:
  - With only `61` page views, `0` downloads is not enough to prove that those markets have structurally bad conversion.
  - If the true conversion rate were the overall visible `2.87%`, the expected downloads from `61` page views would be about `1.75`, and the probability of observing zero is still about `17%`.
  - If Japan's rough `10 installs / 302 page views` rate were used, expected downloads from `61` page views would be about `2.02`, and the probability of observing zero is still about `13%`.
  - Rule-of-three upper bound for `0/61` means the underlying conversion could still be up to roughly `4.9%` at a rough 95% upper-bound level.
- Interpretation:
  - The three localized markets are not proven failures yet; the sample is too small.
  - However, their low page-view base supports the idea that localized ASO/search exposure is still immature.
  - US conversion does not look weak in the current screenshot; `4.53%` is above the global visible `2.87%`.
  - The overall CVR problem is likely caused by market/source mix rather than the US product page alone.
  - Review display clarification from the user: public Store not showing reviews is considered normal/expected at the current low count, not an active bug. Microsoft cannot disclose the threshold for display, and it is unclear whether the threshold is global or market-specific. Current total review count is about `6`, so accumulating more reviews remains important for trust and possible ranking, but do not treat this as a Microsoft support escalation item unless new evidence appears.
  - The global CVR drop versus earlier months should not be read as proof that the older higher-price/weaker-screenshot listing converted better. Earlier months had much lower page-view volume, so one install could create a high day-level CVR.
  - The late-August PV growth likely widened exposure to lower-intent users, which can increase total page views while diluting overall CVR.
- Next measurement need:
  - Export or screenshot market-level funnel data for US, JP, MX, CN, and BR separately: page views, install attempts, successful installs, user-initiated aborts, and conversion.
  - Separate page type/source if possible: Product Display Page, Mini-PDP, Store installer, Store app/search, Store web, external URL, custom campaign.

## 2026-08-27 Update - 2.2 Store Submitted

- User submitted version `2.2.0.0` to Microsoft Store.
- Store upload package:
  - `_out\Folderly_2.2.0.0_x64_store.msix`
  - SHA-256: `B2D658D8104FDFD25D0C5F2CA14E04D34857FBD25C41BC92AE7CC90441B3142A`
  - Size: `94,007,854` bytes
  - Built at: `2026-08-27 15:55:22`
- Package identity confirmed:
  - Name: `KanekoApps.Folderly`
  - Publisher: `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`
  - Version: `2.2.0.0`
  - Resources: `en-US`, `ja-JP`, `es-ES`, `es-MX`, `pt-BR`, `zh-CN`
- MSIX content check passed:
  - `WebView2Loader.dll`
  - `coreclr.dll`
  - `hostfxr.dll`
  - `PresentationNative_cor3.dll`
- `makeappx unpack` verification passed.
- Test result before the Store package:
  - `dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"`
  - Result: `149 passed`
- Release MSBuild passed with the known `NU1702` warning only.

### Done Since 2.1

- Preview/editor performance was improved:
  - dragging and scale changes now feel lightweight and update in real time
  - the preview no longer changes overall shape while dragging
  - preview rendering is closer to the final folder icon result
  - preview folder size was increased slightly for better visibility
- Tag icon UI was improved:
  - fixed uneven/gapped icon alignment in the icon picker
  - added more tag icon options
- Startup behavior was optimized:
  - WebView2 environment initialization is now delayed/preloaded around the apply window instead of blocking ordinary app startup.
- Store screenshots were replaced by the user in Partner Center for the 2.2 submission.

### Store/Marketing Notes

- The user submitted 2.2 with updated Store screenshots. Store screenshot localization can continue gradually after this submission.
- Newly noticed strength: customized Folderly folder icons also appear on the Windows desktop. This may be useful in Store copy/screenshot updates because it shows a clearer everyday benefit than Explorer-only organization.
- The user updated the English Store listing only to include the desktop angle:
  - Short description: `Make desktop and File Explorer folders instantly recognizable with your own images.`
  - Long description opening: `Turn plain Windows folders into visual shortcuts for your desktop and File Explorer.`
  - Full long description now emphasizes custom photos/images/color tags, desktop and File Explorer organization, preview adjustment, local-only operation, no subscription, no ads, and local image storage.
  - Keyword change: `folder icon changer` -> `desktop folder icons`
- Japanese and other localized Store listings have not yet been updated for the desktop angle.

### Remaining Store Work

- After certification approval, check live Store pages:
  - JP: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=ja-JP&gl=JP`
  - EN/US: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=en-US&gl=US`
  - ES/MX: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=es-MX&gl=MX`
  - ES/ES: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=es-ES&gl=ES`
  - PT/BR: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=pt-BR&gl=BR`
  - ZH/CN: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=zh-CN&gl=CN`
- Confirm after approval:
  - version `2.2.0.0` is live
  - localized listings render without unintended English fallback
  - local prices display correctly
  - new screenshots are visible and not stale
  - public review/rating count behavior
- Future candidate work:
  - update Store text/images to mention desktop folder visibility
  - continue localized Store screenshot work
  - watch market-level `PV -> Trial -> Purchase` after the 2.1/2.2 localization and screenshot changes settle

## 2026-08-26 Update - 2.1 Store Submitted

- User submitted version `2.1.0.0` to Microsoft Store.
- Store upload package:
  - `_out\Folderly_2.1.0.0_x64_store.msix`
  - SHA-256: `F253D12D9949F8B62520C799D4B01616570F51E8F6A5F9DECF1237CDE99913D0`
  - Size: `94,001,147` bytes
  - Built at: `2026-08-26 21:41:26`
- Package identity confirmed:
  - Name: `KanekoApps.Folderly`
  - Publisher: `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`
  - Version: `2.1.0.0`
- MSIX content check passed:
  - `WebView2Loader.dll`
  - `coreclr.dll`
  - `hostfxr.dll`
  - `PresentationNative_cor3.dll`
  - `es\Folderly.resources.dll`
  - `pt-BR\Folderly.resources.dll`
  - `zh-Hans\Folderly.resources.dll`
  - `ja\Folderly.resources.dll`
- Test result before submission package:
  - `dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"`
  - Result: `148 passed`

### Store Listing Work Completed

- Partner Center recognized supported package languages:
  - English (United States)
  - Japanese (Japan)
  - Spanish (Mexico)
  - Spanish (Spain)
  - Portuguese (Brazil)
  - Chinese (China)
- Store listing text was completed for:
  - English (United States)
  - Japanese (Japan)
  - Spanish (Mexico)
  - Spanish (Spain)
  - Portuguese (Brazil)
  - Chinese (China)
- Listing copy source of truth:
  - [docs/OPERATIONS.md](OPERATIONS.md) -> `Store Listing Text`
- Recent Store listing/price commits:
  - `87c0d35` English Store listing text
  - `ffb69da` English Store keywords
  - `9c7cad0` Spanish (Mexico) Store listing text
  - `c5b43c3` Spanish (Spain) Store listing text
  - `57db686` Portuguese (Brazil) Store listing text
  - `00d54c5` Chinese (Simplified, China) Store listing text
  - `f175241` market-specific pricing

### Market-Specific Pricing

Current Partner Center draft:

| Market | Currency | Retail price |
|---|---:|---:|
| Default | USD | $2.99 |
| Japan | JPY | ¥480 |
| Mexico | MXN | $39 |
| Spain | EUR | €1.99 |
| Brazil | BRL | R$9.95 |
| China | CNY | ¥15 |

Note: China `¥15` is CNY, not JPY.

### Remaining Store Work

- User will gradually update localized Store screenshots.
- Code-side localization for the first language batch is done; do not add new app languages until data is checked.
- After certification approval, check live Store pages:
  - JP: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=ja-JP&gl=JP`
  - EN/US: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=en-US&gl=US`
  - ES/MX: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=es-MX&gl=MX`
  - ES/ES: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=es-ES&gl=ES`
  - PT/BR: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=pt-BR&gl=BR`
  - ZH/CN: `https://apps.microsoft.com/detail/9N99JH5H91H8?hl=zh-CN&gl=CN`
- Confirm after approval:
  - version `2.1.0.0` is live
  - all localized listings render without fallback to English where not intended
  - local prices display correctly
  - screenshots are not stale or inconsistent with current UI/icon
  - review/rating count behavior, especially previously reported Partner Center vs public Store mismatch

## 2026-08-25 Update - 2.1 Current

- Latest source version: `2.1.0.0` (app display `2.1`).
- Latest app commit: `a7d4d45 問い合わせフォームを多言語対応`.
- Current branch state before this handoff update: `main` is ahead of `origin/main` by local commits. Push when the user asks.
- Store candidate: `_out\Folderly_2.1.0.0_x64_store.msix`.
- Store candidate SHA-256: `59F839465DD0A05EAB38B7238702EC59888351CD83AD31252CECB55CBE690ADD`.
- Local install candidate: `_out\Folderly_2.1.0.0_x64_sideload.msix`.
- Local install candidate SHA-256: `DCE08E2981F963280BC88579523ADE145FE8E9AA71CF3E8C899BECE4E3AF7D71`.
- Installed locally: `KanekoApps.Folderly 2.1.0.0`.
- Local package was rebuilt after adding the final multilingual Tally contact form URLs, signed, verified, installed, and launched on this PC.

### Done Since 2.0

- Completed the first localization batch in app code:
  - English: `en`
  - Japanese: `ja`
  - Spanish: `es`
  - Portuguese Brazil: `pt-BR`
  - Simplified Chinese: `zh-Hans`
- Added localized `.resx` files and kept all resource key sets aligned:
  - `src/Folderly.App/Resources/Strings.es.resx`
  - `src/Folderly.App/Resources/Strings.pt-BR.resx`
  - `src/Folderly.App/Resources/Strings.zh-Hans.resx`
- Added app/package language support:
  - `LocalizationService.SupportedLanguages` includes `en`, `es`, `pt-BR`, `zh-Hans`, `ja`.
  - Japanese appears last in the language picker.
  - `system` language detection maps Spanish, Portuguese, and Simplified Chinese Windows UI cultures to the matching Folderly UI.
  - `Package.appxmanifest` declares the new language resources.
- Improved the language picker visual style so it no longer looks like an old dark native dropdown.
- Adjusted non-Japanese UI wording after review:
  - Chinese wording was made less literal in the visible Settings/Help areas.
  - Trial license display no longer shows unrealistic huge remaining days.
  - Review, purchase, help, onboarding, apply-success, warning, and icon/title tooltip strings are localized.
- Added multilingual support form routing:
  - Japanese: `https://tally.so/r/q48Bqk`
  - English: `https://tally.so/r/PdZEN0`
  - Spanish: `https://tally.so/r/1AoroW`
  - Portuguese Brazil: `https://tally.so/r/yP5l54`
  - Simplified Chinese: `https://tally.so/r/LZLdL1`
- Added/kept font fallback for non-English UI:
  - WPF/App: `Segoe UI Variable`, `Microsoft YaHei UI`, `Yu Gothic UI`, `Meiryo`.
  - Apply WebView UI uses the same fallback family.

### Validation Done

- Resource parity check for all app resource files:
  - `Strings.resx`, `Strings.ja.resx`, `Strings.es.resx`, `Strings.pt-BR.resx`, `Strings.zh-Hans.resx`
  - Result before packaging: all had matching keys, no missing keys, no extra keys, no empty values.
- Non-Japanese resource kana scan:
  - No unexpected Japanese kana in `en`, `es`, `pt-BR`, `zh-Hans` except intentional language names such as `日本語`.
- `dotnet build .\src\Folderly.App\Folderly.App.csproj -c Release`
  - Passed.
- `dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"`
  - Passed before the final Tally URL-only change.
- Release MSBuild for `Folderly.Package.wapproj`
  - Passed with the known `NU1702` warning only.
- Sideload MSIX signing:
  - `signtool verify` passed with `0` errors.
- Local install:
  - Existing `KanekoApps.Folderly` was removed, `_out\Folderly_2.1.0.0_x64_sideload.msix` was installed, and Folderly was launched.

### Manual Checks Next

- In Settings, switch language one by one:
  - English
  - Spanish
  - Portuguese Brazil
  - Simplified Chinese
  - Japanese
- For each language, check:
  - tab labels
  - Settings
  - Help
  - onboarding
  - apply window
  - purchase prompt
  - review prompt
  - warning/error text
  - support/contact link opens the correct Tally form
- For Chinese specifically, visually check whether `Microsoft YaHei UI` fallback looks acceptable on this PC.
- Check that Help -> Contact opens:
  - `es`: `https://tally.so/r/1AoroW`
  - `pt-BR`: `https://tally.so/r/yP5l54`
  - `zh-Hans`: `https://tally.so/r/LZLdL1`
- If submitting 2.1 to Store, still prepare non-code Store work:
  - localized Store title/description/keywords
  - localized first screenshot text
  - Store listing language setup in Partner Center
  - screenshot assets for `es`, `pt-BR`, and `zh-Hans`

### Localization Priority After 2.1

The first batch (`Spanish`, `Portuguese Brazil`, `Chinese Simplified`) is now implemented in code. Do not add more languages until Store metadata/screenshots and basic conversion data for these three are checked.

Next language candidates remain:

- Group 2: `Turkish`, `Korean`, `Arabic`, `Thai`.
- Group 3: `Vietnamese`, `Indonesian`, `French`, `Italian`, `Hindi`, `Chinese Traditional`.

Recommended next step: finish Store listing assets and metadata for `Spanish`, `Portuguese Brazil`, and `Chinese Simplified`, then watch market-level `PV -> Trial -> Purchase` before expanding further.

## 2026-08-25 Update

- Latest source version: `2.0.0.0` (app display `2.0`).
- Next Store candidate: `_out\Folderly_2.0.0.0_x64_store.msix`.
- Store candidate SHA-256: `DA8973ABD729C3CE8296A1B48FC0FED9B8B0E444A229C87F5726B62E189BE035`.
- Local install candidate: `_out\Folderly_2.0.0.0_x64_sideload.msix`.
- Local install candidate SHA-256: `F2612827AE35A012DF512132D4213F935D1518169DF5C9AC02EA0E6D03B2517C`.
- Installed locally: `KanekoApps.Folderly 2.0.0.0`.
- Version/package commit: `4b266d2`.
- A restore point tag exists before the preview work:
  - `before-preview-fix-20260825` -> `f85829cbd5107d1c5f86660bf8e249ddd5723dd6`

### Done Today

- Spanish localization groundwork was added:
  - This is source-level preparation only; no new MSIX has been packaged or submitted for Spanish yet.
  - `src/Folderly.App/Resources/Strings.es.resx` covers the full current app UI string set.
  - Settings language selection is now data-driven instead of fixed `system/ja/en` radio buttons.
  - `system` language detection now resolves Spanish Windows UI cultures to Spanish.
  - Explorer context menu title now supports Spanish: `Personalizar con Folderly`.
  - `Package.appxmanifest` now declares `es-ES` and `es-MX`.
  - `tests/Folderly.Tests/Resources/LocalizationResourceTests.cs` checks localized `.resx` files for missing keys, empty values, and missing `{0}` placeholders.
- Version was bumped for Store submission:
  - `src/Folderly.App/Folderly.App.csproj`: `2.0`
  - `src/Folderly.Package/Package.appxmanifest`: `2.0.0.0`
  - Release MSIX was rebuilt, packed, signed for local sideload verification, installed locally, and manifest-checked.
- Onboarding was implemented and polished:
  - first-run onboarding uses three pages: right-click a folder, choose an image/apply, manage/revert
  - onboarding is available again from Help
  - onboarding screenshots are shown without extra card frames
  - the window and image layout were widened so the second step is easier to read
- Review prompt timing was adjusted:
  - review prompts are still disabled during trial
  - purchase prompt remains trial-only
  - paid review prompt now appears on the 2nd successful verified apply instead of the 3rd
  - trial apply counts do not carry over into review prompt counts
- Image reset from the apply window was improved:
  - after `Reset image`, the user can press `Apply` when the folder already has a Folderly customization
  - reset apply uses the existing `RevertService`, so desktop.ini restoration, `_folderly` cleanup, history removal, and shell refresh stay on the established revert path
- Preview image quality was improved:
  - exact preview generation now renders at `640px` instead of `320px`
  - image resizing now uses `KnownResamplers.Lanczos3`
- Preview/final icon adjustment behavior was reworked:
  - `Center` mode can scale below `100%` again
  - empty image area is filled with the folder base color `#FFC72C`
  - `FitWidth` / `FitHeight` remain active after dragging the preview image
  - mode selection resets scale/offset only when the user explicitly changes mode
  - C# state synchronization no longer resets scale/offset during preview dragging
  - intentional clipping is now allowed even when the image is exactly fit to width/height
- Local MSIX was rebuilt, signed, installed, and launched on this PC after the latest changes.
- The user confirmed the current preview behavior feels good.

### Store Release Notes

Japanese:

```text
Folderly 2.0

プレビュー品質、初回ガイド、画像リセット、ヘルプ/設定、購入案内、レビュー依頼のタイミングを改善しました。
また、横幅最大/縦幅最大で画像位置を調整したときの挙動や、Windows側でアイコン表示が反映されにくいフォルダ向けの案内も改善しています。
```

English:

```text
Folderly 2.0

Improved preview quality, first-run onboarding, image reset/revert flow, Help/Settings, purchase guidance, and review prompt timing.
Also improved image positioning behavior in Fit Width/Fit Height modes and added clearer guidance for folders where Windows may not immediately show icon changes.
```

### Important Implementation Notes

- The edited shared rendering path is `src/Folderly.Core/Composition/ImageAdjuster.cs`.
- `ImageAdjuster` affects both preview rendering and the final `.ico` generation. Do not treat these as preview-only changes.
- Exact preview is generated in `src/Folderly.App/Views/ApplyWindow.xaml.cs` with `previewSize = 640`.
- Mode-button behavior lives in `src/Folderly.App/Resources/ApplyWindow.html`.
- Crop mode reset behavior is now explicit through `ApplyViewModel.SetCropMode(cropMode, resetPosition)`.
- Keep preview and final icon geometry aligned through `FolderTemplate.GetImageRegionPixelSize()`.

### Validation Done

- `dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"`
  - Passed: 148 tests.
- `dotnet build .\src\Folderly.App\Folderly.App.csproj -c Release`
  - Passed.
- Release MSBuild for `Folderly.Package.wapproj`
  - Passed with the known NU1702 warning only.
- Store MSIX was built.
- Sideload MSIX was signed, verified, installed locally, and launched.
- Store MSIX manifest was unpacked and checked:
  - Identity: `KanekoApps.Folderly`
  - Version: `2.0.0.0`
  - Required root files present: `WebView2Loader.dll`, `coreclr.dll`, `hostfxr.dll`, `PresentationNative_cor3.dll`, `Folderly.exe`

### Manual Checks Before Store Submission

- Test at least three image shapes:
  - wide image
  - tall image
  - square image
- For each, check:
  - `Center`
  - `FitWidth` / `横幅最大`
  - `FitHeight` / `縦幅最大`
  - drag after selecting `FitWidth` / `FitHeight`
  - scale below `100%`
  - intentional clipping beyond the visible region
  - folder-color margin when image does not fill the whole image region
- Apply the icon and confirm Explorer's actual folder icon matches the preview closely.
- For an already customized folder, click `Reset image`, confirm `Apply` remains enabled, apply it, and confirm the folder reverts to its original appearance.
- Scope that should be low-risk but still worth a smoke check:
  - apply
  - revert
  - history/reapply
  - right-click context menu launch

### Localization Priority

Goal: increase CVR by adding languages that cover large groups of users who mainly read that language and are less likely to buy from an English-only Store page. Do not over-weight speculative Microsoft Store search demand or Folderly-specific search demand; those are hard to measure externally.

Recommended first localization batch:

1. `Chinese Simplified`
2. `Spanish`
3. `Portuguese Brazil`

Interpretation:

- If choosing purely by "large non-English audience + Windows usage", start with `Chinese Simplified`.
- If choosing the safest first language, start with `Spanish` because it covers a large language area with lower market/distribution uncertainty than mainland China.
- `Portuguese Brazil` remains highly attractive because Brazil is a large single-language market with relatively low English proficiency and strong Windows desktop usage.

Suggested implementation order:

- Aggressive: `Chinese Simplified` -> `Spanish` -> `Portuguese Brazil`.
- Conservative: `Spanish` -> `Portuguese Brazil` -> `Chinese Simplified`.

Next language groups:

- Group 2: `Turkish`, `Korean`, `Arabic`, `Thai`.
- Group 3: `Vietnamese`, `Indonesian`, `French`, `Italian`, `Hindi`, `Chinese Traditional`.
- Lower priority for this specific goal: `German`, `Polish`, `Ukrainian`, `Dutch`, `Swedish`, `Russian`.

Rationale:

- `German`, `Polish`, `Dutch`, and `Swedish` are commercially strong markets, but English proficiency is high, so English Store pages likely already capture more of the audience.
- `Arabic` and `Thai` may have large English-barrier gains, but Arabic requires RTL/UI/screenshot QA and Thai has a smaller market than the first batch.
- `Hindi` has huge speaker counts, but the overlap between Hindi-primary, low-English users and users likely to buy a paid Windows utility through Microsoft Store is uncertain.

For each new language, ideally localize at least:

- app UI strings
- Store title, short description, description, keywords
- first Store screenshot text
- FAQ/support copy
- purchase/review/support prompts

App implementation path for the next languages:

- Add `Strings.pt-BR.resx` or `Strings.zh-Hans.resx` with the same keys as `Strings.resx`.
- Add one `LocalizationService.SupportedLanguages` entry with the target code, culture name, display-name key, and context menu title.
- Add the matching `<Resource Language="pt-BR" />` or `<Resource Language="zh-Hans" />` in `Package.appxmanifest`.
- Run the localization resource tests before packaging. They intentionally fail if even one key or placeholder is missing.
- Do not expose a new language in `SupportedLanguages` until its `.resx`, Store listing copy, and first Store screenshot text are ready enough to avoid a half-localized experience.

After each language launch, compare Partner Center market-level `PV -> Trial -> Purchase` before adding many more languages.

## 2026-08-21 Update

- Latest source version: `1.6.12.0` (app display `1.6.12`).
- Next Store candidate: `_out\Folderly_1.6.12.0_x64_store.msix`.
- Store candidate SHA-256: `07C66B79BC358DDC771BE89707EF0C0E40CD838B1768A58EB923C7670EC38949`.
- Local install candidate: `_out\Folderly_1.6.12.0_x64_sideload.msix`.
- Installed locally: `KanekoApps.Folderly 1.6.12.0`.
- Latest pushed commit: `0c304b0`.
- Empty preview card now opens the image picker when clicked. After an image is selected, the same card remains dedicated to drag position adjustment.
- Image picker now starts in the target folder when that folder exists.
- Review prompt is now shown on the 2nd successful apply, 3 seconds after the Explorer reopen flow, with a foreground owner so it does not appear behind Explorer.
- Review prompt now uses a branded Folderly dialog with the app icon and clear Store rating / Later actions instead of the default system MessageBox.
- Review prompt primary button now uses a lighter blue background so black system button text remains readable.
- Review prompt can now be dragged by the card surface while keeping button clicks unaffected.

### Submit/Check Notes

- This build is OK to submit if the user confirms the review prompt visually one last time.
- Main manual checks before/after submission:
  - review prompt appears after the 2nd successful apply, after Explorer reopen + about 3 seconds
  - prompt appears in front of Explorer
  - `Storeで評価` / `Rate` button text is readable on the light blue background
  - card surface can be dragged
  - `Storeで評価` / `Rate` and `あとで` / `Later` clicks are not affected by drag handling
- Review prompt test state was reset locally to `review_prompt.apply_count=1`, so the next successful apply should show the prompt.
- Validation done:
  - `dotnet test .\tests\Folderly.Tests\Folderly.Tests.csproj --filter "FullyQualifiedName!~CheckPath_NoWriteAccess_IsDenied"` passed: 138 tests.
  - Release MSBuild for `Folderly.Package.wapproj` passed with the known NU1702 warning only.
  - Store MSIX was built.
  - Sideload MSIX was signed, verified, and installed locally.

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
4. Public rating/review display may remain hidden until Microsoft Store's undisclosed review-count threshold is met.
5. Review replies are visible if Microsoft exposes them.
6. In-app Help opens the Help tab, not an external page or dialog.
7. Japanese language opens `https://tally.so/r/q48Bqk`.
8. English language opens `https://tally.so/r/PdZEN0`.
9. Tag edit dialog no longer shows a disabled Delete button.
10. Primary button background is light enough to read comfortably.

### Review Display Status

- Previous support case: `2607300030003368`.
- Current understanding from the user:
  - The public Store review/rating display is normal at the current low review count, not a confirmed bug.
  - Microsoft does not disclose how many reviews are required before public display appears.
  - Microsoft also does not clarify whether the threshold is global or market-specific.
  - Current total review count is about `6`.
- Strategy implication:
  - Do not spend more effort treating review display as a support-blocked issue unless new contradictory evidence appears.
  - Keep improving legitimate review acquisition after successful paid use, because if the threshold is market-specific, visible reviews may take longer to appear in each market.

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
- Watch acquisition/review metrics for the sale period through `2026-09-02`.
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

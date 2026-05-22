# Microsoft Store Submission Notes

Last updated: 2026-05-23

This document is the handover for the current Microsoft Store submission state.

## Current Store Package

- Product: `Folderly`
- Package identity name: `KanekoApps.Folderly`
- Publisher: `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`
- Publisher display name: `Kaneko Apps`
- Package family name: `KanekoApps.Folderly_q8156m1pgwn5a`
- Store ID: `9N99JH5H91H8`
- Store URL: `https://apps.microsoft.com/detail/9N99JH5H91H8`
- Store protocol link: `ms-windows-store://pdp/?productid=9N99JH5H91H8`
- Store package version: `1.0.16.0`
- Architecture: `x64`
- Minimum OS: Windows 10 1809 (`10.0.17763.0`)
- Restricted capability: `runFullTrust`
- Uploaded Store candidate package: `_out/Folderly_1.0.16.0_x64_store.msix`

## Version Rule

Microsoft Store rejects MSIX packages with a non-zero revision number.

- Do not submit `1.0.0.16`.
- Use `1.0.16.0`.
- The rejected package was `_out/Folderly_1.0.0.16_x64_store.msix`.

## Package Creation

Visual Studio 2022 and 2026 did not show `Publish`, `Store`, or `Create App Packages` for `Folderly.Package` in this environment.

Use the manual MakeAppx flow when needed:

1. Build `src/Folderly.Package/Folderly.Package.wapproj` in `Release|x64`.
2. Stage `src/Folderly.Package/bin/x64/Release`.
3. Copy `src/Folderly.Package/Package.appxmanifest` as `AppxManifest.xml`.
4. Copy `src/Folderly.Package/Images`.
5. Run `makeappx pack`.
6. Upload the resulting `.msix` to Partner Center.

Partner Center accepted this package:

```text
_out/Folderly_1.0.16.0_x64_store.msix
```

## Partner Center Properties

Category:

- Primary category: `Utilities & tools`
- Subcategory: file manager, system tools, or other tools if available
- Secondary category: none

Privacy:

- The Store requires a privacy policy because of declared capabilities.
- Privacy Policy URL: `https://sknk-aaa.github.io/folderly/`
- Support URL: `https://sknk-aaa.github.io/folderly/#support`

Support:

- Website: `https://sknk-aaa.github.io/folderly/#support`
- Support contact: support email address
- Phone/address: Partner Center may show account contact info if these are blank.

Device family:

- Select `Windows 10/11 Desktop` only.
- Do not select Mobile, Xbox, Team, Mixed Reality, Windows 8/8.1, or Phone.
- `Let Microsoft decide whether to make this app available to any future device families` can remain checked.

Pricing:

- Recommended launch price: 300 JPY buyout.
- Store trial: 7 days.
- No IAP/subscription for the initial release.

Age ratings:

- Folderly is a local desktop utility.
- It has no user-generated content sharing, web publishing, social features, gambling, or mature content.
- Answer the age-rating questionnaire accordingly.

Mixed Reality:

- Folderly is not a Mixed Reality app.
- Leave Mixed Reality-specific fields blank if possible.
- If forced to choose a display mode, choose the seated/standing option.

## runFullTrust Justification

Paste this into the restricted capability explanation field:

```text
Folderly is a Windows desktop utility for customizing folder appearance. The app uses the runFullTrust capability because it needs to run as a full-trust desktop app in order to integrate with Windows Explorer, provide a folder context menu entry, apply folder icon customization, write desktop.ini/icon files inside user-selected folders, and refresh Explorer so the updated folder appearance is visible.

Folderly only operates on folders explicitly selected by the user. It does not collect, transmit, sell, or share personal information. Settings, customization history, selected local images, and generated icon files are stored locally on the user's device.
```

## Current Submission Status

Done:

- Official Partner Center identity confirmed.
- `Package.appxmanifest` identity updated.
- Store package version fixed to `1.0.16.0`.
- Store candidate MSIX generated.
- Partner Center package upload completed successfully.
- `Windows 10/11 Desktop` device family selected.

Still required in Partner Center:

- Confirm GitHub Pages privacy/support URLs are public.
- Finish Store listing text and screenshots.
- Finish price/trial/market settings.
- Finish age rating.
- Fill the `runFullTrust` justification.
- Submit for certification.

## Related Docs

- Store listing draft: [STORE_LISTING_DRAFT.md](STORE_LISTING_DRAFT.md)
- Privacy/support page: [index.html](index.html)
- Privacy policy draft: [PRIVACY_POLICY_DRAFT.md](PRIVACY_POLICY_DRAFT.md)
- Test checklist: [TESTING.md](TESTING.md)

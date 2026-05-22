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
- Latest Store candidate package: `_out/Folderly_1.0.16.0_x64_store.msix`

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

Partner Center accepted this package format:

```text
_out/Folderly_1.0.16.0_x64_store.msix
```

After any code fix, rebuild this file and replace the package in Partner Center. Do not upload `_out/Folderly_1.0.16.0_x64_sideload.msix`; that file is only for local testing.

## Local Sideload Verification

The Store package and local test package are intentionally different files:

- Store upload: `_out/Folderly_1.0.16.0_x64_store.msix`
- Local install: `_out/Folderly_1.0.16.0_x64_sideload.msix`

Why:

- Partner Center expects the Store package identity and signs the Store package during ingestion.
- Local Windows installation requires a trusted signature before `Add-AppxPackage` can install the MSIX.
- The signing certificate subject must match the manifest publisher exactly:
  `CN=F27FAE8B-A689-44D3-AB88-09E593D2DA9E`.

Known failure modes:

- If `signtool verify` succeeds but `Add-AppxPackage` fails with `0x800B0109`, import the sideload certificate into `Cert:\LocalMachine\Root` and `Cert:\LocalMachine\TrustedPeople`. Current-user trust can be insufficient for AppX deployment.
- If the UI still behaves like an older build, check `Get-AppxPackage *Folderly*` and `Get-Process Folderly`. The old package `Folderly.FolderlyApp_1.0.0.16_x64__n6y34gfnxsf8c` used publisher `CN=Folderly` and can remain installed/running separately from `KanekoApps.Folderly`.
- Folderly has single-instance IPC. If an old process is still running, launching a new executable can forward the request to the old instance. Stop `Folderly.exe` before reinstalling.

Reusable sideload script:

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
Get-AppxPackage -Name KanekoApps.Folderly
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
- Partner Center package upload completed successfully for an earlier candidate.
- `Windows 10/11 Desktop` device family selected.
- Local sideload verification flow confirmed with the Store identity.

Still required in Partner Center:

- Replace the uploaded package with the latest regenerated `_out/Folderly_1.0.16.0_x64_store.msix` after the preview/crop-mode fix.
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

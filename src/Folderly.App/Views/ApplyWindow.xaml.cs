using Folderly.App.Infrastructure;
using Folderly.App.Services;
using Folderly.App.ViewModels;
using Folderly.Core.Application;
using Folderly.Core.Composition;
using Folderly.Core.Folder;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using CoreCropMode = Folderly.Core.Composition.CropMode;

namespace Folderly.App.Views;

public partial class ApplyWindow : Window
{
    private readonly ApplyViewModel _vm;
    private bool _webViewReady;
    private static readonly Dictionary<string, string> CachedHtmlByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private int _previewRenderVersion;
    private bool _previewRenderActive;
    private bool _previewRenderPending;
    private bool _previewRenderPendingExact;

    public ApplyWindow(string folderPath)
    {
        InitializeComponent();
        _vm = new ApplyViewModel(folderPath);
        TryRestoreExistingCustomization();

        Loaded += async (_, _) => await InitWebViewAsync();
    }

    // ─── WebView2 初期化 ────────────────────────────────────────────────────

    private async Task InitWebViewAsync()
    {
        try
        {
            // AppServices.Initialize() で並列開始済みの Environment を使い回す
            var env = await (AppServices.WebView2EnvTask
                ?? CoreWebView2Environment.CreateAsync(null, Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Folderly", "WebView2")));
            await WebView.EnsureCoreWebView2Async(env);

            WebView.CoreWebView2.Settings.IsNonClientRegionSupportEnabled = true;
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled   = false;
            WebView.CoreWebView2.Settings.AreDevToolsEnabled              = false;

            WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            var html = LoadHtml();
            WebView.NavigateToString(html);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"WebView2 の初期化に失敗しました。\n{ex.Message}",
                "Folderly", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private static string LoadHtml()
    {
        var language = AppServices.Localize.CurrentLang;
        if (CachedHtmlByLanguage.TryGetValue(language, out var cachedHtml))
            return cachedHtml;

        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("Folderly.App.Resources.ApplyWindow.html");
        if (stream is null) throw new InvalidOperationException("ApplyWindow.html が見つかりません");
        using var reader = new StreamReader(stream);
        var html = LocalizeHtml(reader.ReadToEnd());
        CachedHtmlByLanguage[language] = html;
        return html;
    }

    private static string LocalizeHtml(string html)
    {
        var L = AppServices.Localize;
        static string Html(string value) => WebUtility.HtmlEncode(value);
        string T(string key) => Html(L[key]);

        var replacements = new (string From, string To)[]
        {
            ("対象フォルダ", T("TargetFolder")),
            ("フォルダプレビュー", T("FolderPreviewTitle")),
            ("画像を選択するとここに表示されます", T("PreviewSubtext")),
            ("画像をドラッグ&ドロップ", T("DndSubtext")),
            ("画像を選択してください", T("SelectImageTitle")),
            ("または画像をここにドラッグ&amp;ドロップ", T("DndSubtext")),
            ("画像をリセット", T("ResetImage")),
            ("画像の調整", T("ImageAdjustSection")),
            ("拡大率", T("ScaleLabel")),
            ("中央に戻す", T("ResetPosition")),
            ("表示モード", T("DisplayModeLabel")),
            ("余白なし", T("CropCenter")),
            ("横幅最大", T("CropFitWidth")),
            ("縦幅最大", T("CropFitHeight")),
            ("位置調整", T("PositionAdjustment")),
            ("X 位置", T("XPosition")),
            ("Y 位置", T("YPosition")),
            ("左に移動", T("MoveLeft")),
            ("右に移動", T("MoveRight")),
            ("上に移動", T("MoveUp")),
            ("下に移動", T("MoveDown")),
            ("タグの選択", T("TagSelectTitle")),
            ("フォルダの種類を色で識別できます", T("TagSelectDesc")),
            ("タグを編集", T("TagEditTitle")),
            ("ヒント", T("HintTitle")),
            ("プレビューの枠内が実際に表示される範囲です", T("HintPreviewRange")),
            ("キャンセル", T("Cancel")),
            ("適用", T("Apply")),
            ("戻る", T("Back")),
            ("タグ名とアイコンをカスタマイズできます", T("TagEditDesc")),
            ("タグ一覧", T("TagListTitle")),
            ("クリックして編集", T("ClickToEdit")),
            ("新規タグを追加", T("NewTagBtn")),
            ("タグの編集", T("TagEditHeadTitle")),
            ("プレビュー — フォルダの左上タブに表示されます", T("PreviewOnFolderTab")),
            ("フォルダアイコン上にタグ名を表示", T("ShowTagNameOnIcon")),
            ("オフにすると、左上タブにアイコンと色のみが表示されます", T("ShowTagNameOffNote")),
            ("フォルダアイコン上にアイコンを表示", T("ShowTagIconOnIcon")),
            ("タグごとに選んだアイコンを左上タブに表示します", T("ShowTagIconOnIconNote")),
            ("タグ名を入力", T("TagNamePlaceholder")),
            ("タグ名", T("TagNameLabel")),
            ("カラー", T("TagColorLabel")),
            ("アイコン", T("TagIconLabel")),
            ("変更は「保存」を押すまで反映されません", T("SaveChangesHint")),
            ("保存", T("Save")),
            ("編集", T("IconEdit")),
            ("メディア", T("IconMedia")),
            ("仕事", T("IconWork")),
            ("ドキュメント", T("IconDocument")),
            ("ダウンロード", T("IconDownload")),
            ("その他", T("IconOther")),
            ("写真", T("IconPhoto")),
            ("音楽", T("IconMusic")),
            ("ゲーム", T("IconGame")),
            ("学習", T("IconStudy")),
            ("デザイン", T("IconDesign")),
            ("重要", T("IconImportant")),
            ("プライベート", T("IconPrivate")),
        };

        foreach (var (from, to) in replacements)
            html = html.Replace(from, to, StringComparison.Ordinal);

        if (AppServices.Localize.CurrentLang == "ja")
            return html;

        html = Regex.Replace(
            html,
            @"<div class=""editing""><b id=""editing-tag-name"">.*?</b>\s*.*?</div>",
            $"<div class=\"editing\">{T("EditingSuffix")} <b id=\"editing-tag-name\">&quot;—&quot;</b></div>",
            RegexOptions.Singleline);
        html = Regex.Replace(
            html,
            @"headEl\.textContent\s*=\s*'[^']*'\s*\+\s*data\.name\s*\+\s*'[^']*';",
            "headEl.textContent = '\"' + data.name + '\"';");
        html = Regex.Replace(
            html,
            @"headEl\.textContent\s*=\s*'[^']*'\s*\+\s*nameInput\.value\s*\+\s*'[^']*';",
            "headEl.textContent = '\"' + nameInput.value + '\"';");

        var applyButtonHtml = "<span class=\"ico\"><svg width=\"14\" height=\"14\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polyline points=\"4,12 10,18 20,6\"/></svg></span>"
            + Html(L["Apply"]);
        html = html.Replace(
            $"btn.textContent = '{Html(L["Apply"])}中...';",
            $"btn.textContent = {JsonSerializer.Serialize(L["Applying"])};",
            StringComparison.Ordinal);
        html = html.Replace(
            $"btn.innerHTML = '<span class=\"ico\"><svg width=\"14\" height=\"14\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polyline points=\"4,12 10,18 20,6\"/></svg></span>{Html(L["Apply"])}';",
            $"btn.innerHTML = {JsonSerializer.Serialize(applyButtonHtml)};",
            StringComparison.Ordinal);

        return html;
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // 初回ナビゲーション完了後のみ状態を送信
        if (!_webViewReady)
        {
            _webViewReady = true;
            await SendStateAsync();
            await SendPreviewAsync();
        }
    }

    // ─── JS → C# メッセージ受信 ──────────────────────────────────────────────

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var raw = e.TryGetWebMessageAsString();
        if (string.IsNullOrEmpty(raw)) return;

        Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                var type = root.GetProperty("type").GetString();

                switch (type)
                {
                    case "ready":
                        await SendStateAsync();
                        await SendPreviewAsync();
                        break;

                    case "selectImage":
                        SelectImageFromDialog();
                        break;

                    case "dropImage":
                        await HandleDropImageAsync(root);
                        break;

                    case "scale":
                        _vm.Scale = root.GetProperty("value").GetDouble();
                        await SendPreviewAsync();
                        break;

                    case "offsetX":
                        _vm.OffsetX = root.GetProperty("value").GetDouble();
                        await SendPreviewAsync();
                        break;

                    case "offsetY":
                        _vm.OffsetY = root.GetProperty("value").GetDouble();
                        await SendPreviewAsync();
                        break;

                    case "offset":
                        _vm.OffsetX = root.GetProperty("offsetX").GetDouble();
                        _vm.OffsetY = root.GetProperty("offsetY").GetDouble();
                        await SendPreviewAsync();
                        break;

                    case "offsetPreview":
                        _vm.OffsetX = root.GetProperty("offsetX").GetDouble();
                        _vm.OffsetY = root.GetProperty("offsetY").GetDouble();
                        await SendPreviewAsync(exact: false);
                        break;

                    case "transform":
                        UpdateTransform(root);
                        await SendPreviewAsync();
                        break;

                    case "transformPreview":
                        UpdateTransform(root);
                        await SendPreviewAsync(exact: false);
                        break;

                    case "cropMode":
                        var modeStr = root.GetProperty("mode").GetString() ?? "Center";
                        _vm.CropMode = modeStr switch
                        {
                            "FitWidth"  => CoreCropMode.FitWidth,
                            "FitHeight" => CoreCropMode.FitHeight,
                            _           => CoreCropMode.Center,
                        };
                        await SendPreviewAsync();
                        break;

                    case "resetPosition":
                        _vm.ResetPosition();
                        await SendStateAsync();
                        await SendPreviewAsync();
                        break;

                    case "resetImage":
                        _vm.SourceImage = null;
                        _vm.SourceImagePath = string.Empty;
                        _vm.ResetPosition();
                        await SendStateAsync();
                        await SendPreviewAsync();
                        break;

                    case "selectTag":
                        var key = root.GetProperty("key").GetString() ?? "none";
                        _vm.SelectedTagColor = TagColors.All.FirstOrDefault(t => t.Key == key)
                                               ?? TagColors.None;
                        await SendPreviewAsync();
                        break;

                    case "saveTagSettings":
                        await SaveTagSettingsAsync(root.GetProperty("data"));
                        break;

                    case "apply":
                        await ApplyAsync();
                        break;

                    case "cancel":
                        Close();
                        break;

                    case "minimize":
                        WindowState = WindowState.Minimized;
                        break;
                }
            }
            catch { /* JSON parse 失敗などは無視 */ }
        });
    }

    // ─── C# → JS: 状態送信 ──────────────────────────────────────────────────

    private async Task SendStateAsync()
    {
        if (!_webViewReady) return;

        var tags = TagColors.All
            .Where(t => !t.IsNone)
            .Select(t => new
            {
                key       = t.Key,
                name      = TagSettingsService.GetDisplayName(t),
                hexColor  = TagSettingsService.GetTagHexColor(t) ?? t.HexColor ?? "#888888",
                iconIndex = TagSettingsService.GetTagIconIndex(t),
            })
            .ToList();

        var state = new
        {
            folderPath        = _vm.FolderPath,
            selectedTagKey    = _vm.SelectedTagColor?.IsNone == true ? "none" : (_vm.SelectedTagColor?.Key ?? "none"),
            scale             = _vm.Scale,
            offsetX           = _vm.OffsetX,
            offsetY           = _vm.OffsetY,
            cropMode          = _vm.CropMode.ToString(),
            showTagNameOnIcon = TagSettingsService.GetShowTagNameOnIcon(),
            showTagIconOnIcon = TagSettingsService.GetShowTagIconOnIcon(),
            tags,
        };

        var json = JsonSerializer.Serialize(state);
        await ExecuteScriptSafeAsync($"window.folderlySetState({json})");
    }

    private async Task SendPreviewAsync(bool exact = true)
    {
        if (!_webViewReady) return;

        _previewRenderVersion++;
        _previewRenderPending = true;
        _previewRenderPendingExact |= exact;

        if (_previewRenderActive) return;

        _previewRenderActive = true;

        try
        {
            while (_previewRenderPending)
            {
                var renderVersion = _previewRenderVersion;
                var renderExact = _previewRenderPendingExact;
                _previewRenderPending = false;
                _previewRenderPendingExact = false;

                await RenderAndSendPreviewAsync(renderExact, renderVersion);
            }
        }
        finally
        {
            _previewRenderActive = false;
        }

        if (_previewRenderPending)
            await SendPreviewAsync(exact: false);
    }

    private async Task RenderAndSendPreviewAsync(bool exact, int renderVersion)
    {
        if (!_webViewReady) return;

        // OffscreenPreview プロパティを現在の ViewModel 状態に同期
        if (_vm.SourceImage is null)
        {
            await ExecuteScriptSafeAsync("window.folderlyClearPreview && window.folderlyClearPreview()");
            return;
        }

        // WPF レイアウトを強制更新してからレンダリング
        var pngBytes = exact
            ? await RenderExactPreviewPngAsync()
            : RenderFastPreviewPng();
        if (renderVersion != _previewRenderVersion && _previewRenderPending) return;

        var b64     = Convert.ToBase64String(pngBytes);
        var dataUrl = $"data:image/png;base64,{b64}";

        await ExecuteScriptSafeAsync($"window.folderlySetPreview('{dataUrl}')");
    }

    private byte[] RenderFastPreviewPng()
    {
        OffscreenPreview.SourceImage       = _vm.SourceImage;
        OffscreenPreview.SelectedTagColor  = _vm.EffectiveSelectedTagColor;
        OffscreenPreview.TagName           = TagSettingsService.GetDisplayName(_vm.SelectedTagColor);
        OffscreenPreview.ShowTagNameOnIcon = TagSettingsService.GetShowTagNameOnIcon();
        OffscreenPreview.TagIconIndex      = TagSettingsService.GetTagIconIndex(_vm.SelectedTagColor);
        OffscreenPreview.ShowTagIconOnIcon = TagSettingsService.GetShowTagIconOnIcon();
        OffscreenPreview.Scale             = _vm.Scale;
        OffscreenPreview.OffsetX           = _vm.OffsetX;
        OffscreenPreview.OffsetY           = _vm.OffsetY;
        OffscreenPreview.CropMode          = _vm.CropMode;

        OffscreenPreview.Measure(new System.Windows.Size(320, 320));
        OffscreenPreview.Arrange(new Rect(0, 0, 320, 320));
        OffscreenPreview.UpdateLayout();

        var rtb = new RenderTargetBitmap(320, 320, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(OffscreenPreview);

        using var ms = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        encoder.Save(ms);
        return ms.ToArray();
    }

    private async Task<byte[]> RenderExactPreviewPngAsync()
    {
        using var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_vm.SourceImage!));
        encoder.Save(stream);
        stream.Position = 0;

        using var sourceImage = await SixLabors.ImageSharp.Image.LoadAsync(stream);
        using var adjustedImage = ImageAdjuster.Adjust(
            sourceImage,
            FolderTemplate.GetImageRegionPixelSize(),
            _vm.GetAdjustParams());

        var tagNameForIcon = TagSettingsService.GetShowTagNameOnIcon()
            ? TagSettingsService.GetDisplayName(_vm.SelectedTagColor)
            : null;

        using var composed = TemplateRenderer.Render(
            adjustedImage,
            _vm.EffectiveSelectedTagColor,
            FolderTemplate.BaseSize,
            tagNameForIcon,
            TagSettingsService.GetTagIconIndex(_vm.SelectedTagColor),
            TagSettingsService.GetShowTagIconOnIcon());

        composed.Mutate(ctx => ctx.Resize(320, 320));

        using var ms = new MemoryStream();
        composed.SaveAsPng(ms);
        return ms.ToArray();
    }

    private void UpdateTransform(JsonElement root)
    {
        if (root.TryGetProperty("scale", out var scale))
            _vm.Scale = scale.GetDouble();
        if (root.TryGetProperty("offsetX", out var offsetX))
            _vm.OffsetX = offsetX.GetDouble();
        if (root.TryGetProperty("offsetY", out var offsetY))
            _vm.OffsetY = offsetY.GetDouble();
    }

    private async Task SendTagDataAsync()
    {
        if (!_webViewReady) return;

        var tags = TagColors.All
            .Where(t => !t.IsNone)
            .Select(t => new
            {
                key       = t.Key,
                name      = TagSettingsService.GetDisplayName(t),
                hexColor  = TagSettingsService.GetTagHexColor(t) ?? t.HexColor ?? "#888888",
                iconIndex = TagSettingsService.GetTagIconIndex(t),
            })
            .ToList();

        var json = JsonSerializer.Serialize(tags);
        await ExecuteScriptSafeAsync($"window.folderlyUpdateTags({json})");
    }

    private async Task ExecuteScriptSafeAsync(string script)
    {
        try
        {
            await WebView.ExecuteScriptAsync(script);
        }
        catch { /* WebView2 が閉じられた後などは無視 */ }
    }

    // ─── 画像選択・ロード ───────────────────────────────────────────────────

    private void TryRestoreExistingCustomization()
    {
        try
        {
            var entry = AppServices.History.GetByPath(Path.GetFullPath(_vm.FolderPath));
            if (entry is null) return;
            if (string.IsNullOrWhiteSpace(entry.SourceImagePath)) return;
            if (!File.Exists(entry.SourceImagePath)) return;
            if (!LoadImage(entry.SourceImagePath, resetPosition: false, showError: false)) return;

            _vm.CropMode = entry.CropMode switch
            {
                "fit_width"  => CoreCropMode.FitWidth,
                "fit_height" => CoreCropMode.FitHeight,
                _            => CoreCropMode.Center,
            };
            _vm.Scale   = entry.ImageScale;
            _vm.OffsetX = entry.ImageOffsetX;
            _vm.OffsetY = entry.ImageOffsetY;
            _vm.SelectedTagColor = !string.IsNullOrWhiteSpace(entry.TagKey)
                ? TagColors.All.FirstOrDefault(t => t.Key == entry.TagKey) ?? TagColors.None
                : TagColors.None;
        }
        catch
        {
            // 履歴復元に失敗しても、通常の新規カスタマイズ画面として開ければよい。
        }
    }

    private void SelectImageFromDialog()
    {
        var dlg = new OpenFileDialog
        {
            Title  = AppServices.Localize["SelectImage"],
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All Files|*.*",
        };
        if (dlg.ShowDialog(this) != true) return;
        LoadImage(dlg.FileName);
    }

    private async Task HandleDropImageAsync(JsonElement root)
    {
        var dataUrl = root.GetProperty("dataUrl").GetString();
        if (string.IsNullOrEmpty(dataUrl)) return;

        // data:image/png;base64,xxxx から bytes を取得
        var commaIdx = dataUrl.IndexOf(',');
        if (commaIdx < 0) return;
        var bytes = Convert.FromBase64String(dataUrl.Substring(commaIdx + 1));

        try
        {
            var bitmap = new BitmapImage();
            using var ms = new MemoryStream(bytes);
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption  = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            _vm.SourceImage     = bitmap;
            _vm.SourceImagePath = string.Empty;
            _vm.ResetPosition();
            await SendPreviewAsync();
        }
        catch
        {
            MessageBox.Show(AppServices.Localize["ImageLoadError"], "Folderly",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool LoadImage(string path, bool resetPosition = true, bool showError = true)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource     = new Uri(path);
            bitmap.CacheOption   = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
            bitmap.Freeze();

            _vm.SourceImage     = bitmap;
            _vm.SourceImagePath = path;
            if (resetPosition)
                _vm.ResetPosition();

            _ = SendPreviewAsync();
            return true;
        }
        catch
        {
            if (showError)
            {
                MessageBox.Show(AppServices.Localize["ImageLoadError"], "Folderly",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return false;
        }
    }

    // ─── タグ設定保存 ───────────────────────────────────────────────────────

    private async Task SaveTagSettingsAsync(JsonElement data)
    {
        if (data.TryGetProperty("tags", out var tagsEl))
        {
            foreach (var t in tagsEl.EnumerateArray())
            {
                var tagKey    = t.GetProperty("key").GetString() ?? string.Empty;
                var name      = t.GetProperty("name").GetString() ?? string.Empty;
                var hexColor  = t.GetProperty("hexColor").GetString() ?? string.Empty;
                var iconIndex = t.GetProperty("iconIndex").GetInt32();

                var tagColor = TagColors.All.FirstOrDefault(tc => tc.Key == tagKey);
                if (tagColor is null || tagColor.IsNone) continue;

                TagSettingsService.SetDisplayName(tagColor, name);
                TagSettingsService.SetTagHexColor(tagColor, hexColor);
                TagSettingsService.SetTagIconIndex(tagColor, iconIndex);
            }
        }

        if (data.TryGetProperty("showTagNameOnIcon", out var showEl))
            TagSettingsService.SetShowTagNameOnIcon(showEl.GetBoolean());

        if (data.TryGetProperty("showTagIconOnIcon", out var showIconEl))
            TagSettingsService.SetShowTagIconOnIcon(showIconEl.GetBoolean());

        _vm.RefreshTagSettings();
        await SendTagDataAsync();
        await SendPreviewAsync();
    }

    // ─── 適用 ────────────────────────────────────────────────────────────────

    private async Task ApplyAsync()
    {
        if (!_vm.CanApply) return;

        var protection = FolderProtection.CheckPath(_vm.FolderPath);
        if (protection.IsWarning)
        {
            var L   = AppServices.Localize;
            var msg = IsOneDrivePath(_vm.FolderPath)
                ? L["OneDriveWarningMessage"]
                : string.Format(L["WarningGenericMessage"], protection.Reason);
            var res = MessageBox.Show(msg, L["WarningGenericTitle"],
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (res != MessageBoxResult.OK) return;
        }

        _vm.IsApplying = true;
        await ExecuteScriptSafeAsync("window.folderlySetApplying(true)");

        try
        {
            using var stream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(_vm.SourceImage!));
            encoder.Save(stream);
            stream.Position = 0;

            var request = new ApplyRequest(
                FolderPath:        _vm.FolderPath,
                SourceImageStream: stream,
                SourceImagePath:   _vm.SourceImagePath ?? string.Empty,
                AdjustParams:      _vm.GetAdjustParams(),
                TagColor:          _vm.EffectiveSelectedTagColor,
                ForceApply:        false,
                TagName:           TagSettingsService.GetDisplayName(_vm.SelectedTagColor),
                ShowTagNameOnIcon: TagSettingsService.GetShowTagNameOnIcon(),
                TagIconIndex:      TagSettingsService.GetTagIconIndex(_vm.SelectedTagColor),
                ShowTagIconOnIcon: TagSettingsService.GetShowTagIconOnIcon());

            var result = await AppServices.Apply.ApplyAsync(request);

            if (result.IsWarning)
                result = await AppServices.Apply.ApplyAsync(request with { ForceApply = true });

            if (result.IsSuccess)
            {
                await ExecuteScriptSafeAsync(
                    $"document.getElementById('btn-apply').textContent={JsonSerializer.Serialize("✓ " + AppServices.Localize["ApplyCompleted"])};");

                Hide();
                if (ShouldReopenExplorer())
                    await ReopenExplorerWindowsAsync(_vm.FolderPath);
                Close();
                return;
            }
        }
        catch (FolderProtectionException ex)
        {
            MessageBox.Show(ex.Message, AppServices.Localize["ProtectionDeniedTitle"],
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(AppServices.Localize["ApplyFailed"], ex.Message),
                "Folderly", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _vm.IsApplying = false;
            await ExecuteScriptSafeAsync("window.folderlySetApplying(false)");
        }
    }

    // ─── Explorer 再起動 ─────────────────────────────────────────────────────

    private static bool ShouldReopenExplorer()
        => AppServices.History.GetSetting("force_explorer_restart_on_reapply") != "false";

    private static bool IsOneDrivePath(string folderPath)
    {
        var normalized = Path.GetFullPath(folderPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return IsUnderRoot(normalized, Environment.GetEnvironmentVariable("OneDrive")) ||
               IsUnderRoot(normalized, Environment.GetEnvironmentVariable("OneDriveCommercial"));

        static bool IsUnderRoot(string normalizedPath, string? root)
        {
            if (string.IsNullOrWhiteSpace(root)) return false;

            var normalizedRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task ReopenExplorerWindowsAsync(string folderPath)
    {
        await Task.Run(() =>
        {
            var parentPath    = Directory.GetParent(folderPath)?.FullName;
            var pathsToReopen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType != null)
                {
                    var shell   = Activator.CreateInstance(shellType);
                    var windows = shellType.InvokeMember("Windows", System.Reflection.BindingFlags.InvokeMethod, null, shell, null);
                    var count   = windows?.GetType().InvokeMember("Count", System.Reflection.BindingFlags.GetProperty, null, windows, null) is int c ? c : 0;

                    for (var i = count - 1; i >= 0; i--)
                    {
                        try
                        {
                            var win = windows?.GetType().InvokeMember("Item", System.Reflection.BindingFlags.InvokeMethod, null, windows, new object[] { i });
                            if (win == null) continue;
                            var url  = win.GetType().InvokeMember("LocationURL", System.Reflection.BindingFlags.GetProperty, null, win, null) as string;
                            if (string.IsNullOrWhiteSpace(url)) continue;
                            var loc  = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.LocalPath : null;
                            if (string.IsNullOrWhiteSpace(loc)) continue;

                            if (string.Equals(loc, parentPath, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(loc, folderPath, StringComparison.OrdinalIgnoreCase))
                            {
                                pathsToReopen.Add(loc);
                                win.GetType().InvokeMember("Quit", System.Reflection.BindingFlags.InvokeMethod, null, win, null);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            if (pathsToReopen.Count == 0 && !string.IsNullOrWhiteSpace(parentPath))
                pathsToReopen.Add(parentPath);

            Thread.Sleep(300);

            foreach (var path in pathsToReopen)
            {
                try
                {
                    if (Directory.Exists(path))
                        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"")
                            { UseShellExecute = true });
                }
                catch { }
            }
        });
    }
}

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
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using CoreCropMode = Folderly.Core.Composition.CropMode;

namespace Folderly.App.Views;

public partial class ApplyWindow : Window
{
    private const string NetworkWarningSeenSettingKey = "network_folder_warning_seen";
    private const int ExactPreviewSize = 640;
    private const int InteractivePreviewSize = 384;
    private const int LiveSourcePreviewMaxSize = 640;

    private readonly ApplyViewModel _vm;
    private bool _webViewReady;
    private static readonly Dictionary<string, string> CachedHtmlByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private int _previewRenderVersion;
    private bool _previewRenderActive;
    private bool _previewRenderPending;
    private bool _previewRenderPendingExact;
    private int _latestTransformRevision;
    private int _previewRenderPendingTransformRevision;
    private BitmapSource? _previewSourceBitmapCacheKey;
    private Image<Rgba32>? _previewSourceImageCache;
    private BitmapSource? _liveSourceBitmapSentKey;
    private bool _hasSentPreviewImage;

    public ApplyWindow(string folderPath)
    {
        var sw = Stopwatch.StartNew();
        StartupTrace.Log($"ApplyWindow.ctor begin path={folderPath}");
        InitializeComponent();
        StartupTrace.Log($"ApplyWindow.ctor InitializeComponent completed elapsed={StartupTrace.Elapsed(sw)}");
        Title = AppServices.Localize["ApplyWindowTitle"];
        _vm = new ApplyViewModel(folderPath);
        TryRestoreExistingCustomization();
        StartupTrace.Log($"ApplyWindow.ctor completed elapsed={StartupTrace.Elapsed(sw)}");

        Loaded += async (_, _) =>
        {
            StartupTrace.Log($"ApplyWindow.Loaded path={_vm.FolderPath}");
            await InitWebViewAsync();
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        ClearPreviewSourceCache();
        base.OnClosed(e);
    }

    // ─── WebView2 初期化 ────────────────────────────────────────────────────

    private async Task InitWebViewAsync()
    {
        var sw = Stopwatch.StartNew();
        StartupTrace.Log("ApplyWindow.InitWebView begin");
        try
        {
            var env = await AppServices.GetWebView2EnvironmentAsync();
            StartupTrace.Log($"ApplyWindow.InitWebView environment ready elapsed={StartupTrace.Elapsed(sw)}");
            await WebView.EnsureCoreWebView2Async(env);
            StartupTrace.Log($"ApplyWindow.InitWebView EnsureCoreWebView2 completed elapsed={StartupTrace.Elapsed(sw)}");

            WebView.CoreWebView2.Settings.IsNonClientRegionSupportEnabled = true;
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled   = false;
            WebView.CoreWebView2.Settings.AreDevToolsEnabled              = false;

            WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            var html = LoadHtml();
            StartupTrace.Log($"ApplyWindow.InitWebView HTML ready length={html.Length} elapsed={StartupTrace.Elapsed(sw)}");
            WebView.NavigateToString(html);
            StartupTrace.Log($"ApplyWindow.InitWebView NavigateToString called elapsed={StartupTrace.Elapsed(sw)}");
        }
        catch (Exception ex)
        {
            StartupTrace.Log($"ApplyWindow.InitWebView failed elapsed={StartupTrace.Elapsed(sw)} error={ex.Message}");
            MessageBox.Show(
                string.Format(AppServices.Localize["WebViewInitFailed"], ex.Message),
                "Folderly", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private static string LoadHtml()
    {
        var sw = Stopwatch.StartNew();
        var language = AppServices.Localize.CurrentLang;
        if (CachedHtmlByLanguage.TryGetValue(language, out var cachedHtml))
        {
            StartupTrace.Log($"ApplyWindow.LoadHtml cache hit language={language} elapsed={StartupTrace.Elapsed(sw)}");
            return cachedHtml;
        }

        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("Folderly.App.Resources.ApplyWindow.html");
        if (stream is null) throw new InvalidOperationException("ApplyWindow.html が見つかりません");
        using var reader = new StreamReader(stream);
        var html = LocalizeHtml(reader.ReadToEnd());
        CachedHtmlByLanguage[language] = html;
        StartupTrace.Log($"ApplyWindow.LoadHtml cache miss language={language} length={html.Length} elapsed={StartupTrace.Elapsed(sw)}");
        return html;
    }

    private static string LocalizeHtml(string html)
    {
        var L = AppServices.Localize;
        static string Html(string value) => WebUtility.HtmlEncode(value);
        string T(string key) => Html(L[key]);
        string DefaultTagName(string key) =>
            Html(L[key].Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim() ?? L[key]);

        html = html
            .Replace("__FOLDERLY_HTML_LANG__", Html(L.HtmlLang), StringComparison.Ordinal)
            .Replace("__FOLDERLY_UI_FONT_STACK__", L.CssFontFamily, StringComparison.Ordinal)
            .Replace("__FOLDERLY_MONO_FONT_STACK__", L.CssMonospaceFontFamily, StringComparison.Ordinal);

        var replacements = new (string From, string To)[]
        {
            ("対象フォルダ", T("TargetFolder")),
            ("最小化", T("WindowMinimize")),
            ("最大化", T("WindowMaximize")),
            ("閉じる", T("WindowClose")),
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
            ("開発", DefaultTagName("TagBlue")),
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
            ("ホーム", T("IconHome")),
            ("フォルダ", T("IconFolder")),
            ("タグ", T("IconTag")),
            ("場所", T("IconPlace")),
            ("カレンダー", T("IconCalendar")),
            ("フラグ", T("IconFlag")),
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
        var sw = Stopwatch.StartNew();
        StartupTrace.Log($"ApplyWindow.NavigationCompleted success={e.IsSuccess} webError={e.WebErrorStatus}");
        // 初回ナビゲーション完了後のみ状態を送信
        if (!_webViewReady)
        {
            _webViewReady = true;
            await SendStateAsync();
            StartupTrace.Log($"ApplyWindow.NavigationCompleted initial state sent elapsed={StartupTrace.Elapsed(sw)}");
            await SendPreviewAsync();
            StartupTrace.Log($"ApplyWindow.NavigationCompleted initial preview sent elapsed={StartupTrace.Elapsed(sw)}");
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
                        UpdateTransformRevision(root);
                        var modeStr = root.GetProperty("mode").GetString() ?? "Center";
                        _vm.SetCropMode(ParseCropMode(modeStr), resetPosition: true);
                        await SendTransformStateAsync();
                        await SendPreviewAsync();
                        break;

                    case "resetPosition":
                        UpdateTransformRevision(root);
                        _vm.ResetPosition();
                        await SendStateAsync();
                        await SendPreviewAsync();
                        break;

                    case "resetImage":
                        _vm.SourceImage = null;
                        _vm.SourceImagePath = string.Empty;
                        _vm.IsImageResetPending = _vm.HasExistingCustomization;
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

        NormalizeViewModelAdjustParams();

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
            canApply          = _vm.CanApply,
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
        _previewRenderPendingTransformRevision = _latestTransformRevision;

        if (_previewRenderActive) return;

        _previewRenderActive = true;

        try
        {
            while (_previewRenderPending)
            {
                var renderVersion = _previewRenderVersion;
                var renderExact = _previewRenderPendingExact;
                var renderTransformRevision = _previewRenderPendingTransformRevision;
                _previewRenderPending = false;
                _previewRenderPendingExact = false;

                await RenderAndSendPreviewAsync(renderExact, renderVersion, renderTransformRevision);
            }
        }
        finally
        {
            _previewRenderActive = false;
        }

        if (_previewRenderPending)
            await SendPreviewAsync(exact: false);
    }

    private async Task RenderAndSendPreviewAsync(bool exact, int renderVersion, int transformRevision)
    {
        if (!_webViewReady) return;

        // OffscreenPreview プロパティを現在の ViewModel 状態に同期
        if (_vm.SourceImage is null)
        {
            _hasSentPreviewImage = false;
            ClearPreviewSourceCache();
            await ExecuteScriptSafeAsync("window.folderlyClearSourceImage && window.folderlyClearSourceImage(); window.folderlyClearPreview && window.folderlyClearPreview()");
            return;
        }

        // WPF レイアウトを強制更新してからレンダリング
        await SendLiveSourceImageAsync();

        if (exact && NormalizeViewModelAdjustParams())
            await SendTransformStateAsync(transformRevision);

        var showLoading = exact && !_hasSentPreviewImage;
        if (showLoading)
            await ExecuteScriptSafeAsync("window.folderlySetPreviewLoading && window.folderlySetPreviewLoading(true)");

        byte[] pngBytes;
        try
        {
            pngBytes = await RenderPreviewPngAsync(exact);
        }
        catch
        {
            if (showLoading)
                await ExecuteScriptSafeAsync("window.folderlySetPreviewLoading && window.folderlySetPreviewLoading(false)");
            throw;
        }

        if (renderVersion != _previewRenderVersion && _previewRenderPending)
        {
            if (showLoading)
                await ExecuteScriptSafeAsync("window.folderlySetPreviewLoading && window.folderlySetPreviewLoading(false)");
            return;
        }

        var b64     = Convert.ToBase64String(pngBytes);
        var dataUrl = $"data:image/png;base64,{b64}";

        await ExecuteScriptSafeAsync($"window.folderlySetPreview('{dataUrl}', {transformRevision})");
        _hasSentPreviewImage = true;
    }

    private async Task<Image<Rgba32>> GetPreviewSourceImageAsync()
    {
        if (ReferenceEquals(_previewSourceBitmapCacheKey, _vm.SourceImage) &&
            _previewSourceImageCache is not null)
        {
            return _previewSourceImageCache;
        }

        ClearPreviewSourceCache();
        _previewSourceBitmapCacheKey = _vm.SourceImage;

        using var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_vm.SourceImage!));
        encoder.Save(stream);
        stream.Position = 0;

        _previewSourceImageCache = await Image.LoadAsync<Rgba32>(stream);
        return _previewSourceImageCache;
    }

    private void ClearPreviewSourceCache()
    {
        _previewSourceImageCache?.Dispose();
        _previewSourceImageCache = null;
        _previewSourceBitmapCacheKey = null;
        _liveSourceBitmapSentKey = null;
    }

    private async Task SendLiveSourceImageAsync()
    {
        if (!_webViewReady || _vm.SourceImage is null) return;
        if (ReferenceEquals(_liveSourceBitmapSentKey, _vm.SourceImage)) return;

        var sourceImage = await GetPreviewSourceImageAsync();

        using var liveImage = sourceImage.Clone();
        if (liveImage.Width > LiveSourcePreviewMaxSize || liveImage.Height > LiveSourcePreviewMaxSize)
        {
            liveImage.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(LiveSourcePreviewMaxSize, LiveSourcePreviewMaxSize),
                Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3,
            }));
        }

        using var ms = new MemoryStream();
        await liveImage.SaveAsPngAsync(ms);
        var dataUrl = $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
        var dataUrlJson = JsonSerializer.Serialize(dataUrl);

        _liveSourceBitmapSentKey = _vm.SourceImage;
        await ExecuteScriptSafeAsync(
            $"window.folderlySetSourceImage && window.folderlySetSourceImage({dataUrlJson}, {liveImage.Width}, {liveImage.Height})");
    }

    private async Task<byte[]> RenderPreviewPngAsync(bool exact)
    {
        var sourceImage = await GetPreviewSourceImageAsync();
        var previewSize = exact ? ExactPreviewSize : InteractivePreviewSize;
        var previewScale = previewSize / (double)FolderTemplate.BaseSize;
        var previewParams = new ImageAdjustParams(
            Scale: (float)_vm.Scale,
            OffsetX: (float)(_vm.OffsetX * previewScale),
            OffsetY: (float)(_vm.OffsetY * previewScale),
            Mode: _vm.CropMode);

        using var adjustedImage = ImageAdjuster.Adjust(
            sourceImage,
            FolderTemplate.GetImageRegionPixelSize(previewSize),
            previewParams);

        var tagNameForIcon = TagSettingsService.GetShowTagNameOnIcon()
            ? TagSettingsService.GetDisplayName(_vm.SelectedTagColor)
            : null;

        using var composed = TemplateRenderer.Render(
            adjustedImage,
            _vm.EffectiveSelectedTagColor,
            previewSize,
            tagNameForIcon,
            TagSettingsService.GetTagIconIndex(_vm.SelectedTagColor),
            TagSettingsService.GetShowTagIconOnIcon());

        using var ms = new MemoryStream();
        composed.SaveAsPng(ms);
        return ms.ToArray();
    }

    private void UpdateTransform(JsonElement root)
    {
        UpdateTransformRevision(root);

        if (root.TryGetProperty("scale", out var scale))
            _vm.Scale = scale.GetDouble();
        if (root.TryGetProperty("offsetX", out var offsetX))
            _vm.OffsetX = offsetX.GetDouble();
        if (root.TryGetProperty("offsetY", out var offsetY))
            _vm.OffsetY = offsetY.GetDouble();
        if (root.TryGetProperty("cropMode", out var cropMode))
            _vm.CropMode = ParseCropMode(cropMode.GetString());
        else if (root.TryGetProperty("mode", out var mode))
            _vm.CropMode = ParseCropMode(mode.GetString());
    }

    private void UpdateTransformRevision(JsonElement root)
    {
        if (!root.TryGetProperty("revision", out var revision)) return;
        if (revision.ValueKind != JsonValueKind.Number) return;
        if (!revision.TryGetInt32(out var value)) return;

        _latestTransformRevision = Math.Max(_latestTransformRevision, value);
    }

    private bool NormalizeViewModelAdjustParams()
    {
        if (_vm.SourceImage is null) return false;

        var current = _vm.GetAdjustParams();
        var normalized = ImageAdjuster.Normalize(
            _vm.SourceImage.PixelWidth,
            _vm.SourceImage.PixelHeight,
            FolderTemplate.GetImageRegionPixelSize(),
            current);

        var changed =
            Math.Abs(normalized.Scale - current.Scale) > 0.0001f ||
            Math.Abs(normalized.OffsetX - current.OffsetX) > 0.0001f ||
            Math.Abs(normalized.OffsetY - current.OffsetY) > 0.0001f ||
            normalized.Mode != current.Mode;

        if (!changed)
            return false;

        _vm.Scale = normalized.Scale;
        _vm.OffsetX = normalized.OffsetX;
        _vm.OffsetY = normalized.OffsetY;
        _vm.CropMode = normalized.Mode;
        return true;
    }

    private async Task SendTransformStateAsync(int? transformRevision = null)
    {
        var state = new
        {
            scale = _vm.Scale,
            offsetX = _vm.OffsetX,
            offsetY = _vm.OffsetY,
            cropMode = _vm.CropMode.ToString(),
            revision = transformRevision ?? _latestTransformRevision,
        };
        var json = JsonSerializer.Serialize(state);
        await ExecuteScriptSafeAsync($"window.folderlySetTransform && window.folderlySetTransform({json})");
    }

    private static CoreCropMode ParseCropMode(string? mode)
        => mode switch
        {
            "FitWidth"  => CoreCropMode.FitWidth,
            "FitHeight" => CoreCropMode.FitHeight,
            _           => CoreCropMode.Center,
        };

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
        var sw = Stopwatch.StartNew();
        StartupTrace.Log($"ApplyWindow.TryRestoreExistingCustomization begin path={_vm.FolderPath}");
        try
        {
            var entry = AppServices.History.GetByPath(Path.GetFullPath(_vm.FolderPath));
            if (entry is null)
            {
                StartupTrace.Log($"ApplyWindow.TryRestoreExistingCustomization no entry elapsed={StartupTrace.Elapsed(sw)}");
                return;
            }
            _vm.HasExistingCustomization = true;
            if (string.IsNullOrWhiteSpace(entry.SourceImagePath))
            {
                StartupTrace.Log($"ApplyWindow.TryRestoreExistingCustomization entry without source image elapsed={StartupTrace.Elapsed(sw)}");
                return;
            }
            if (!File.Exists(entry.SourceImagePath))
            {
                StartupTrace.Log($"ApplyWindow.TryRestoreExistingCustomization source image missing elapsed={StartupTrace.Elapsed(sw)}");
                return;
            }
            if (!LoadImage(entry.SourceImagePath, resetPosition: false, showError: false))
            {
                StartupTrace.Log($"ApplyWindow.TryRestoreExistingCustomization source image load failed elapsed={StartupTrace.Elapsed(sw)}");
                return;
            }

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
            StartupTrace.Log($"ApplyWindow.TryRestoreExistingCustomization restored elapsed={StartupTrace.Elapsed(sw)}");
        }
        catch (Exception ex)
        {
            StartupTrace.Log($"ApplyWindow.TryRestoreExistingCustomization failed elapsed={StartupTrace.Elapsed(sw)} error={ex.Message}");
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
        if (Directory.Exists(_vm.FolderPath))
        {
            dlg.InitialDirectory = _vm.FolderPath;
        }

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
            _vm.IsImageResetPending = false;
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
        var sw = Stopwatch.StartNew();
        StartupTrace.Log($"ApplyWindow.LoadImage begin resetPosition={resetPosition} path={path}");
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
            _vm.IsImageResetPending = false;
            if (resetPosition)
                _vm.ResetPosition();

            _ = SendPreviewAsync();
            StartupTrace.Log($"ApplyWindow.LoadImage completed width={bitmap.PixelWidth} height={bitmap.PixelHeight} elapsed={StartupTrace.Elapsed(sw)}");
            return true;
        }
        catch (Exception ex)
        {
            StartupTrace.Log($"ApplyWindow.LoadImage failed elapsed={StartupTrace.Elapsed(sw)} error={ex.Message}");
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

    private async Task RevertImageResetAsync()
    {
        _vm.IsApplying = true;
        await ExecuteScriptSafeAsync("window.folderlySetApplying(true)");

        try
        {
            await AppServices.Revert.RevertAsync(_vm.FolderPath);
            _vm.HasExistingCustomization = false;
            _vm.IsImageResetPending = false;

            Hide();
            if (ShouldReopenExplorer())
                await ReopenExplorerWindowsAsync(_vm.FolderPath);

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(AppServices.Localize["RevertFailed"], ex.Message),
                "Folderly", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _vm.IsApplying = false;
            await ExecuteScriptSafeAsync("window.folderlySetApplying(false)");
        }
    }

    private async Task ApplyAsync()
    {
        if (!_vm.CanApply) return;

        if (_vm.SourceImage is null && _vm.IsImageResetPending)
        {
            await RevertImageResetAsync();
            return;
        }

        var protection = FolderProtection.CheckPath(_vm.FolderPath);
        if (protection.IsWarning)
        {
            var L   = AppServices.Localize;
            var isNetwork = FolderProtection.IsNetworkPath(_vm.FolderPath);
            var shouldShowWarning = !isNetwork ||
                                    AppServices.History.GetSetting(NetworkWarningSeenSettingKey) != "1";
            if (shouldShowWarning)
            {
                var title = isNetwork
                    ? L["NetworkFolderWarningTitle"]
                    : L["WarningGenericTitle"];
                var msg = isNetwork
                    ? L["NetworkFolderWarningMessage"]
                    : string.Format(L["WarningGenericMessage"], protection.Reason);
                var res = MessageBox.Show(msg, title,
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (res != MessageBoxResult.OK) return;

                if (isNetwork)
                    AppServices.History.SetSetting(NetworkWarningSeenSettingKey, "1");
            }
        }

        _vm.IsApplying = true;
        await ExecuteScriptSafeAsync("window.folderlySetApplying(true)");

        try
        {
            NormalizeViewModelAdjustParams();

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

                if (result.IconVerified)
                    await AppServices.License.InitializeAsync();

                var purchasePromptApplyCount = result.IconVerified && AppServices.License.IsActive && AppServices.License.IsTrial
                    ? PurchasePromptService.RecordTrialSuccessfulApplyAndGetPromptCount()
                    : null;

                var reviewPromptApplyCount = result.IconVerified && AppServices.License.IsActive && !AppServices.License.IsTrial
                    ? ReviewPromptService.RecordSuccessfulApplyAndGetPromptCount()
                    : null;

                if (!result.IconVerified)
                {
                    var warningMessage = BuildApplyVerificationWarningMessage(result);
                    MessageBox.Show(
                        warningMessage,
                        AppServices.Localize["ApplyVerificationWarningTitle"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                Hide();
                if (ShouldReopenExplorer())
                    await ReopenExplorerWindowsAsync(_vm.FolderPath);

                if (purchasePromptApplyCount is not null)
                {
                    await Task.Delay(3000);
                    ShowPurchasePrompt(purchasePromptApplyCount.Value);
                }
                else if (reviewPromptApplyCount is not null)
                {
                    await Task.Delay(3000);
                    ShowReviewPrompt(reviewPromptApplyCount.Value);
                }

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

    private static string BuildApplyVerificationWarningMessage(ApplyResult result)
    {
        var L = AppServices.Localize;
        if (result.Diagnostics?.IsNetwork == true)
        {
            return string.Format(
                L["ApplyVerificationNetworkWarningMessage"],
                FormatDiagnostics(result.Diagnostics));
        }

        return L["ApplyVerificationWarningMessage"];
    }

    private static string FormatDiagnostics(ApplyDiagnostics diagnostics)
    {
        static string YesNo(bool value) => value ? "yes" : "no";

        return string.Join(Environment.NewLine, new[]
        {
            $"Path type: {diagnostics.LocationKind}",
            $"Folderly folder saved: {YesNo(diagnostics.FolderlyDirectoryExists)}",
            $"Icon file saved: {YesNo(diagnostics.ExpectedIconFileExists)}",
            $"desktop.ini saved: {YesNo(diagnostics.DesktopIniExists)}",
            $"desktop.ini references icon: {YesNo(diagnostics.DesktopIniReferencesExpectedIcon)}",
            $"desktop.ini Hidden: {YesNo(diagnostics.DesktopIniHidden)}",
            $"desktop.ini System: {YesNo(diagnostics.DesktopIniSystem)}",
            $"Folder ReadOnly: {YesNo(diagnostics.FolderReadOnly)}",
            $"Folder System: {YesNo(diagnostics.FolderSystem)}",
            $"Explorer verification: {YesNo(diagnostics.IconVerified)}",
        });
    }

    private void ShowReviewPrompt(int applyCount)
    {
        var shouldOpenReview = ShowForegroundReviewDialog();

        if (shouldOpenReview)
        {
            ReviewPromptService.MarkReviewOpened();
            StoreNavigationService.OpenReviewPage();
        }
        else
        {
            ReviewPromptService.MarkPromptSkipped(applyCount);
        }
    }

    private void ShowPurchasePrompt(int applyCount)
    {
        var shouldOpenStore = ShowForegroundPurchaseDialog();
        PurchasePromptService.MarkPromptHandled(applyCount);

        if (shouldOpenStore)
            StoreNavigationService.OpenProductPage();
    }

    private static bool ShowForegroundReviewDialog()
        => ShowForegroundDialog(ReviewPromptDialog.Show);

    private static bool ShowForegroundPurchaseDialog()
        => ShowForegroundDialog(PurchasePromptDialog.Show);

    private static bool ShowForegroundDialog(Func<Window, bool> showDialog)
    {
        var owner = new Window
        {
            Width = 1,
            Height = 1,
            Left = SystemParameters.WorkArea.Left + SystemParameters.WorkArea.Width / 2,
            Top = SystemParameters.WorkArea.Top + SystemParameters.WorkArea.Height / 2,
            WindowStyle = WindowStyle.None,
            ResizeMode = System.Windows.ResizeMode.NoResize,
            ShowInTaskbar = false,
            ShowActivated = true,
            Topmost = true,
            Opacity = 0.01
        };

        try
        {
            owner.Show();
            owner.Activate();
            owner.Focus();

            return showDialog(owner);
        }
        finally
        {
            owner.Close();
        }
    }

    // ─── Explorer 再起動 ─────────────────────────────────────────────────────

    private static bool ShouldReopenExplorer()
        => AppServices.History.GetSetting("force_explorer_restart_on_reapply") != "false";

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

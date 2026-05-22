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
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CoreCropMode = Folderly.Core.Composition.CropMode;

namespace Folderly.App.Views;

public partial class ApplyWindow : Window
{
    private readonly ApplyViewModel _vm;
    private bool _webViewReady;
    private static string? _cachedHtml;

    public ApplyWindow(string folderPath)
    {
        InitializeComponent();
        _vm = new ApplyViewModel(folderPath);

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
        if (_cachedHtml is not null) return _cachedHtml;
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("Folderly.App.Resources.ApplyWindow.html");
        if (stream is null) throw new InvalidOperationException("ApplyWindow.html が見つかりません");
        using var reader = new StreamReader(stream);
        return _cachedHtml = reader.ReadToEnd();
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

    private async Task SendPreviewAsync()
    {
        if (!_webViewReady) return;

        // OffscreenPreview プロパティを現在の ViewModel 状態に同期
        OffscreenPreview.SourceImage      = _vm.SourceImage;
        OffscreenPreview.SelectedTagColor = _vm.EffectiveSelectedTagColor;
        OffscreenPreview.TagName          = TagSettingsService.GetDisplayName(_vm.SelectedTagColor);
        OffscreenPreview.ShowTagNameOnIcon = TagSettingsService.GetShowTagNameOnIcon();
        OffscreenPreview.TagIconIndex     = TagSettingsService.GetTagIconIndex(_vm.SelectedTagColor);
        OffscreenPreview.ShowTagIconOnIcon = TagSettingsService.GetShowTagIconOnIcon();
        OffscreenPreview.Scale            = _vm.Scale;
        OffscreenPreview.OffsetX          = _vm.OffsetX;
        OffscreenPreview.OffsetY          = _vm.OffsetY;
        OffscreenPreview.CropMode         = _vm.CropMode;

        // WPF レイアウトを強制更新してからレンダリング
        OffscreenPreview.Measure(new Size(320, 320));
        OffscreenPreview.Arrange(new Rect(0, 0, 320, 320));
        OffscreenPreview.UpdateLayout();

        var rtb = new RenderTargetBitmap(320, 320, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(OffscreenPreview);

        using var ms = new MemoryStream();
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        enc.Save(ms);

        var b64     = Convert.ToBase64String(ms.ToArray());
        var dataUrl = $"data:image/png;base64,{b64}";

        await ExecuteScriptSafeAsync($"window.folderlySetPreview('{dataUrl}')");
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

    private void LoadImage(string path)
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
            _vm.ResetPosition();

            _ = SendPreviewAsync();
        }
        catch
        {
            MessageBox.Show(AppServices.Localize["ImageLoadError"], "Folderly",
                MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var msg = string.Format(L["WarningGenericMessage"], protection.Reason);
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
                    "document.getElementById('btn-apply').textContent='✓ 適用完了';");

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

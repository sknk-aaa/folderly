using Folderly.App.Infrastructure;
using System.Diagnostics;
using Windows.Services.Store;

namespace Folderly.App.Services;

/// <summary>
/// Microsoft Store のライセンス状態を管理する（SPEC F-16）。
/// 未パッケージ環境（開発時）では Store API が使えないため、フェイルセーフとして試用版扱いにする。
/// SPEC Section 8.4: "Store API 失敗 → 試用版として扱う（フェイルセーフ）"
/// </summary>
public sealed class StoreLicenseService
{
    private const int MaximumDisplayableTrialDays = 365;

    private StoreContext? _context;
    private StoreAppLicense? _license;
    private Task? _initializeTask;

    public bool IsTrial      { get; private set; } = true;
    public bool IsActive     { get; private set; } = true;
    public int  DaysRemaining{ get; private set; } = 7;
    public bool HasDisplayableTrialDays => IsTrial && DaysRemaining is >= 0 and <= MaximumDisplayableTrialDays;

    public event EventHandler? LicenseChanged;

    public async Task InitializeAsync()
    {
        var sw = Stopwatch.StartNew();
        if (_initializeTask is not null)
        {
            StartupTrace.Log("StoreLicense.Initialize reused task");
            await _initializeTask;
            StartupTrace.Log($"StoreLicense.Initialize reused task completed elapsed={StartupTrace.Elapsed(sw)}");
            return;
        }

        StartupTrace.Log("StoreLicense.Initialize begin");
        _initializeTask = InitializeCoreAsync();
        await _initializeTask;
        StartupTrace.Log($"StoreLicense.Initialize completed elapsed={StartupTrace.Elapsed(sw)}");
    }

    private async Task InitializeCoreAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _context = StoreContext.GetDefault();
            StartupTrace.Log($"StoreLicense.InitializeCore context ready elapsed={StartupTrace.Elapsed(sw)}");
            _context.OfflineLicensesChanged += OnLicenseChanged;
            await RefreshAsync();
            StartupTrace.Log($"StoreLicense.InitializeCore completed elapsed={StartupTrace.Elapsed(sw)}");
        }
        catch
        {
            // 未パッケージ環境またはStore API 失敗 → フェイルセーフ: 試用版として扱う
            IsTrial       = true;
            IsActive      = true;
            DaysRemaining = 7;
        }
    }

    private async void OnLicenseChanged(StoreContext sender, object args)
    {
        await RefreshAsync();
        LicenseChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task RefreshAsync()
    {
        if (_context == null) return;
        var sw = Stopwatch.StartNew();
        StartupTrace.Log("StoreLicense.Refresh begin");
        try
        {
            _license = await _context.GetAppLicenseAsync();
            IsActive  = _license.IsActive;
            IsTrial   = _license.IsTrial;

            if (IsTrial && _license.ExpirationDate != DateTimeOffset.MinValue)
            {
                var remaining = _license.ExpirationDate - DateTimeOffset.UtcNow;
                DaysRemaining = Math.Max(0, (int)Math.Ceiling(remaining.TotalDays));
            }
            else
            {
                DaysRemaining = 0;
            }
            StartupTrace.Log($"StoreLicense.Refresh completed isActive={IsActive} isTrial={IsTrial} elapsed={StartupTrace.Elapsed(sw)}");
        }
        catch (Exception ex)
        {
            StartupTrace.Log($"StoreLicense.Refresh failed elapsed={StartupTrace.Elapsed(sw)} error={ex.Message}");
            // Store API エラー → 現在の状態を維持
        }
    }
}

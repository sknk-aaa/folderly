using Windows.Services.Store;

namespace Folderly.App.Services;

/// <summary>
/// Microsoft Store のライセンス状態を管理する（SPEC F-16）。
/// 未パッケージ環境（開発時）では Store API が使えないため、フェイルセーフとして試用版扱いにする。
/// SPEC Section 8.4: "Store API 失敗 → 試用版として扱う（フェイルセーフ）"
/// </summary>
public sealed class StoreLicenseService
{
    private StoreContext? _context;
    private StoreAppLicense? _license;

    public bool IsTrial      { get; private set; } = true;
    public bool IsActive     { get; private set; } = true;
    public int  DaysRemaining{ get; private set; } = 7;

    public event EventHandler? LicenseChanged;

    public async Task InitializeAsync()
    {
        try
        {
            _context = StoreContext.GetDefault();
            _context.OfflineLicensesChanged += OnLicenseChanged;
            await RefreshAsync();
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
        }
        catch
        {
            // Store API エラー → 現在の状態を維持
        }
    }
}

using System.Globalization;
using MauiApp1.Resources.Languages;
using MauiApp1.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;

namespace MauiApp1
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // ===== Theme =====
            var theme = SettingsService.Theme;
            if (theme == "Dark")
                Current.UserAppTheme = AppTheme.Dark;
            else
                Current.UserAppTheme = AppTheme.Light;

            // ===== Language =====
            var lang = SettingsService.Language;
            var culture = new CultureInfo(lang);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            AppResources.Culture = culture;

            // ===== Firebase / POI Repository =====
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vinh_khanh.db3");
            PoiRepository.Init(dbPath);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override void OnStart()
        {
            base.OnStart();

            _ = PoiRepository.Instance.InitializeAsync();

            // 🔥 LOG DEVICE + SET STATUS ONLINE
            _ = LogAppAccessWithStatus("online");

            // DEBUG (giữ nguyên)
            _ = DebugFirebaseAsync();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Sync lại data
            _ = PoiRepository.Instance.SyncFromFirebaseAsync();

            // 🔥 SET STATUS ONLINE khi mở lại app
            _ = LogAppAccessWithStatus("online");
        }

        protected override void OnSleep()
        {
            base.OnSleep();

            // 🔥 SET STATUS OFFLINE NGAY KHI TẮT APP
            _ = LogAppAccessWithStatus("offline");
        }

        // ════════════════════════════════════════
        // 🔥 DEEP LINKING (APP LINKS)
        // ════════════════════════════════════════
        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            // Xử lý link dạng https://vinh-khanh-cms.web.app/poi.html?id=xxx
            if (uri.Host == "vinh-khanh-cms.web.app" && uri.AbsolutePath.Contains("/poi.html"))
            {
                string query = uri.Query; // "?id=xxx"
                if (query.Contains("id="))
                {
                    var poiId = query.Split("id=")[1].Split('&')[0];
                    if (!string.IsNullOrEmpty(poiId))
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            // Đảm bảo Shell đã khởi tạo
                            while (Shell.Current == null)
                                await Task.Delay(100);

                            // Điều hướng vào màn hình chi tiết POI
                            await Shell.Current.GoToAsync($"POIDetailPage?poiId={poiId}");
                        });
                    }
                }
            }
        }

        // ════════════════════════════════════════
        // 🔥 DEVICE ID
        // ════════════════════════════════════════
        private string GetDeviceId()
        {
            string key = "device_id";

            if (Preferences.ContainsKey(key))
                return Preferences.Get(key, "");

            string newId = Guid.NewGuid().ToString();
            Preferences.Set(key, newId);

            return newId;
        }

        // ════════════════════════════════════════
        // 🔥 LOG ACCESS (CALL FIREBASE)
        // ════════════════════════════════════════
        private async Task LogAppAccess()
        {
            try
            {
                string deviceId = GetDeviceId();

                await FirebaseService.Instance.SetDeviceLogAsync(
                    deviceId,
                    DeviceInfo.Model,
                    DeviceInfo.Platform.ToString()
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] LogAccess lỗi: {ex.Message}");
            }
        }

        // ════════════════════════════════════════
        // 🔥 SET DEVICE STATUS (online/offline)
        // ════════════════════════════════════════
        private async Task LogAppAccessWithStatus(string status)
        {
            try
            {
                string deviceId = GetDeviceId();
                string device = DeviceInfo.Model;
                string platform = DeviceInfo.Platform.ToString();

                // Set device info + status (Device + Platform + Status + LastActive)
                await FirebaseService.Instance.SetDeviceStatusAsync(
                    deviceId,
                    device,
                    platform,
                    status
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] SetStatus lỗi: {ex.Message}");
            }
        }

        // ===== DEBUG GIỮ NGUYÊN =====
        private async Task DebugFirebaseAsync()
        {
            try
            {
                using var http = new System.Net.Http.HttpClient();
                http.Timeout = TimeSpan.FromSeconds(5);

                var ping = await http.GetAsync("https://www.google.com");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Internet: {ping.StatusCode}");

                var PROJECT_ID = "vinh-khanh-cms";
                var API_KEY = "AIzaSyDO7cvTxvx26Qu6Bo6Ts5ZT0cl8yBhcj5s";
                var url = $"https://firestore.googleapis.com/v1/projects/{PROJECT_ID}/databases/(default)/documents/pois?key={API_KEY}";

                System.Diagnostics.Debug.WriteLine($"[DEBUG] URL: {url}");

                var resp = await http.GetAsync(url);
                var body = await resp.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] HTTP Status: {resp.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Response: {body.Substring(0, Math.Min(500, body.Length))}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Exception: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
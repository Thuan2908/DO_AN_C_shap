using MauiApp1.Models;
using SQLite;
using System.Globalization;

namespace MauiApp1.Services
{
    /// <summary>
    /// Nguồn dữ liệu POI duy nhất cho toàn app.
    /// Chiến lược offline-first:
    ///   1. Load SQLite cache ngay lập tức (không chờ mạng)
    ///   2. Fetch Firebase background
    ///   3. Nếu Firebase trả data → cập nhật SQLite + báo UI reload
    /// </summary>
    public class PoiRepository
    {
        private readonly SQLiteAsyncConnection _db;
        private readonly FirebaseService _firebase;

        // UI subscribe event này để reload danh sách khi data mới về
        public event Action<List<POI>>? OnPoisUpdated;
        private HashSet<string> _logged = new();
        private static PoiRepository? _instance;
        public static PoiRepository Instance => _instance
            ?? throw new InvalidOperationException("Gọi Init() trước.");

        public static void Init(string dbPath)
        {
            _instance = new PoiRepository(dbPath);
        }

        private PoiRepository(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _firebase = FirebaseService.Instance;
        }

        // ══════════════════════════════════════════
        // KHỞI ĐỘNG — gọi 1 lần trong App.xaml.cs
        // ══════════════════════════════════════════
        public async Task InitializeAsync()
        {
            await _db.CreateTableAsync<POI>();

            // Load cache ngay → UI có data ngay lập tức
            var cached = await _db.Table<POI>().ToListAsync();
            if (cached.Count > 0)
                OnPoisUpdated?.Invoke(cached);

            // Sync Firebase background
            _ = SyncFromFirebaseAsync();
        }

        // ══════════════════════════════════════════
        // SYNC TỪ FIREBASE
        // ══════════════════════════════════════════
        public async Task SyncFromFirebaseAsync()
        {
            var pois = await _firebase.FetchPoisAsync();
            if (pois == null || pois.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[Repo] Firebase không trả data, dùng cache.");
                return;
            }

            // Cập nhật SQLite
            await _db.DeleteAllAsync<POI>();
            await _db.InsertAllAsync(pois);
            //  _logged.Clear();
            System.Diagnostics.Debug.WriteLine($"[Repo] Đã sync {pois.Count} POI từ Firebase.");

            // Thông báo UI trên main thread
            MainThread.BeginInvokeOnMainThread(() => OnPoisUpdated?.Invoke(pois));
        }

        // ══════════════════════════════════════════
        // ĐỌC
        // ══════════════════════════════════════════
        public async Task<List<POI>> GetAllAsync()
        {
            var pois = await _db.Table<POI>().ToListAsync();
            if (pois.Count == 0)
            {
                // Cache rỗng → thử lấy thẳng từ Firebase
                var fresh = await _firebase.FetchPoisAsync();
                if (fresh != null && fresh.Count > 0)
                {
                    await _db.InsertAllAsync(fresh);
                    return fresh;
                }
            }
            return pois;
        }

        public async Task<POI?> GetByIdAsync(int id)
            => await _db.Table<POI>().FirstOrDefaultAsync(p => p.Id == id);

        // ══════════════════════════════════════════
        // GHI LỊCH SỬ (fire-and-forget)
        // ══════════════════════════════════════════
        //public void LogPlay(POI poi, string source)
        //{
        //    var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        //    var device = DeviceInfo.Platform.ToString();
        //    _ = _firebase.LogHistoryAsync(poi.GetName(), lang, source, device);
        //}

        //mới chú ý
        public void LogPlay(POI poi, string source)
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            string key = $"{poi.Id}_{source}_{lang}_{today}";

            if (_logged.Contains(key))
                return;

            _logged.Add(key);

            var device = DeviceInfo.Platform.ToString();

            _ = _firebase.LogHistoryAsync(
                poi.GetName(),
                lang,
                source,
                device
            );
        }


    }
}
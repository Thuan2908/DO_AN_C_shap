using System.Net.Http.Json;
using System.Text.Json;
using MauiApp1.Models;

namespace MauiApp1.Services
{
    /// <summary>
    /// Đồng bộ POI từ Firestore REST API (không cần SDK nặng).
    /// Chiến lược: Firebase là nguồn chính, SQLite là cache offline.
    /// </summary>
    public class FirebaseService
    {
        // ══════════════════════════════════════════
        // ⚠️  THAY 2 DÒNG NÀY BẰNG CỦA BẠN
        // ══════════════════════════════════════════
        private const string PROJECT_ID = "vinh-khanh-cms";   // vd: vinh-khanh-cms
        private const string API_KEY = "AIzaSyDO7cvTxvx26Qu6Bo6Ts5ZT0cl8yBhcj5s";       // từ Firebase Console

        private const string BASE_URL =
            $"https://firestore.googleapis.com/v1/projects/{PROJECT_ID}/databases/(default)/documents";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // ── Singleton ──
        private static FirebaseService? _instance;
        public static FirebaseService Instance => _instance ??= new FirebaseService();
        private FirebaseService() { }

        // ══════════════════════════════════════════
        // LẤY TẤT CẢ POI TỪ FIRESTORE
        // ══════════════════════════════════════════
        /// <summary>
        /// Trả về danh sách POI từ Firestore.
        /// Nếu mạng lỗi → trả về null (caller dùng SQLite cache).
        /// </summary>
        public async Task<List<POI>?> FetchPoisAsync()
        {
            try
            {
                var url = $"{BASE_URL}/pois?key={API_KEY}";
                var resp = await _http.GetAsync(url);

                if (!resp.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Firebase] HTTP {resp.StatusCode}");
                    return null;
                }

                var root = await resp.Content.ReadFromJsonAsync<FirestoreListResponse>(_jsonOpts);
                if (root?.Documents == null || root.Documents.Count == 0)
                    return new List<POI>();

                var pois = new List<POI>();
                int localId = 1;

                foreach (var doc in root.Documents)
                {
                    var dto = ParseDocument(doc);
                    // bỏ qua doc lỗi hoặc bị ẩn; chấp nhận cả status rỗng (POI mới tạo chưa set)
                    if (dto != null && dto.Status != "inactive" && (dto.Latitude != 0 || dto.Longitude != 0))
                    {
                        pois.Add(dto.ToPoi(localId++));
                    }
                }

                // Sắp theo Priority rồi theo tên
                return pois
                    .OrderBy(p => p.Priority)
                    .ThenBy(p => p.Name_vi)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firebase] FetchPois lỗi: {ex.Message}");
                return null;
            }
        }

        // ══════════════════════════════════════════
        // GHI LỊCH SỬ SỬ DỤNG LÊN FIRESTORE
        // ══════════════════════════════════════════
        /// <summary>
        /// Ghi log khi người dùng nghe thuyết minh (GPS hoặc QR).
        /// Fire-and-forget — không await ở caller để không block UI.
        /// </summary>
        public async Task LogHistoryAsync(
            string poiName, string lang,
            string source, string device = "MAUI")
        {
            try
            {
                var url = $"{BASE_URL}/history?key={API_KEY}";
                var body = new
                {
                    fields = new
                    {
                        poiName = new { stringValue = poiName },
                        lang = new { stringValue = lang },
                        source = new { stringValue = source },
                        device = new { stringValue = device },
                        timestamp = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };
                var content = new StringContent(
                    JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8,
                    "application/json");

                await _http.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firebase] LogHistory lỗi: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════
        // PARSE FIRESTORE DOCUMENT FORMAT
        // ══════════════════════════════════════════
        // Firestore REST trả về format đặc biệt:
        // { "fields": { "name_vi": { "stringValue": "..." }, "lat": { "doubleValue": 10.7 } } }
        private FirebasePoiDto? ParseDocument(FirestoreDocument doc)
        {
            if (doc.Fields == null) return null;
            var f = doc.Fields;

            var firestoreId = doc.Name?.Split('/').LastOrDefault() ?? "";

            // ── DEBUG: in ra tất cả field keys để kiểm tra tên field thực tế ──
            System.Diagnostics.Debug.WriteLine(
                $"[Firebase] Doc {firestoreId} fields: {string.Join(", ", f.Keys)}");

            string Str(string key)
            {
                if (!f.TryGetValue(key, out var v)) { System.Diagnostics.Debug.WriteLine($"[Firebase]   MISS: {key}"); return ""; }
                var r = v.StringValue ?? ""; System.Diagnostics.Debug.WriteLine($"[Firebase]   {key} = \"{r}\""); return r;
            }
            double Dbl(string key)
            {
                if (!f.TryGetValue(key, out var v)) { System.Diagnostics.Debug.WriteLine($"[Firebase]   MISS: {key}"); return 0; }
                var r = v.DoubleValue ?? (v.IntegerValue.HasValue ? (double)v.IntegerValue.Value : 0);
                System.Diagnostics.Debug.WriteLine($"[Firebase]   {key} = {r}"); return r;
            }
            int Int(string key)
            {
                if (!f.TryGetValue(key, out var v)) { System.Diagnostics.Debug.WriteLine($"[Firebase]   MISS: {key}"); return 0; }
                var r = (int)(v.IntegerValue ?? (long)(v.DoubleValue ?? 0));
                System.Diagnostics.Debug.WriteLine($"[Firebase]   {key} = {r}"); return r;
            }

            // Parse scripts (nested map)
            FirebaseScripts? scripts = null;
            if (f.TryGetValue("scripts", out var scriptsField) && scriptsField.MapValue?.Fields != null)
            {
                var sf = scriptsField.MapValue.Fields;
                scripts = new FirebaseScripts
                {
                    Vi = sf.TryGetValue("vi", out var sv) ? sv.StringValue ?? "" : "",
                    En = sf.TryGetValue("en", out var se) ? se.StringValue ?? "" : "",
                    Ja = sf.TryGetValue("ja", out var sj) ? sj.StringValue ?? "" : "",
                };
            }

            // Field names dùng PascalCase khớp với CMS mới
            // Fallback camelCase cho data cũ nếu có
            string S(string pascal, string camel = "")
            {
                var v = Str(pascal);
                return v.Length > 0 ? v : (camel.Length > 0 ? Str(camel) : "");
            }

            return new FirebasePoiDto
            {
                FirestoreId = firestoreId,
                Name_vi = S("Name_vi", "name_vi"),
                Name_en = S("Name_en", "name_en"),
                Name_ja = S("Name_ja", "name_ja"),
                Address_vi = S("Address_vi", "address_vi"),
                Address_en = S("Address_en", "address_en"),
                Address_ja = S("Address_ja", "address_ja"),
                Description_vi = S("Description_vi", "description_vi"),
                Description_en = S("Description_en", "description_en"),
                Description_ja = S("Description_ja", "description_ja"),
                Detail_vi = S("Detail_vi", "detail_vi"),
                Detail_en = S("Detail_en", "detail_en"),
                Detail_ja = S("Detail_ja", "detail_ja"),
                Content_vi = S("Content_vi", "content_vi"),
                Content_en = S("Content_en", "content_en"),
                Content_ja = S("Content_ja", "content_ja"),
                Scripts = scripts,
                Latitude = Dbl("Latitude") != 0 ? Dbl("Latitude") : Dbl("lat"),
                Longitude = Dbl("Longitude") != 0 ? Dbl("Longitude") : Dbl("lng"),
                Radius = Dbl("Radius") != 0 ? Dbl("Radius") : Dbl("radius"),
                NearRadius = Dbl("NearRadius") != 0 ? Dbl("NearRadius") : Dbl("nearRadius"),
                Priority = Int("Priority") != 0 ? Int("Priority") : Int("priority"),
                Rating = Dbl("Rating") != 0 ? Dbl("Rating") : Dbl("rating"),
                ImageUrl = S("ImageUrl", "imageUrl"),
                Status = S("status", "status"),
                Icon = S("icon", "icon"),
            };
        }
        public async Task SetDeviceLogAsync(string deviceId, string device, string platform)
        {
            try
            {
                var url = $"{BASE_URL}/app_access_logs/{deviceId}?key={API_KEY}";

                var body = new
                {
                    fields = new
                    {
                        Device = new { stringValue = device },
                        Platform = new { stringValue = platform },
                        LastActive = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = content
                };

                await _http.SendAsync(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firebase] LogDevice lỗi: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════
        // 🔥 SET DEVICE STATUS (online/offline) + Device + Platform
        // ══════════════════════════════════════════
        public async Task SetDeviceStatusAsync(string deviceId, string device, string platform, string status)
        {
            try
            {
                var url = $"{BASE_URL}/app_access_logs/{deviceId}?key={API_KEY}";

                var body = new
                {
                    fields = new
                    {
                        Device = new { stringValue = device },
                        Platform = new { stringValue = platform },
                        Status = new { stringValue = status },
                        LastActive = new { timestampValue = DateTime.UtcNow.ToString("o") }
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = content
                };

                await _http.SendAsync(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firebase] SetStatus lỗi: {ex.Message}");
            }
        }
    }

    // ══════════════════════════════════════════
    // FIRESTORE REST RESPONSE MODELS
    // ══════════════════════════════════════════
    public class FirestoreListResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("documents")]
        public List<FirestoreDocument> Documents { get; set; } = new();
    }

    public class FirestoreDocument
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("fields")]
        public Dictionary<string, FirestoreValue>? Fields { get; set; }
    }

    public class FirestoreValue
    {
        [System.Text.Json.Serialization.JsonPropertyName("stringValue")]
        public string? StringValue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("doubleValue")]
        public double? DoubleValue { get; set; }

        // ⚠️ Firestore REST trả integerValue dạng STRING ("10" chứ không phải 10)
        // phải dùng string? rồi parse thủ công
        [System.Text.Json.Serialization.JsonPropertyName("integerValue")]
        public string? IntegerValueRaw { get; set; }

        // Helper: parse an toàn sang long
        [System.Text.Json.Serialization.JsonIgnore]
        public long? IntegerValue =>
            long.TryParse(IntegerValueRaw, out var v) ? v : null;

        [System.Text.Json.Serialization.JsonPropertyName("booleanValue")]
        public bool? BooleanValue { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("mapValue")]
        public FirestoreMapValue? MapValue { get; set; }
    }

    public class FirestoreMapValue
    {
        [System.Text.Json.Serialization.JsonPropertyName("fields")]
        public Dictionary<string, FirestoreValue>? Fields { get; set; }
    }




}
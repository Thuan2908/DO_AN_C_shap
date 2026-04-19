using SQLite;
using MauiApp1.Models;
using System.Text.Json;
//muốn dùng thì xóa AutoIncrement trong POI nhưng phải thêm id trong poi.json ko cần xóa data tải lại khó thêm cột mới
namespace MauiApp1.Database
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "poi.db");

            _database = new SQLiteAsyncConnection(dbPath);

            // tạo bảng nếu chưa có
            _database.CreateTableAsync<POI>().Wait();
        }

        // ===== lấy danh sách POI =====
        public Task<List<POI>> GetPOIsAsync()
        {
            return _database.Table<POI>().ToListAsync();
        }

        // ===== thêm POI =====
        public Task<int> AddPOIAsync(POI poi)
        {
            return _database.InsertAsync(poi);
        }

        // ===== update POI =====
        public Task<int> UpdatePOIAsync(POI poi)
        {
            return _database.UpdateAsync(poi);
        }

        // ===== import JSON =====
        public async Task ImportFromJson()
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("poi.json");
            using var reader = new StreamReader(stream);

            var json = await reader.ReadToEndAsync();

            var pois = JsonSerializer.Deserialize<List<POI>>(json);

            if (pois == null) return;

            foreach (var poi in pois)
            {
                var exist = await _database.FindAsync<POI>(poi.Id);

                if (exist == null)
                    await AddPOIAsync(poi);     // chưa có → thêm
                else
                    await UpdatePOIAsync(poi);  // có rồi → update
            }
        }
    }
}








//muốn dùng thì trong poi phải thêm AutoIncrement trong poi key , nhược điểm là phải xóa hết data tải lại nhưng ko cần thêm id trong poi.json dễ thêm cột mới
//namespace MauiApp1.Database
//{
//    public class DatabaseService
//    {
//        // biến kết nối database SQLite
//        private SQLiteAsyncConnection _database;

//        // ===== Constructor =====
//        public DatabaseService()
//        {
//            // tạo đường dẫn file database
//            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "poi.db");

//            // nếu database đã tồn tại thì xóa
//            if (File.Exists(dbPath))
//                File.Delete(dbPath);   // xóa DB cũ

//            // tạo kết nối SQLite
//            _database = new SQLiteAsyncConnection(dbPath);

//            // tạo bảng POI trong database
//            _database.CreateTableAsync<POI>().Wait();
//        }

//        // ===== Lấy toàn bộ POI =====
//        public Task<List<POI>> GetPOIsAsync()
//        {
//            // SELECT * FROM POI
//            return _database.Table<POI>().ToListAsync();
//        }

//        // ===== Thêm POI vào database =====
//        public Task<int> AddPOIAsync(POI poi)
//        {
//            // INSERT INTO POI
//            return _database.InsertAsync(poi);
//        }

//        // ===== Import dữ liệu từ JSON =====
//        public async Task ImportFromJson()
//        {
//            // lấy danh sách POI trong database
//            var list = await GetPOIsAsync();

//            // nếu database đã có dữ liệu thì không import nữa
//            if (list.Count > 0)
//                return;

//            // mở file JSON trong Resources/Raw
//            using var stream = await FileSystem.OpenAppPackageFileAsync("poi.json");

//            // đọc file
//            using var reader = new StreamReader(stream);

//            // chuyển thành string
//            var json = await reader.ReadToEndAsync();

//            // chuyển JSON -> List<POI>
//            var pois = JsonSerializer.Deserialize<List<POI>>(json);

//            // in ra số lượng POI trong JSON
//            Console.WriteLine($"JSON count: {pois?.Count}");

//            // nếu JSON lỗi thì dừng
//            if (pois == null)
//                return;

//            // thêm từng POI vào database
//            foreach (var poi in pois)
//            {
//                await AddPOIAsync(poi);
//            }
//        }
//    }
//}
using System.Collections.ObjectModel;
using MauiApp1.Models;
using MauiApp1.Resources.Languages;
using MauiApp1.Services;
using Microsoft.Maui.Devices.Sensors;

namespace MauiApp1.ViewModels
{
    public class MapViewModel
    {
        // service lay GPS
        private readonly GPSService _gpsService;

        // service xu ly geofence (vao/ra khu vuc)
        private GeofenceService? _geofenceService;

        // service audio doc noi dung
        private readonly AudioService _audioService;

        // danh sach POI hien thi tren map/list
        public ObservableCollection<POI> POIs { get; set; }

        // danh sach POI day du (de filter tim kiem)
        List<POI> _allPOIs = new();

        // su kien khi POI load xong
        public event Action? POIsLoaded;

        // su kien khi GPS cap nhat vi tri
        public event Action<Location>? LocationUpdated;

        // su kien khi trang thai POI thay doi (vao/ra khu vuc)
        public event Action<string, bool>? POIStateChanged;


        public MapViewModel()
        {
            _gpsService = new GPSService();

            POIs = new ObservableCollection<POI>();

            // dang ky su kien GPS thay doi vi tri
            _gpsService.LocationChanged += OnLocationChanged;

            _audioService = AudioService.Instance;

            // ── MỚI: khi Firebase sync xong → tự reload UI ──
            PoiRepository.Instance.OnPoisUpdated += OnPoisUpdated;

            _ = StartGPS();
        }

        // khoi tao du lieu
        public async Task InitializeAsync()
        {
            await LoadPOIs();
        }

        // ══════════════════════════════════════════════════
        // THAY ĐỔI CHÍNH: load từ PoiRepository thay vì
        // DatabaseService.ImportFromJson() + GetPOIsAsync()
        // ══════════════════════════════════════════════════
        private async Task LoadPOIs()
        {
            // GetAllAsync(): trả cache SQLite ngay, background sync Firebase
            var list = await PoiRepository.Instance.GetAllAsync();

            ApplyPoisToUI(list);
        }

        // callback khi Firebase sync xong (chạy trên MainThread)
        private void OnPoisUpdated(List<POI> freshPois)
        {
            ApplyPoisToUI(freshPois);
        }

        // áp dụng danh sách POI mới vào UI + geofence
        private void ApplyPoisToUI(List<POI> list)
        {
            _allPOIs = list.ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                POIs.Clear();
                foreach (var poi in list)
                    POIs.Add(poi);

                POIsLoaded?.Invoke();
            });

            // rebuild geofence với POI mới nhất
            _geofenceService = new GeofenceService(list);
            _geofenceService.GeofenceTriggered += OnGeofenceTriggered;
        }

        // bat dau nghe GPS
        private async Task StartGPS()
        {
            await _gpsService.StartListeningAsync();
        }

        // khi GPS thay doi vi tri
        private async void OnLocationChanged(Location location)
        {
            LocationUpdated?.Invoke(location);

            if (_geofenceService != null)
                await _geofenceService.ProcessLocation(location);
        }

        // khi co su kien geofence (vao/ra vung)
        private async void OnGeofenceTriggered(GeofenceEvent e)
        {
            switch (e.EventType)
            {
                case GeofenceEventType.Enter:
                    POIStateChanged?.Invoke(e.POI.Name, true);
                    break;

                case GeofenceEventType.Exit:
                    POIStateChanged?.Invoke(e.POI.Name, false);
                    break;

                //case GeofenceEventType.StopAudio:
                //    _audioService.Stop();
                //    break;

                case GeofenceEventType.Audio:

                    if (_audioService.IsManualMode)
                        return;

                    if (_audioService.IsPlaying)
                        return;

                    var text = $"{AppResources.EnterArea} {e.POI.Name}. {e.POI.Content}";
                    await _audioService.Speak(text);

                    // ── MỚI: ghi lịch sử lên Firestore (fire-and-forget) ──
                    PoiRepository.Instance.LogPlay(e.POI, source: "GPS");

                    break;
            }
        }

        // dung GPS
        public void Stop()
        {
            _gpsService.StopListening();

            // ── MỚI: hủy đăng ký để tránh memory leak ──
            PoiRepository.Instance.OnPoisUpdated -= OnPoisUpdated;
        }

        // ham filter POI theo tu khoa tim kiem
        public void FilterPois(string keyword)
        {
            keyword = keyword?.ToLower() ?? "";

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allPOIs
                : _allPOIs.Where(p =>
                    p.Name?.ToLower().Contains(keyword) ?? false);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                POIs.Clear();
                foreach (var poi in filtered)
                    POIs.Add(poi);
            });
        }
    }
}
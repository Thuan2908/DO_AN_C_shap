using MauiApp1.Models;
using Microsoft.Maui.Devices.Sensors;

namespace MauiApp1.Services
{
    // service xu ly geofence (kiem tra vao/ra khu vuc POI)
    public class GeofenceService
    {
        // danh sach POI
        private List<POI> _pois;

        // thoi gian cooldown truoc khi audio cua 1 POI duoc phat lai
        private const int AudioCooldownSeconds = 60;

        // delay giua cac audio POI
        private const int NextAudioDelaySeconds = 3;

        // trang thai dang phat audio
        private bool _isPlaying = false;

        // su kien gui ve MapViewModel khi co thay doi geofence
        public event Action<GeofenceEvent>? GeofenceTriggered;

        public GeofenceService(List<POI> pois)
        {
            _pois = pois ?? new List<POI>();
        }

        // xu ly khi GPS cap nhat vi tri
        public async Task ProcessLocation(Location location)
        {
            // danh sach POI ma user dang o trong
            List<POI> insidePOIs = new();

            foreach (var poi in _pois)
            {
                // tinh khoang cach tu user den POI (met)
                double distanceMeters =
                    Location.CalculateDistance(
                        location,
                        new Location(poi.Latitude, poi.Longitude),
                        DistanceUnits.Kilometers) * 1000;

                bool wasInside = poi.IsInside;
                bool isInside = distanceMeters <= poi.Radius;

                // ENTER (lan dau vao khu vuc POI)
                if (!wasInside && isInside)
                {
                    poi.IsInside = true;

                    GeofenceTriggered?.Invoke(new GeofenceEvent
                    {
                        POI = poi,
                        EventType = GeofenceEventType.Enter
                    });
                }

                // EXIT (roi khoi khu vuc POI)
                if (wasInside && !isInside)
                {
                    poi.IsInside = false;

                    GeofenceTriggered?.Invoke(new GeofenceEvent
                    {
                        POI = poi,
                        EventType = GeofenceEventType.Exit
                    });

                    GeofenceTriggered?.Invoke(new GeofenceEvent
                    {
                        POI = poi,
                        EventType = GeofenceEventType.StopAudio
                    });
                }

                if (isInside)
                    insidePOIs.Add(poi);
            }

            if (!insidePOIs.Any())
                return;

            if (!_isPlaying)
            {
                _isPlaying = true;
                await PlayInsidePOIs(insidePOIs);
                _isPlaying = false;
            }
        }

        // phat audio cac POI ma user dang o trong
        private async Task PlayInsidePOIs(List<POI> insidePOIs)
        {
            var sorted = insidePOIs
                .OrderByDescending(p => p.Priority)
                .ToList();

            foreach (var poi in sorted)
            {
                if (!poi.IsInside)
                    continue;

                if (!CanPlayAudio(poi))
                    continue;

                poi.LastTriggered = DateTime.Now;

                GeofenceTriggered?.Invoke(new GeofenceEvent
                {
                    POI = poi,
                    EventType = GeofenceEventType.Audio
                });

                // ── MỚI: ghi lịch sử lên Firestore ngay khi kích hoạt ──
                // fire-and-forget: không await, không block audio
                //chú ý
                // PoiRepository.Instance.LogPlay(poi, source: "GPS");

                // doi audio doc xong truoc khi doc POI tiep
                while (AudioService.Instance.IsPlaying)
                    await Task.Delay(300);

                await Task.Delay(NextAudioDelaySeconds * 1000);
            }
        }

        // kiem tra POI co du dieu kien phat audio hay khong
        private bool CanPlayAudio(POI poi)
        {
            return (DateTime.Now - poi.LastTriggered)
                > TimeSpan.FromSeconds(AudioCooldownSeconds);
        }
    }
}
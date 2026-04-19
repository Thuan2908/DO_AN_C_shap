using Microsoft.Maui.Devices.Sensors;

namespace MauiApp1.Services
{
    // service lay vi tri GPS lien tuc
    public class GPSService
    {
        // dung de huy task GPS
        private CancellationTokenSource? _cts;

        // trang thai dang tracking hay khong
        private bool _isTracking;

        // su kien khi vi tri thay doi
        public event Action<Location>? LocationChanged;

        // bat dau nghe GPS
        public async Task StartListeningAsync()
        {
            // neu dang tracking thi khong chay lai
            if (_isTracking)
                return;

            _isTracking = true;
            _cts = new CancellationTokenSource();

            // chay ham theo doi GPS (background task)
            _ = TrackLocationAsync(_cts.Token);
        }

        // ham theo doi vi tri lien tuc
        private async Task TrackLocationAsync(CancellationToken token)
        {
            // luu vi tri truoc do
            Location? lastLocation = null;

            // khoang cach toi thieu de coi la di chuyen
            const double MIN_DISTANCE_METERS = 2;

            // delay ban dau
            int delay = 2000;

            // dem so lan khong di chuyen
            int idleCounter = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // yeu cau lay GPS
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.High,
                        TimeSpan.FromSeconds(4)
                    );

                    var location = await Geolocation.Default
                        .GetLocationAsync(request, token);

                    if (location != null)
                    {
                        // neu do chinh xac kem thi bo qua
                        if (location.Accuracy > 40)
                            continue;

                        // lan dau tien lay GPS
                        if (lastLocation == null)
                        {
                            lastLocation = location;

                            // gui vi tri moi
                            LocationChanged?.Invoke(location);
                        }
                        else
                        {
                            // tinh khoang cach giua 2 vi tri
                            double distance =
                                Location.CalculateDistance(
                                    lastLocation,
                                    location,
                                    DistanceUnits.Kilometers) * 1000;

                            // neu di chuyen hon 2m
                            if (distance >= MIN_DISTANCE_METERS)
                            {
                                lastLocation = location;

                                // reset trang thai idle
                                idleCounter = 0;
                                delay = 2000;

                                // gui vi tri moi
                                LocationChanged?.Invoke(location);
                            }
                            else
                            {
                                // chua di chuyen
                                idleCounter++;

                                // neu dung yen nhieu lan
                                if (idleCounter >= 5)
                                {
                                    // van gui vi tri de update UI
                                    LocationChanged?.Invoke(location);

                                    // giam tan suat lay GPS
                                    delay = 6000;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // bo qua loi GPS
                }

                // delay truoc khi lay GPS lan tiep theo
                await Task.Delay(delay, token);
            }
        }

        // dung nghe GPS
        public void StopListening()
        {
            if (!_isTracking)
                return;

            _cts?.Cancel();
            _isTracking = false;
        }
    }
}
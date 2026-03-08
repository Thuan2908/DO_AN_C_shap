using FoodStreet.Models;
using FoodStreet.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;

namespace FoodStreet.PageModels
{
    public class MainPageModel : INotifyPropertyChanged
    {
        private readonly PoiRepository repo = new();
        private readonly LocationService locationService = new();
        GeofenceService geofenceService = new();
        List<Poi> poiCache = new();
        NarrationService narrationService = new();
        public ObservableCollection<Poi> Pois { get; set; } = new();

        public MainPageModel()
        {
            Init();
        }
        private async void Init()
        {
            await Load();   // Load POI trước

            locationService.OnLocationChanged += OnLocationChanged;
            await locationService.StartAsync();  // Start GPS sau
        }
        //public MainPageModel()
        //{
        //    _ = Load();

        //    locationService.OnLocationChanged += OnLocationChanged;
        //    _ = locationService.StartAsync();
        //}

        public event PropertyChangedEventHandler? PropertyChanged;

        void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(name));
        }

        void OnLocationChanged(Location loc)
        {
            //Console.WriteLine($"Lat: {loc.Latitude}");
            //Console.WriteLine($"Lng: {loc.Longitude}");

            var pois = geofenceService.CheckNearby(loc, poiCache);

            if (pois.Count > 0)
            {
                foreach (var poi in pois)
                {
                    Console.WriteLine($"Đã vào POI: {poi.Name}");
                    _ = narrationService.SpeakAsync(poi);
                }
            }
        }


        //void OnLocationChanged(Location loc)
        //{
        //    var fakeLocation = new Location(10.764221, 106.701187);

        //    var poi = geofenceService.CheckNearby(fakeLocation, poiCache);
        //    Console.WriteLine($"Số POI: {poiCache.Count}");
        //    if (poi != null)
        //    {
        //        Console.WriteLine($"🔥 Đã vào POI: {poi.Name}");
        //    }
        //}

        //async Task Load()
        //{
        //    await repo.Init();

        //    var list = await repo.GetAll();

        //    if (list.Count == 0)
        //    {
        //        await repo.Insert(new Poi
        //        {
        //            Name = "Bánh tráng Vĩnh Khánh",
        //            Latitude = 10.761573,
        //            Longitude = 106.702549,
        //            Radius = 100,
        //            Priority = 1,
        //            Description = "Quán nổi tiếng",
        //            TtsScript = "Bạn đang đến khu bánh tráng Vĩnh Khánh"
        //        });

        //        list = await repo.GetAll();
        //    }

        //    MainThread.BeginInvokeOnMainThread(() =>
        //    {
        //        Pois.Clear();

        //        foreach (var poi in list)
        //            Pois.Add(poi);
        //    });

        //    Console.WriteLine("POI loaded: " + list.Count);
        //    poiCache = list;
        //}
        async Task Load()
        {
            await repo.Init();

            var list = await repo.GetAll();

            var existing = list
                .FirstOrDefault(p => p.Name == "Bánh tráng Vĩnh Khánh");

            if (existing != null)
            {
                // Cập nhật lại tọa độ nếu đã tồn tại
                existing.Latitude = 10.761573;
                existing.Longitude = 106.702549;
                existing.Radius = 50;
                existing.Priority = 1;
                existing.Description = "Quán nổi tiếng";
                existing.TtsScript = "Bạn đang đến khu bánh tráng Vĩnh Khánh";

                await repo.Update(existing);
            }
            else
            {
                // Insert nếu chưa có
                await repo.Insert(new Poi
                {
                    Name = "Bánh tráng Vĩnh Khánh",
                    Latitude = 10.761573,
                    Longitude = 106.702549,
                    Radius = 50,
                    Priority = 1,
                    Description = "Quán nổi tiếng",
                    TtsScript = "Bạn đang đến khu bánh tráng Vĩnh Khánh"
                });
            }

            // Load lại sau khi update/insert
            list = await repo.GetAll();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Pois.Clear();
                foreach (var poi in list)
                    Pois.Add(poi);
            });

            poiCache = list;

            Console.WriteLine("POI loaded: " + list.Count);
        }

        public void FilterPois(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Pois.Clear();
                    foreach (var poi in poiCache)
                        Pois.Add(poi);
                });
                return;
            }

            var filtered = poiCache
                .Where(p => p.Name.ToLower().Contains(keyword.ToLower()))
                .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Pois.Clear();
                foreach (var poi in filtered)
                    Pois.Add(poi);
            });
        }



    }
}
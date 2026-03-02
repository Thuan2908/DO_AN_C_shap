using FoodStreet.Models;
using FoodStreet.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Maui.Devices.Sensors;

namespace FoodStreet.PageModels
{
    public class MainPageModel : INotifyPropertyChanged
    {
        private readonly PoiRepository repo = new();
        private readonly LocationService locationService = new();
        GeofenceService geofenceService = new();
        List<Poi> poiCache = new();
       
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
            Console.WriteLine($"Lat: {loc.Latitude}");
            Console.WriteLine($"Lng: {loc.Longitude}");

            var poi = geofenceService.CheckNearby(loc, poiCache);

            if (poi != null)
            {
                Console.WriteLine($" Đã vào POI: {poi.Name}");
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

        async Task Load()
        {
            await repo.Init();

            var list = await repo.GetAll();

            if (list.Count == 0)
            {
                await repo.Insert(new Poi
                {
                    Name = "Bánh tráng Vĩnh Khánh",
                    Latitude = 10.764221,
                    
                    Longitude = 106.701187,
                    Radius = 1,
                    Priority = 1,
                    Description = "Quán nổi tiếng",
                    TtsScript = "Bạn đang đến khu bánh tráng Vĩnh Khánh"
                });

                list = await repo.GetAll();
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Pois.Clear();

                foreach (var poi in list)
                    Pois.Add(poi);
            });

            Console.WriteLine("POI loaded: " + list.Count);
            poiCache = list;
        }
    }
}
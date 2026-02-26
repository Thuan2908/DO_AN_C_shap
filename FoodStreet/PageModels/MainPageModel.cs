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

        public ObservableCollection<Poi> Pois { get; set; } = new();

        public MainPageModel()
        {
            _ = Load();

            locationService.OnLocationChanged += OnLocationChanged;
            _ = locationService.StartAsync();
        }

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
        }

        private async Task Load()
        {
            await repo.Init();

            var list = await repo.GetAll();

            if (list.Count == 0)
            {
                await repo.Insert(new Poi
                {
                    Name = "Bánh tráng Vĩnh Khánh",
                    Latitude = 10.755,
                    Longitude = 106.705,
                    Radius = 30,
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
        }
    }
}
using FoodStreet.Models;
using FoodStreet.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace FoodStreet.Pages;

public partial class MapPage : ContentPage
{
    PoiRepository repo = new();
    Dictionary<Poi, Pin> poiPins = new();
    LocationService locationService = new();
    GeofenceService geofenceService = new();
    List<Poi> poiList = new();
    //public MapPage()
    //{
    //    InitializeComponent();
    //    //BindingContext = new MainPageModel();
    //    LoadMap();
    //}
    public MapPage()
    {
        InitializeComponent();
        //BindingContext = new MainPageModel();
        locationService.OnLocationChanged += OnLocationChanged;

        LoadMap();

        _ = locationService.StartAsync();
    }

    async void LoadMap()
    {
        await repo.Init();
        var pois = await repo.GetAll();
        poiList = pois;

        foreach (var poi in pois)
        {
            var location = new Location(poi.Latitude, poi.Longitude);

            var pin = new Pin
            {
                Label = poi.Name,
                Location = location,
                Type = PinType.Place
            };

            pin.InfoWindowClicked += async (s, e) =>
            {
                await Navigation.PushAsync(new PoiDetailPage(poi));
            };

            map.Pins.Add(pin);
            poiPins.Add(poi, pin);

            // Vẽ vòng tròn geofence
            var circle = new Circle
            {
                Center = location,
                Radius = new Distance(poi.Radius),
                StrokeColor = Colors.Blue,
                StrokeWidth = 2,
                FillColor = Colors.Blue.WithAlpha(0.2f)
            };

            map.MapElements.Add(circle);
        }
    }

    void OnLocationChanged(Location loc)
    {
        var nearbyPois = geofenceService.CheckNearby(loc, poiList);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            HighlightPois(nearbyPois);
        });

    }
    void HighlightPois(List<Poi> pois)
    {
        foreach (var item in poiPins)
        {
            var pin = item.Value;

            pin.Label = "";
            pin.Type = PinType.Place;
        }

        foreach (var poi in pois)
        {
            if (poiPins.TryGetValue(poi, out var pin))
            {
                pin.Label = "⭐ " + poi.Name;
                pin.Type = PinType.SavedPin;
            }
        }
    }
}
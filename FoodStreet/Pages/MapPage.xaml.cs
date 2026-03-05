using FoodStreet.Models;
using FoodStreet.Services;
using Microsoft.Maui.Controls.Maps;

namespace FoodStreet.Pages;

public partial class MapPage : ContentPage
{
    PoiRepository repo = new();

    public MapPage()
    {
        InitializeComponent();
        LoadMap();
    }

    async void LoadMap()
    {
        await repo.Init();
        var pois = await repo.GetAll();

        foreach (var poi in pois)
        {
            var pin = new Pin
            {
                Label = poi.Name,
                Location = new Location(poi.Latitude, poi.Longitude),
                Type = PinType.Place
            };

            map.Pins.Add(pin);
        }
    }
}
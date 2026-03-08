using FoodStreet.Models;
using FoodStreet.Services;

namespace FoodStreet.Pages;

public partial class PoiDetailPage : ContentPage
{
    Poi poi;

    NarrationService narration = new();

    public PoiDetailPage(Poi poiData)
    {
        InitializeComponent();
       
        poi = poiData;

        title.Text = poi.Name;
        description.Text = poi.Description;
    }

    async void OnSpeakClicked(object sender, EventArgs e)
    {
        await narration.SpeakAsync(poi);
    }
}
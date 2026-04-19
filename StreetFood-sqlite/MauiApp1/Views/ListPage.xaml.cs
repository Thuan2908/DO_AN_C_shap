#pragma warning disable CA1416
using MauiApp1.Models;
using MauiApp1.ViewModels;
using System;
using System.Collections.Generic;

namespace MauiApp1.Views;

// nhan ViewModel tu trang MapPage truyen qua
[QueryProperty(nameof(ViewModel), "ViewModel")]
public partial class ListPage : ContentPage
{
    // gan ViewModel vao BindingContext de binding UI
    public MapViewModel ViewModel
    {
        set => BindingContext = value;
    }

    public ListPage()
    {
        InitializeComponent();
    }

    // nut mo trang chi tiet POI
    private async void DetailButton_Clicked(object sender, EventArgs e)
    {
        // lay POI tu CommandParameter cua button
        if (sender is ImageButton btn && btn.CommandParameter is POI selectedPOI)
        {
            // chuyen sang trang POI Detail
            await Shell.Current.GoToAsync(nameof(POIDetailPage), new Dictionary<string, object>
            {
                { "SelectedPOI", selectedPOI }
            });
        }
    }

    // nut mo POI tren map
    private async void MapButton_Clicked(object sender, EventArgs e)
    {
        // lay POI duoc chon
        if (sender is ImageButton btn && btn.CommandParameter is POI poi)
        {
            // quay lai MapPage va truyen POI duoc chon
            await Shell.Current.GoToAsync("..", new Dictionary<string, object>
        {
            { "SelectedPOI", poi }
        });
        }
    }

    // su kien khi user go text vao SearchBar
    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e) // them vào sự kiện TextChanged của SearchBar
    {
        // goi ham filter trong MapViewModel
        if (BindingContext is MapViewModel vm)
        {
            vm.FilterPois(e.NewTextValue);
        }
    }
}
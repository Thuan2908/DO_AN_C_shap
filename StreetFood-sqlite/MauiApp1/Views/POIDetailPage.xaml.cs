#pragma warning disable CA1416

using MauiApp1.Models;
using MauiApp1.ViewModels;
using Microsoft.Maui.Media;


namespace MauiApp1.Views;

// nhan POI duoc truyen tu trang MapPage hoac ListPage
[QueryProperty(nameof(SelectedPOI), "SelectedPOI")]
public partial class POIDetailPage : ContentPage
{
    // ViewModel cua trang chi tiet
    private readonly POIDetailViewModel _viewModel;

    // property nhan POI tu navigation
    public POI? SelectedPOI
    {
        // gan POI vao ViewModel de hien thi du lieu
        set => _viewModel.SelectedPOI = value;
    }

    public POIDetailPage()
    {
        InitializeComponent();        // load giao dien XAML

        _viewModel = new POIDetailViewModel(); // tao ViewModel

        BindingContext = _viewModel;  // gan ViewModel vao UI de binding
    }

    // khi roi khoi trang (back, chuyen trang...)
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // dung audio neu dang phat
        _viewModel.Stop();            // Dừng audio khi rời trang
    }
}
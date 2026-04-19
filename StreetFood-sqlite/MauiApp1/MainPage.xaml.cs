using MauiApp1.Services;
using Microsoft.Maui.ApplicationModel;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Xin quyền GPS
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            await Permissions.RequestAsync<Permissions.LocationAlways>();


            // Chỉ bật nếu user cho phép
            if (SettingsService.GPSBackground)
            {
                BackgroundGpsManager.Start();
            }
            else
            {
                BackgroundGpsManager.Stop();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // nếu muốn dừng GPS
            // BackgroundGpsManager.Stop();
        }

        //private void OnCounterClicked(object? sender, EventArgs e)
        //{
        //    count++;

        //    if (count == 1)
        //        CounterBtn.Text = $"Clicked {count} time";
        //    else
        //        CounterBtn.Text = $"Clicked {count} times";

        //    SemanticScreenReader.Announce(CounterBtn.Text);
        //}
    }
}
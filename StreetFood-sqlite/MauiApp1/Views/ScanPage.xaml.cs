using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace MauiApp1.Views;

public partial class ScanPage : ContentPage
{
    public ScanPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("Lỗi", "Không có quyền camera", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        cameraView.IsDetecting = true; // bật scan
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        cameraView.IsDetecting = false; // tắt scan
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // 👇 Scan đang chạy nhưng CHƯA dùng dữ liệu
    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results.FirstOrDefault()?.Value;

        if (result != null)
        {
            // 👉 Tạm thời chỉ log, chưa xử lý gì
            System.Diagnostics.Debug.WriteLine($"Scan: {result}");

            // ❌ chưa return về Map
            // ❌ chưa xử lý POI
        }
    }
}
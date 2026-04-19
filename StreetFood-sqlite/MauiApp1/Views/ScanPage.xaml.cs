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
    private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results.FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(result))
            return;

        // Tắt nhận diện tạm thời để tránh quét trùng lặp nhiều lần
        cameraView.IsDetecting = false;

        // Kiểm tra xem kết quả có phải là URL không
        if (Uri.TryCreate(result, UriKind.Absolute, out var uri) && 
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Bật cờ hiệu NGAY LẬP TỨC để đảm bảo OnSleep không set offline
                App.IsInternalAction = true;

                // Lấy Device ID để truyền sang Web (giúp web giữ trạng thái online)
                string deviceId = (App.Current as App)?.GetDeviceId() ?? "";
                string finalUrl = result;
                if (!string.IsNullOrEmpty(deviceId))
                {
                    finalUrl += result.Contains("?") ? $"&device={deviceId}" : $"?device={deviceId}";
                }

                // Mở trình duyệt
                await Browser.Default.OpenAsync(new Uri(finalUrl), BrowserLaunchMode.SystemPreferred);
            });
        }
        else
        {
            // Nếu không phải URL, hiện thông báo bình thường
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Thông báo", $"Nội dung quét được: {result}", "Đóng");
                
                // Sau khi đóng thông báo thì bật lại scan
                cameraView.IsDetecting = true;
            });
        }
    }
}
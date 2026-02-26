using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace FoodStreet.Services;

public class LocationService
{
    public event Action<Location>? OnLocationChanged;

    bool isRunning = false;

    public async Task StartAsync()
    {
        if (isRunning)
            return;

        isRunning = true;

        // 🔥 BẮT BUỘC xin quyền runtime
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
        {
            Console.WriteLine("KHÔNG ĐƯỢC CẤP QUYỀN GPS");
            return;
        }

        while (isRunning)
        {
            try
            {
                var request = new GeolocationRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(10));

                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    Console.WriteLine("GPS ĐANG CHẠY");
                    OnLocationChanged?.Invoke(location);
                }
                else
                {
                    Console.WriteLine("Location NULL");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GPS ERROR: " + ex.Message);
            }

            await Task.Delay(3000);
        }
    }

    public void Stop()
    {
        isRunning = false;
    }
}
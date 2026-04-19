using global::Android.App;
using global::Android.Content;
using global::Android.OS;
using AndroidX.Core.App;
using MauiApp1.Services;

namespace MauiApp1.Platforms.Android.Services
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public class GpsForegroundService : Service
    {
        public const string CHANNEL_ID = "gps_service";

        GPSService gpsService = new GPSService();

        public override IBinder? OnBind(Intent intent) => null;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();

            var notification = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle("GPS đang chạy")
                .SetContentText("Ứng dụng đang theo dõi vị trí")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true)
                .Build();

            StartForeground(1001, notification);

            // kiểm tra sau khi đã start foreground
            if (!SettingsService.GPSBackground)
            {
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            StartGPS();

            return StartCommandResult.Sticky;
        }

        async void StartGPS()
        {
            await gpsService.StartListeningAsync();
        }

        public override void OnDestroy()
        {
            gpsService.StopListening();
            base.OnDestroy();
        }

        void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    CHANNEL_ID,
                    "GPS Service",
                    NotificationImportance.Low);

                var manager = (NotificationManager)GetSystemService(NotificationService);
                manager.CreateNotificationChannel(channel);
            }
        }
    }
}
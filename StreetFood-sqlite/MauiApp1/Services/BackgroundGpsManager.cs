#if ANDROID
using global::Android.Content;
using MauiApp1.Platforms.Android.Services;
#endif

namespace MauiApp1.Services;

public static class BackgroundGpsManager
{
    public static void Start()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;

        var intent = new Intent(context, typeof(GpsForegroundService));

        context.StartForegroundService(intent);
#endif
    }

    public static void Stop()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;

        var intent = new Intent(context, typeof(GpsForegroundService));

        context.StopService(intent);
#endif
    }
}
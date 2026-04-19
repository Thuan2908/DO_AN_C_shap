using Android.App;
using Android.Content.PM;
using Android.Content;
using Android.OS;

namespace MauiApp1
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Intent.ActionView },
                  Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
                  DataScheme = "https",
                  DataHost = "vinh-khanh-cms.web.app",
                  DataPathPrefix = "/poi.html",
                  AutoVerify = true)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}

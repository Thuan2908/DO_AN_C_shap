using CommunityToolkit.Maui;
using FoodStreet.Models;
using Microsoft.Extensions.Logging;

namespace FoodStreet
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // SERVICES
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<GeofenceService>();
            builder.Services.AddSingleton<NarrationService>();
            builder.Services.AddSingleton<PoiRepository>();

            // PAGES
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MapPage>();

            return builder.Build();
        }
    }
}
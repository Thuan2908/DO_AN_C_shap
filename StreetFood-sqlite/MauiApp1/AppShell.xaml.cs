using MauiApp1.Views;

namespace MauiApp1
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(POIDetailPage), typeof(POIDetailPage));
            Routing.RegisterRoute(nameof(ListPage), typeof(ListPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(ScanPage), typeof(ScanPage));
        }
    }
}

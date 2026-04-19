#pragma warning disable CA1416
using MauiApp1.Models;
using MauiApp1.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.Linq;
using MauiApp1.Services;

namespace MauiApp1.Views;

// nhan POI duoc chon tu trang khac (ListPage)
[QueryProperty(nameof(SelectedPOI), "SelectedPOI")]
public partial class MapPage : ContentPage
{
    MapViewModel vm;

    bool isFirstLoad = true; // chi zoom lan dau khi co GPS
    bool ignoreNextSelection = false;//thm // dung de bo qua lan chon POI tiep theo

    Dictionary<POI, Circle> poiCircles = new(); // luu vong tron cua tung POI
    POI? currentSelectedPOI; // POI dang duoc highlight

    //public POI SelectedPOI
    //{
    //    set
    //    {
    //        if (ignoreNextSelection)
    //        {
    //            ignoreNextSelection = false;
    //            return;
    //        }

    //        if (value != null)
    //            ShowSelectedPOI(value);
    //    }
    //}

    // nhan POI tu trang List
    public POI SelectedPOI
    {
        set
        {
            if (ignoreNextSelection)
            {
                ignoreNextSelection = false;
                return;
            }

            if (value == null)
                return;

            // ===== tránh zoom lại cùng POI =====
            if (currentSelectedPOI == value)
                return;

            ShowSelectedPOI(value);
        }
    }

    public MapPage()
    {
        InitializeComponent();

        vm = new MapViewModel();
        BindingContext = vm;

        // dang ky cac su kien
        vm.LocationUpdated += OnLocationUpdated; // GPS cap nhat
        vm.POIsLoaded += LoadPOIs; // load POI len map
        vm.POIStateChanged += OnPOIStateChanged; // thay doi trang thai vao/ra vung

        BackgroundGpsManager.Start(); // chạy GPS background

        // ===== Zoom map lần đầu =====
        //MainThread.BeginInvokeOnMainThread(() =>
        //{
        //    map.MoveToRegion(MapSpan.FromCenterAndRadius(
        //        new Location(10.761536, 106.702303),
        //        Distance.FromMeters(200)
        //    ));
        //});

        Init();
    }

    // khoi tao du lieu
    async void Init()
    {
        await vm.InitializeAsync();
    }

    // bam tab List
    private async void OnListTabTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ListPage),
            new Dictionary<string, object>
            {
                { "ViewModel", vm }
            });
    }

    // bam nut setting
    private async void OnSettingClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    // ===== Load POI =====
#pragma warning disable CA1416
    void LoadPOIs()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            map.Pins.Clear(); // xoa pin cu
            map.MapElements.Clear(); // xoa vung tron cu
            poiCircles.Clear();

            // tao vung tron cho tung POI
            foreach (var poi in vm.POIs)
            {
                var location = new Location(poi.Latitude, poi.Longitude);

                var circle = new Circle
                {
                    Center = location,
                    Radius = Distance.FromMeters(poi.Radius),
                    StrokeWidth = 2
                };

#pragma warning disable CA1416
                circle.StrokeColor = Colors.Blue;
                circle.FillColor = Colors.Blue.WithAlpha(0.2f);
#pragma warning restore CA1416

                poiCircles[poi] = circle;
                map.MapElements.Add(circle);
            }

            // tao pin cho tung POI
            foreach (var poi in vm.POIs)
            {
                var pin = new Pin
                {
                    Label = poi.Name,
                    Address = poi.Content,
                    Type = PinType.Place,
                    Location = new Location(poi.Latitude, poi.Longitude)
                };

                // khi click marker
                pin.MarkerClicked += (s, e) =>
                {
                    e.HideInfoWindow = false;
                };

                // khi click info window -> mo trang detail
                pin.InfoWindowClicked += async (s, e) =>
                {
                    ignoreNextSelection = true;

                    await Shell.Current.GoToAsync(nameof(POIDetailPage),
                        new Dictionary<string, object>
                        {
                            { "SelectedPOI", poi }
                        });
                };

                map.Pins.Add(pin);
            }
        });
    }

    // ===== GPS update =====
    private void OnLocationUpdated(Location location)
    {
        // chi zoom map lan dau khi co GPS
        if (!isFirstLoad) return;

        isFirstLoad = false;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var mapSpan = MapSpan.FromCenterAndRadius(
                new Location(location.Latitude, location.Longitude),
                Distance.FromMeters(100)
            );

            map.MoveToRegion(mapSpan);
        });
    }

    // ===== đổi màu vùng =====
    void OnPOIStateChanged(string name, bool isInside)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var poi = vm.POIs.FirstOrDefault(p => p.Name == name);

            if (poi == null) return;
            if (!poiCircles.ContainsKey(poi)) return;

            var circle = poiCircles[poi];

#pragma warning disable CA1416
            // neu dang o trong vung -> mau do
            if (isInside)
            {
                circle.FillColor = Colors.Red.WithAlpha(0.3f);
                circle.StrokeColor = Colors.Red;
            }
            else
            {
                // neu ra khoi vung -> mau xanh
                circle.FillColor = Colors.Blue.WithAlpha(0.2f);
                circle.StrokeColor = Colors.Blue;
            }
#pragma warning restore CA1416
        });
    }

    // ===== Hiện POI khi chọn từ List =====
    void ShowSelectedPOI(POI poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // ===== Nếu click lại chính POI đang chọn → bỏ chọn =====
            //            if (currentSelectedPOI == poi)
            //            {
            //                if (poiCircles.ContainsKey(poi))
            //                {
            //                    var circle = poiCircles[poi];

            //#pragma warning disable CA1416
            //                    if (poi.IsInside)
            //                    {
            //                        circle.FillColor = Colors.Red.WithAlpha(0.3f);
            //                        circle.StrokeColor = Colors.Red;
            //                    }
            //                    else
            //                    {
            //                        circle.FillColor = Colors.Blue.WithAlpha(0.2f);
            //                        circle.StrokeColor = Colors.Blue;
            //                    }
            //#pragma warning restore CA1416
            //                }

            //                currentSelectedPOI = null;

            //                return;
            //            }

            // ===== reset POI cũ =====
            if (currentSelectedPOI != null && poiCircles.ContainsKey(currentSelectedPOI))
            {
                var oldCircle = poiCircles[currentSelectedPOI];

#pragma warning disable CA1416
                if (currentSelectedPOI.IsInside)
                {
                    oldCircle.FillColor = Colors.Red.WithAlpha(0.3f);
                    oldCircle.StrokeColor = Colors.Red;
                }
                else
                {
                    oldCircle.FillColor = Colors.Blue.WithAlpha(0.2f);
                    oldCircle.StrokeColor = Colors.Blue;
                }
#pragma warning restore CA1416
            }

            // ===== zoom tới POI mới =====
            var location = new Location(poi.Latitude, poi.Longitude);

            map.MoveToRegion(MapSpan.FromCenterAndRadius(
                location,
                Distance.FromMeters(70)
            ));

            // ===== highlight vàng =====
            if (poiCircles.ContainsKey(poi))
            {
                var newCircle = poiCircles[poi];

#pragma warning disable CA1416
                newCircle.FillColor = Colors.Orange.WithAlpha(0.4f);
                newCircle.StrokeColor = Colors.Orange;
#pragma warning restore CA1416
            }

            // luu POI dang duoc chon
            currentSelectedPOI = poi;
        });
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ScanPage));
    }
}
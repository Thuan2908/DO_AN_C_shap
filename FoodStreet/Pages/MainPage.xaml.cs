namespace FoodStreet.Pages;
using FoodStreet.Models;
using FoodStreet.Utilities;



public partial class MainPage : ContentPage
    {
        MainPageModel vm;

        public MainPage()
        {
            InitializeComponent();

            vm = new MainPageModel();
            BindingContext = vm;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            vm.FilterPois(e.NewTextValue);
        }

        // Bấm vào quán -> mở map
        private async void OnOpenMap(object sender, TappedEventArgs e)
        {
            var poi = e.Parameter as Poi;

            if (poi == null)
                return;

         

        // gửi POI cho MapPage
        AppData.SelectedPoi = poi;
        await Shell.Current.GoToAsync("//map");
    }
    }

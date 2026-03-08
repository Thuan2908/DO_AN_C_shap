using FoodStreet.Models;
using FoodStreet.PageModels;

namespace FoodStreet.Pages
{
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
        private void OnPoiSelected(object sender, SelectionChangedEventArgs e)
        {
            var poi = e.CurrentSelection.FirstOrDefault() as Poi;

            if (poi == null)
                return;

            // chuyển sang tab Bản đồ
            //Shell.Current.CurrentItem = Shell.Current.Items[0];
            Shell.Current.CurrentItem = ((AppShell)Shell.Current).MapTabContent;

            ((CollectionView)sender).SelectedItem = null;
        }
    }
}
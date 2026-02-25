using FoodStreet.Models;
using FoodStreet.PageModels;

namespace FoodStreet.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}

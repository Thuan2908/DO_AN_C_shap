namespace FoodStreet
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            BindingContext = new MainPageModel();
        }
    }
}
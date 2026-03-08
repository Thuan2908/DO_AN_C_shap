namespace FoodStreet
{
    public partial class AppShell : Shell
    {
        public ShellContent MapTabContent => MapTab;
        public AppShell()
        {
            InitializeComponent();
            BindingContext = new MainPageModel();
        }
    }
}
using System.Windows.Controls;
using WinDeskClock.ViewModels;

namespace WinDeskClock.Pages.Settings
{
    public partial class About : Page
    {
        public About(AboutViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

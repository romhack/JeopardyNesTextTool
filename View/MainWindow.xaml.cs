using System.Windows;
using JeopardyNesTextTool.ViewModel;

namespace JeopardyNesTextTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new ApplicationViewModel();
            DataContext = viewModel;
        }
    }
}

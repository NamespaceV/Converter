using System.Windows;

namespace Converter
{
    public partial class MainWindow : Window
    {
        private MainWindowVM vm  = new MainWindowVM();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}

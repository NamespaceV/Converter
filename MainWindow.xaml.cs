using Converter.ViewModels;
using System.Windows;

namespace Converter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowVM();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (DataContext as MainWindowVM).OnClosing(e);
        }
    }
}

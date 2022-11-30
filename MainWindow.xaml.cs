using Converter.ViewModels;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Converter
{
    public partial class MainWindow : Window, IconSwitcher
    {
        private ImageSource _activeIcon;
        private ImageSource _passiveIcon;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowVM(this);
            
            _passiveIcon = Icon;
            _activeIcon = LoadActiveIcon();
        }

        private ImageSource LoadActiveIcon()
        {
            return new BitmapImage(new Uri(@"icon/icons8-replace-32-active.png", UriKind.RelativeOrAbsolute));
        }

        public void SetActiveIcon(bool active)
        {
            Icon = active ? _activeIcon : _passiveIcon;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (DataContext as MainWindowVM).OnClosing(e);
        }
    }
}

﻿using Converter.icon;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace Converter
{
    public partial class MainWindow : Window, IconSwitcher
    {
        private ImageSource _activeIcon;
        private ImageSource _passiveIcon;

        public MainWindow(MainWindowVM vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.SetIconSwitcher(this);
            
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

        private void FilesList_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space || e.Key == System.Windows.Input.Key.S) {
                (DataContext as MainWindowVM).ToggleSelected();
            }
        }
    }
}

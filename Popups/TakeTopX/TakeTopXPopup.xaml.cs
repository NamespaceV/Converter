using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Converter.Windows
{
    public partial class TakeTopXPopup : Window
    {
        private readonly MainWindowVM mainWindowVM;

        public TakeTopXPopup(MainWindowVM mainWindowVM)
        {
            InitializeComponent();
            this.mainWindowVM = mainWindowVM;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(number.Text, out int cnt)){
                mainWindowVM.TakeTop(cnt);
                this.Close();
            }
        }
    }
}

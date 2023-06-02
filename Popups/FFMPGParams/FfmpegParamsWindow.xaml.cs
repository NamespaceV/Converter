using Converter.Logic;
using System.Collections.Generic;
using System.Windows;

namespace Converter.Windows
{
    public partial class FfmpegParamsWindow : Window
    {
        public string Result { get; set; }
        private string ResetValue { get; set; }

        public FfmpegParamsWindow(string curArgs, string defArgs)
        {
            InitializeComponent();
            this.ResetValue = defArgs;
            TextInput.Text = curArgs;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Result = TextInput.Text;
            DialogResult = true;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            TextInput.Text = this.ResetValue;
        }
    }
}

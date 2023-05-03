using System.Windows;

namespace Converter.Windows
{
    public partial class TextInputWindow : Window
    {
        public string Result { get; set; }

        public TextInputWindow()
        {
            InitializeComponent();
            TextInput.Text = "-c:a libopus -b:a 64k -frame_duration 60 -c:v libsvtav1 -preset 4 -crf 60 -pix_fmt yuv420p10le -svtav1-params tune=0:film-grain=0 -g {fpsG} -r {fps}";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Result = TextInput.Text;
            DialogResult = true;
        }
    }
}

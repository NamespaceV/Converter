using System.Collections.Generic;
using System.Windows;

namespace Converter.Windows
{
    public partial class TextInputWindow : Window
    {
        public string Result { get; set; }

        public TextInputWindow(string fFMpegParams)
        {
            InitializeComponent();
            var args = new List<string>{
                "-i {input}",
                "-c:a libopus",
                "-b:a 16k",
//                "-b:a 64k",
                "-frame_duration 60",
                "-c:v libsvtav1",
                "-preset 4",
                "-crf 60",
                "-pix_fmt yuv420p10le",
                "-svtav1-params tune=0:film-grain=0",
                "-g 720",
                "-g {fpsG}", // fps * 30 see class CustomParamsBuilder
                "-r {fps}",
                "-nostdin",
                "{output}",
            };
            TextInput.Text = string.IsNullOrWhiteSpace(fFMpegParams) ? string.Join(" ", args) : fFMpegParams;
            //TextInput.Text = "-c:a libopus -b:a 64k -frame_duration 60 -c:v libsvtav1 -preset 4 -crf 60 -pix_fmt yuv420p10le -svtav1-params tune=0:film-grain=0 -g {fpsG} -r {fps}";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Result = TextInput.Text;
            DialogResult = true;
        }

    }
}

using System.Collections.Generic;
using System.Windows;

namespace Converter.Windows
{
    public partial class FfmpegParamsWindow : Window
    {
        public string Result { get; set; }

        public FfmpegParamsWindow(string fFMpegParams)
        {
            InitializeComponent();
           
            TextInput.Text = string.IsNullOrWhiteSpace(fFMpegParams) ? DefaultArgs() : fFMpegParams;
            //TextInput.Text = "-c:a libopus -b:a 64k -frame_duration 60 -c:v libsvtav1 -preset 4 -crf 60 -pix_fmt yuv420p10le -svtav1-params tune=0:film-grain=0 -g {fpsG} -r {fps}";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Result = TextInput.Text;
            DialogResult = true;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            TextInput.Text = DefaultArgs();
        }

        private string DefaultArgs() {
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
            return string.Join(" ", args);
        }
    }
}

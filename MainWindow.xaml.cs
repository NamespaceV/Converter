using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged,  ILogger
    {
        private const string basePath = @"C:\NetworkShare\INPUT30";

        public event PropertyChangedEventHandler PropertyChanged;

        public List<VideoFileVM> Files { get; set; } = new List<VideoFileVM>();
        public string Logs { get; set; } = "asdf";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Files.Add(new VideoFileVM("asd", this));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Files = System.IO.Directory.EnumerateFiles(basePath).Select(f => new VideoFileVM(f, this)).ToList();
            OnPropertyChanged(nameof(Files));
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Log(string s)
        {
            Logs += $"\n{s}";
            OnPropertyChanged(nameof(Logs));
        }

        public void LogSameLine(string s)
        {
            Logs += $"{s}";
            OnPropertyChanged(nameof(Logs));
        }
    }

    public interface ILogger
    {
        void Log(string s);
        void LogSameLine(string s);
    }

    public class VideoFileVM: INotifyPropertyChanged {
        private string path;
        private readonly ILogger logger;
        public string Status { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand ConvertCommand { get; private set; }

        public VideoFileVM(string path, ILogger logger)
        {
            this.path = path;
            this.logger = logger;
            ConvertCommand = new SimpleCommand(Convert);
            Status = "Found";
        }

        private void Convert()
        {
            logger.Log($"Converting {path}");
            Status = "Converting...";
            OnPropertyChanged(nameof(Status));
        }

        public override string ToString() {
            return path;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }

    public class SimpleCommand : ICommand
    {
        private readonly Action lambda;

        public event EventHandler CanExecuteChanged;

        public SimpleCommand(Action lambda)
        {
            this.lambda = lambda;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            lambda.Invoke();
        }
    }
}

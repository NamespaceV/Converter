using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Converter
{
    public static class Constants {
        public static DirectoryInfo baseDir = new DirectoryInfo(@"C:\NetworkShare");
    }

    public partial class MainWindow : Window, INotifyPropertyChanged,  ILogger
    {
        private List<int> supportedFPS = new List<int>() { 30, 60};

        public event PropertyChangedEventHandler PropertyChanged;

        public List<VideoFileVM> Files { get; set; } = new List<VideoFileVM>();
        public string Logs { get; set; } = "asdf";
        public ICommand RefreshCommand { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Files.Add(new VideoFileVM(new FileInfo("asd"),45, this));
            RefreshCommand = new SimpleCommand(RefreshList);
        }

        private void RefreshList()
        {
            var newFiles = new List<VideoFileVM>();
            var oldFiles = Files;
            foreach (var fps in supportedFPS) {
                var files = Constants.baseDir.EnumerateDirectories()
                    .Single(d => d.Name.ToLowerInvariant() == "input" + fps.ToString())
                    .EnumerateFiles();
                foreach (var f in files) {
                    var match = oldFiles.Find(of => of.Matches(f));
                    if (match != null) {
                        newFiles.Add(match);
                    } else {
                        newFiles.Add(new VideoFileVM(f, fps, this));
                    }
                }
            }
            Files = newFiles;
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
        private FileInfo fileInfo;
        private readonly int fps;
        private readonly ILogger logger;
        private Process runningProcess;
        private bool windowHidden;


        public string Status { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand ConvertCommand { get; private set; }
        public SimpleCommand ToggleWindowCommand { get; private set; }

        public VideoFileVM(FileInfo path, int fps, ILogger logger)
        {
            this.fileInfo = path;
            this.fps = fps;
            this.logger = logger;
            ConvertCommand = new SimpleCommand(Convert);
            ToggleWindowCommand = new SimpleCommand(ToggleWindow);
            Status = "Found";
            if (File.Exists(GetOutputFilePath())) {
                Status = "Done";
            }
            ToggleWindowCommand.SetEnabled(false);
        }

        private string GetOutputFilePath() {
            var outDir = Constants.baseDir.EnumerateDirectories().Single(d => d.Name.ToLowerInvariant() == "output").FullName;
            return Path.Combine(outDir, Path.GetFileNameWithoutExtension(fileInfo.Name) + ".webm");
        }

        private void Convert()
        {
            var outName = GetOutputFilePath();
            logger.Log($"Converting {fileInfo.FullName} -> {outName}");
            var binaryPath = Constants.baseDir.GetFiles().Single(f => f.Name == "ffmpeg.exe").FullName;
            var args = $"-i {fileInfo.FullName}"+
                $" -c:a libopus -b:a 64k -c:v libsvtav1 -crf 60"+
                $" -pix_fmt yuv420p10le -g {fps * 50} -preset 4 -svtav1-params tune=0 -r {fps}"+
                $" {outName}";
            var info = new ProcessStartInfo(binaryPath, args);
            runningProcess = Process.Start(info);
            runningProcess.Exited += (e,a) => { OnConvertFinished(); };
            runningProcess.PriorityClass = ProcessPriorityClass.Idle;
            ToggleWindow();
            Status = "Running...";
            ToggleWindowCommand.SetEnabled(true);
            OnPropertyChanged(nameof(Status));
        }

        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private void ToggleWindow() {
            if (runningProcess?.MainWindowHandle == null) {
                logger.Log("TOGGLE - Failed - No handle.");
                return;
            }
            windowHidden = !windowHidden;
            var handle = runningProcess.MainWindowHandle.ToInt32();
            logger.Log($"TOGGLE - windowHidden = {windowHidden} handle = {handle}.");
            ShowWindow(handle, windowHidden ? SW_SHOW : SW_HIDE);
        }

        public void OnConvertFinished() {
            Status = "Done";
            ToggleWindowCommand.SetEnabled(false);
            OnPropertyChanged(nameof(Status));
        }

        public override string ToString() {
            return $"{fps} -> {fileInfo.Name}";
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        internal bool Matches(FileInfo f)
        {
            return fileInfo.FullName == f.FullName;
        }
    }

    public class SimpleCommand : ICommand
    {
        private readonly Action lambda;
        private bool enabled;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public SimpleCommand(Action lambda)
        {
            this.lambda = lambda;
            enabled = true;
        }

        public bool CanExecute(object parameter)
        {
            return enabled;
        }

        public void Execute(object parameter)
        {
            lambda.Invoke();
        }

        public void SetEnabled(bool enabled) {
            if (this.enabled == enabled) return;
            this.enabled = enabled;
        }
    }
}

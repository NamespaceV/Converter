using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Converter
{
    public class VideoFileVM: INotifyPropertyChanged {
        private FileInfo fileInfo;
        private readonly int fps;
        private readonly ILogger logger;
        private Process runningProcess;
        private bool windowHidden;


        public string Status { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SimpleCommand ConvertCommand { get; private set; }
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
                ConvertCommand.SetEnabled(false);
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
            //binaryPath = "notepad";
            //args = "";
            var info = new ProcessStartInfo(binaryPath, args);
            info.RedirectStandardError = true;
            runningProcess = Process.Start(info);
            runningProcess.EnableRaisingEvents = true;
            runningProcess.Exited += (e,a) => { 
                OnConvertFinished(e as Process); };
            runningProcess.PriorityClass = ProcessPriorityClass.Idle;
            Status = "Running...";
            ConvertCommand.SetEnabled(false);
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
            var handle = runningProcess.MainWindowHandle.ToInt32();
            logger.Log($"TOGGLE - windowHidden = {!windowHidden} handle = {handle}.");
            ShowWindow(handle, windowHidden ? SW_SHOW : SW_HIDE);
            windowHidden = !windowHidden;
        }

        public void OnConvertFinished(Process p) {
            if (p.ExitCode == 0) {
                Status = "Done";
                ToggleWindowCommand.SetEnabled(false);
            }
            else
            {
                Status = "Failed";
                logger.Log($"Error: exit code {p.ExitCode} when processing {fileInfo.Name}");
                logger.Log(p.StandardError.ReadToEnd());
                ToggleWindowCommand.SetEnabled(false);
                ConvertCommand.SetEnabled(true);
            }
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
}

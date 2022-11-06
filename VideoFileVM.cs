﻿using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Converter
{
    public enum FileStatus { 
        Found,
        Running,
        Done,
        Failed,
        CorruptedOrNotTracked,
    }

    public class VideoFileVM: INotifyPropertyChanged {
        private FileInfo sourceFI;
        private readonly int fps;
        private readonly ILogger logger;
        private Process runningProcess;
        private bool windowHidden;


        public FileStatus Status { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SimpleCommand ConvertCommand { get; private set; }
        public SimpleCommand ToggleWindowCommand { get; private set; }

        public VideoFileVM(FileInfo source, int fps, ILogger logger)
        {
            this.sourceFI = source;
            this.fps = fps;
            this.logger = logger;
            ConvertCommand = new SimpleCommand(Convert);
            ToggleWindowCommand = new SimpleCommand(ToggleWindow);
            Refresh();
        }

        private FileInfo GetProcessingFileInfo() {
            var outDir = Constants.baseDir.EnumerateDirectories().Single(d => d.Name.ToLowerInvariant() == "processing").FullName;
            return new FileInfo(Path.Combine(outDir, Path.GetFileNameWithoutExtension(sourceFI.Name) + ".webm"));
        }

        private FileInfo GetOutputFileInfo()
        {
            var outDir = Constants.baseDir.EnumerateDirectories().Single(d => d.Name.ToLowerInvariant() == "output").FullName;
            return new FileInfo(Path.Combine(outDir, Path.GetFileNameWithoutExtension(sourceFI.Name) + ".webm"));
        }

        private void Convert()
        {
            var processingFI = GetProcessingFileInfo();
            logger.Log($"Converting {sourceFI.FullName} -> {processingFI.FullName}");
            runningProcess = StartConversionProcess(processingFI);
            Status = FileStatus.Running;
            ConvertCommand.SetEnabled(false);
            ToggleWindowCommand.SetEnabled(true);
            OnPropertyChanged(nameof(Status));
        }

        private Process StartConversionProcess(FileInfo processingFI)
        {
            var binaryPath = Constants.baseDir.GetFiles().Single(f => f.Name == "ffmpeg.exe").FullName;
            var args = $"-i {sourceFI.FullName}" +
                $" -c:a libopus -b:a 64k -c:v libsvtav1 -crf 60" +
                $" -pix_fmt yuv420p10le -g {fps * 50} -preset 4 -svtav1-params tune=0 -r {fps}" +
                $" {processingFI.FullName}";
            var info = new ProcessStartInfo(binaryPath, args);
            var newProcess = Process.Start(info);
            newProcess.EnableRaisingEvents = true;
            newProcess.Exited += (e, a) =>
            {
                OnConvertFinished(e as Process);
            };
            newProcess.PriorityClass = ProcessPriorityClass.Idle;
            return newProcess;
        }

        public void OnConvertFinished(Process p)
        {
            if (p.ExitCode == 0)
            {
                OnProcessingSuccess();
            }
            else
            {
                OnProcessingFailed(p);
            }
        }

        private void OnProcessingFailed(Process p)
        {
            Status = FileStatus.Failed;
            logger.Log($"Error: exit code {p.ExitCode} when processing {sourceFI.Name}");
            ToggleWindowCommand.SetEnabled(false);
            ConvertCommand.SetEnabled(true);
            OnPropertyChanged(nameof(Status));
        }

        private void OnProcessingSuccess()
        {
            Status = FileStatus.Done;
            var destFileInfo = new FileInfo(sourceFI.FullName.Replace("input", "done", System.StringComparison.InvariantCultureIgnoreCase));
            if (!Directory.Exists(destFileInfo.DirectoryName))
            {
                Directory.CreateDirectory(destFileInfo.DirectoryName);
            }
            sourceFI.MoveTo(destFileInfo.FullName);
            GetProcessingFileInfo().MoveTo(GetOutputFileInfo().FullName);
            ToggleWindowCommand.SetEnabled(false);
            OnPropertyChanged(nameof(Status));
        }

        internal void Refresh()
        {
            if (Status == FileStatus.Running) { return; }
            Status = FileStatus.Found;
            ConvertCommand.SetEnabled(true);
            ToggleWindowCommand.SetEnabled(true);
            var outFileInfo = GetProcessingFileInfo();
            if (outFileInfo.Exists)
            {
                if (outFileInfo.Length == 0L)
                {
                    Status = FileStatus.CorruptedOrNotTracked;
                }
                else
                {
                    Status = FileStatus.Done;
                    ConvertCommand.SetEnabled(false);
                }
            }
            ToggleWindowCommand.SetEnabled(false);
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

        public override string ToString() {
            return $"{fps} -> {sourceFI.Name}";
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        internal bool Matches(FileInfo f)
        {
            return sourceFI.FullName == f.FullName;
        }
    }
}

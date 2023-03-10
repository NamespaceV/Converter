using Converter.Basic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Converter.Logic
{
    public enum ConversionStatusEnum { 
        NotStarted,
        Corrupted,
        Unknown
    }

    internal class ConversionProcess
    {
        private readonly FileInfo sourceFI;
        private readonly int fps;
        private readonly ILogger logger;
        private Process runningProcess;
        private bool windowHidden;
        private nint? windowHandle;

        public ConversionProcess(FileInfo sourceFile, int fps, ILogger logger) {
            sourceFI = sourceFile;
            this.fps = fps;
            this.logger = logger;
        }

        public TimeSpan GetVideoDuration()
        {
            var f = TagLib.File.Create(sourceFI.FullName);
            return f.Properties.Duration;
        }


        public FileInfo GetSourceFileInfo()
        {
            return sourceFI;
        }

        public FileInfo GetProcessingFileInfo()
        {
            var outDir = new DirectoryInfo(SettingsProivider.GetBasePath).EnumerateDirectories()
                .Single(d => d.Name.ToLowerInvariant() == "processing").FullName;
            return new FileInfo(Path.Combine(outDir, Path.GetFileNameWithoutExtension(sourceFI.Name) + ".webm"));
        }

        public FileInfo GetOutputFileInfo()
        {
            var outDir = new DirectoryInfo(SettingsProivider.GetBasePath).EnumerateDirectories()
                .Single(d => d.Name.ToLowerInvariant() == "output").FullName;
            return new FileInfo(Path.Combine(outDir, Path.GetFileNameWithoutExtension(sourceFI.Name) + ".webm"));
        }

        public virtual void StartConversionProcess(Action onProcessingSuccess, Action<int?> onProcessingFailed) 
        { 
            if (runningProcess != null && !runningProcess.HasExited) {
                logger.Log($"Ignored!!! Conversion for {sourceFI.Name} already running.");
                return;
            }
            var binaryPath = new DirectoryInfo(SettingsProivider.GetBasePath).GetFiles()
                .Single(f => f.Name == "ffmpeg.exe").FullName;
            //binaryPath = "notepad.exe";
            var args = new List<string>()
            {
                $"-i {sourceFI.FullName}",
                "-c:a libopus",
                "-b:a 64k",
                "-frame_duration 60",
                "-c:v libsvtav1",
                "-preset 4",
                "-crf 60",
                "-pix_fmt yuv420p10le",
                "-svtav1-params tune=0",
                $"-g {fps * 30}",
                $"-r {fps}",
                "-nostdin",
                GetProcessingFileInfo().FullName,
            };
            var info = new ProcessStartInfo(binaryPath, string.Join(" ", args));
            info.UseShellExecute = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            runningProcess = Process.Start(info);
            runningProcess.EnableRaisingEvents = true;
            windowHandle = WindowHelper.GetWindowHandle(runningProcess);
            runningProcess.Exited += (e, a) =>
            {
                windowHandle = null;
                if ((e as Process)?.ExitCode == 0)
                {
                    MoveToDone();
                    onProcessingSuccess();
                }
                else {
                    onProcessingFailed((e as Process)?.ExitCode);
                }
            };
            runningProcess.PriorityClass = ProcessPriorityClass.Idle;
            windowHidden = true;
        }

        private void MoveToDone()
        {
            var destFileInfo = new FileInfo(sourceFI.FullName.Replace("input", "done", StringComparison.InvariantCultureIgnoreCase));
            if (!Directory.Exists(destFileInfo.DirectoryName))
            {
                Directory.CreateDirectory(destFileInfo.DirectoryName);
            }
            sourceFI.MoveTo(destFileInfo.FullName);
            GetProcessingFileInfo().MoveTo(GetOutputFileInfo().FullName);
        }

        internal ConversionStatusEnum CheckStatus()
        {
            if (!GetProcessingFileInfo().Exists)
            {
                return ConversionStatusEnum.NotStarted;
            }
            if (GetProcessingFileInfo().Length == 0L)
            {
                return ConversionStatusEnum.Corrupted;
            }
            return ConversionStatusEnum.Unknown;
        }

        public void ToggleWindow() {
            if (runningProcess?.HasExited != false) {
                logger.Log("TOGGLE - Failed - No runningProcess.");
                return;
            }
            if (!windowHandle.HasValue) {
                windowHandle = WindowHelper.GetWindowHandle(runningProcess);
                if (!windowHandle.HasValue)
                {
                    logger.Log("TOGGLE - Failed - No windowHandle.");
                    return;
                }
            }
            var handle = windowHandle.Value.ToInt32();
            logger.Log($"TOGGLE - windowHidden -> {!windowHidden} handle = {handle}.");
            WindowHelper.ShowWindow(handle, windowHidden ? WindowHelper.ShowWindowEnum.Show : WindowHelper.ShowWindowEnum.Hide);
            windowHidden = !windowHidden;
        }

        public static void ShowProgressDir() {
            var outDir = new DirectoryInfo(SettingsProivider.GetBasePath).EnumerateDirectories()
               .Single(d => d.Name.ToLowerInvariant() == "processing").FullName;
            Process.Start("explorer.exe", outDir);
        }
    }
}

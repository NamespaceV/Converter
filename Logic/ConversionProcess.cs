﻿using Converter.Basic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
            var outDir = Constants.baseDir.EnumerateDirectories().Single(d => d.Name.ToLowerInvariant() == "processing").FullName;
            return sourceFI;
        }

        public FileInfo GetProcessingFileInfo()
        {
            var outDir = Constants.baseDir.EnumerateDirectories().Single(d => d.Name.ToLowerInvariant() == "processing").FullName;
            return new FileInfo(Path.Combine(outDir, Path.GetFileNameWithoutExtension(sourceFI.Name) + ".webm"));
        }

        public FileInfo GetOutputFileInfo()
        {
            var outDir = Constants.baseDir.EnumerateDirectories().Single(d => d.Name.ToLowerInvariant() == "output").FullName;
            return new FileInfo(Path.Combine(outDir, Path.GetFileNameWithoutExtension(sourceFI.Name) + ".webm"));
        }

        public void StartConversionProcess(Action onProcessingSuccess, Action<int?> onProcessingFailed) 
        { 
            if (runningProcess != null) {
                logger.Log($"Ignored!!! Conversion for {sourceFI.Name} already running.");
                return;
            }
            var binaryPath = Constants.baseDir.GetFiles().Single(f => f.Name == "ffmpeg.exe").FullName;
            //binaryPath = "notepad.exe";
            var args = $"-i {sourceFI.FullName}" +
                $" -c:a libopus -b:a 64k -c:v libsvtav1 -crf 60" +
                $" -pix_fmt yuv420p10le -g {fps * 50} -preset 4 -svtav1-params tune=0 -r {fps}" +
                $" {GetProcessingFileInfo().FullName}";
            var info = new ProcessStartInfo(binaryPath, args);
            runningProcess = Process.Start(info);
            runningProcess.EnableRaisingEvents = true;
            runningProcess.Exited += (e, a) =>
            {
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
            windowHidden = false;
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
            if (runningProcess?.MainWindowHandle == null)
            {
                logger.Log("TOGGLE - Failed - No handle.");
                return;
            }
            var handle = runningProcess.MainWindowHandle.ToInt32();
            logger.Log($"TOGGLE - windowHidden = {!windowHidden} handle = {handle}.");
            WindowHelper.ShowWindow(handle, windowHidden ? WindowHelper.ShowWindowEnum.Show : WindowHelper.ShowWindowEnum.Hide);
            windowHidden = !windowHidden;
        }
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;
using System;
using Converter.Basic;
using Converter.Logic;
using System.Windows;
using System.Windows.Threading;

namespace Converter.ViewModels
{
    public enum FileStatus
    {
        Found,
        Running,
        Done,
        Failed,
        Corrupted,
        Unknown,
    }

    public class VideoFileVM : INotifyPropertyChanged
    {
        private ConversionProcess conversion;
        private readonly int fps;
        private readonly ILogger logger;

        private bool inQueue;
        public bool InQueue { get => inQueue; set { inQueue = value; OnPropertyChanged(nameof(InQueue)); }}
        public FileStatus Status { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? Finish { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SimpleCommand ConvertCommand { get; private set; }
        public SimpleCommand ToggleWindowCommand { get; private set; }

        public VideoFileVM(FileInfo source, int fps, ILogger logger)
        {
            conversion = SettingsProivider.UseFakeConversion
                ? new ConversionProcessFake(source, fps, logger)
                : new ConversionProcess(source, fps, logger);
            this.fps = fps;
            this.logger = logger;
            Duration = conversion.GetVideoDuration();
            ConvertCommand = new SimpleCommand(Convert);
            ToggleWindowCommand = new SimpleCommand(() => conversion.ToggleWindow());
            Refresh();
        }

        private void Convert()
        {
            logger.Log($"Converting {conversion.GetSourceFileInfo().FullName} -> {conversion.GetProcessingFileInfo().FullName}");
            conversion.StartConversionProcess(OnProcessingSuccess, OnProcessingFailed);
            Status = FileStatus.Running;
            Start = DateTimeOffset.Now;
            Finish = null;
            ConvertCommand.SetEnabled(false);
            ToggleWindowCommand.SetEnabled(true);
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Start));
            OnPropertyChanged(nameof(Finish));
        }

        private void OnProcessingFailed(int? exitCode)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                Status = FileStatus.Failed;
                Finish = DateTimeOffset.Now;

                logger.Log($"Error: exit code {exitCode} when processing {conversion.GetSourceFileInfo().Name}");
                ToggleWindowCommand.SetEnabled(false);
                ConvertCommand.SetEnabled(true);

                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Finish));
            }));
        }

        private void OnProcessingSuccess()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                Status = FileStatus.Done;
                Finish = DateTimeOffset.Now;

                logger.Log($"Finished processing {conversion.GetSourceFileInfo().Name}");
                ToggleWindowCommand.SetEnabled(false);
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Finish));
            }));

        }

        internal void Refresh()
        {
            if (Status == FileStatus.Running) { return; }

            switch (conversion.CheckStatus())
            {
                case ConversionStatusEnum.NotStarted:
                    Status = FileStatus.Found;
                    ConvertCommand.SetEnabled(true);
                    ToggleWindowCommand.SetEnabled(false);
                    break;
                case ConversionStatusEnum.Corrupted:
                    Status = FileStatus.Unknown;
                    ConvertCommand.SetEnabled(true);
                    ToggleWindowCommand.SetEnabled(false);
                    break;
                case ConversionStatusEnum.Unknown:
                    Status = FileStatus.Done;
                    ConvertCommand.SetEnabled(false);
                    ToggleWindowCommand.SetEnabled(false);
                    break;
            }
        }

        public override string ToString()
        {
            return $"{fps} -> {conversion.GetSourceFileInfo().Name}";
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        internal bool Matches(FileInfo f)
        {
            return conversion.GetSourceFileInfo().FullName == f.FullName;
        }

        internal void ToggleInQueue()
        {
            InQueue = !InQueue;
            OnPropertyChanged(nameof(InQueue));
        }
    }
}

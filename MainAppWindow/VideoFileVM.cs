using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System;
using Converter.Basic;
using Converter.Logic;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Linq;

namespace Converter
{
    public enum FileStatus
    {
        Found,
        Running,
        Done,
        Failed,
        Corrupted,
        Unknown,
        IsDirectory,
    }

    public class VideoFileVM : INotifyPropertyChanged
    {
        private bool isNodeExpanded = true;
        public bool IsNodeExpanded
        {
            get { return isNodeExpanded; }
            set { isNodeExpanded = value; OnPropertyChanged(); }
        }
        public Visibility DetailsVisible { get => isDir ? Visibility.Collapsed : Visibility.Visible; }

        public ObservableCollection<VideoFileVM> Children { get; set; } = new ObservableCollection<VideoFileVM>();
        private ConversionProcess conversion;
        private bool isDir = false;
        private readonly FileInfo source;
        private readonly DirectoryInfo dir;
        public int Fps { get; }
        private readonly ILogger logger;

        private bool inQueue;
        public bool InQueue { get => inQueue; set { SetInQueue(value); OnPropertyChanged(nameof(InQueue)); } }
        public bool IsSelected { get; set; }
        public FileStatus Status { get; set; }
        public TimeSpan? Duration { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? Finish { get; set; }
        public TimeSpan? Took { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        public SimpleCommand ConvertCommand { get; private set; }
        public SimpleCommand ToggleWindowCommand { get; private set; }

        public VideoFileVM(ConversionFactory conversionFactory, FileInfo source, int fps, ILogger logger)
        {
            conversion = conversionFactory.CreateConversionFor(source, fps, logger);
            this.source = source;
            this.Fps = fps;
            OnPropertyChanged(nameof(Fps));
            this.logger = logger;
            ConvertCommand = new SimpleCommand(Convert);
            ToggleWindowCommand = new SimpleCommand(() => conversion.ToggleWindow());
            Refresh();
        }

        public VideoFileVM(DirectoryInfo source, ILogger logger)
        {
            this.logger = logger;
            isDir = true;
            Status = FileStatus.IsDirectory;
            this.dir = source;
        }

        private void Convert()
        {
            CheckVideoDuration();

            logger.Log($"Converting {conversion.GetSourceFileInfo().FullName} -> {conversion.GetProcessingFileInfo().FullName}");
            conversion.StartConversionProcess(OnProcessingSuccess, OnProcessingFailed);
            Status = FileStatus.Running;
            Start = DateTimeOffset.Now;
            Finish = null;
            ConvertCommand.SetEnabled(false);
            ToggleWindowCommand.SetEnabled(true);

            OnPropertyChanged(nameof(Duration));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Start));
            OnPropertyChanged(nameof(Finish));
        }


        public void CheckVideoDuration()
        {
            if (Duration.HasValue)
            {
                return;
            }
            try
            {
                using var f = TagLib.File.Create(source.FullName);
                Duration = f.Properties.Duration;
            }
            catch { }
        }

        private void OnProcessingFailed(int? exitCode)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                Status = FileStatus.Failed;
                Finish = DateTimeOffset.Now;
                Took = Finish - Start;

                logger.Log($"Error: exit code {exitCode} when processing {conversion.GetSourceFileInfo().Name}");
                ToggleWindowCommand.SetEnabled(false);
                ConvertCommand.SetEnabled(true);

                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Finish));
                OnPropertyChanged(nameof(Took));
            }));
        }

        private void OnProcessingSuccess()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                Status = FileStatus.Done;
                Finish = DateTimeOffset.Now;
                Took = Finish - Start;

                logger.Log($"Finished processing {conversion.GetSourceFileInfo().Name}");
                logger.Log($"Took: {Took}");
                ToggleWindowCommand.SetEnabled(false);
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Finish));
                OnPropertyChanged(nameof(Took));
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

        public string Name { get => ToString(); }

        public override string ToString()
        {
            if (isDir) {
                return dir.Name;
            }
            return $"{conversion.GetSourceFileInfo().Name}";
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        internal bool Matches(FileInfo f)
        {
            if (isDir) return false;
            return conversion.GetSourceFileInfo().FullName == f.FullName;
        }

        internal void ToggleInQueue()
        {
            InQueue = !InQueue;
        }

        internal void SetInQueue(bool b) {
            inQueue = b;
            foreach (var c in Children)
            {
                c.InQueue  = b;
            }
        }

        internal bool TryFind(SourceFile f, out VideoFileVM match)
        {
            if (Matches(f.FileInfo))
            {
                match = this;
                return true;
            }
            foreach (var c in Children){
                if (c.TryFind(f, out match))
                    return true;
            }
            match = null;
            return false;
        }

        internal bool IsFor(DirectoryInfo dir)
        {
            return this.dir?.FullName == dir.FullName;
        }

        internal VideoFileVM GetNextReadyFile()
        {
            if (InQueue && Status == FileStatus.Found) {
                return this;
            }
            foreach (var c in Children) {
                var r = c.GetNextReadyFile();
                if (r != null) return r;
            }
            return null;
        }

        internal int GetFilesCount()
        {
            var cnt = isDir ? 0 : 1;
            return cnt + Children.ToList().Sum(c => c.GetFilesCount());
        }

        internal int GetActiveCount()
        {
            var cnt = Status == FileStatus.Running ? 1 : 0;
            return cnt + Children.ToList().Sum(c => c.GetActiveCount());
        }

        internal TimeSpan GetActiveDuration()
        {
            var inQueueDuration = new TimeSpan();
            if (Duration.HasValue && Status == FileStatus.Running)
            {
                inQueueDuration += Duration.Value;
            }
            return inQueueDuration + Children.Aggregate(new TimeSpan(), (a, b) => a + b.GetActiveDuration()); ;
        }

        internal decimal GetInQueueCount()
        {
            var cnt = InQueue && (Status == FileStatus.Running || Status == FileStatus.Found) ? 1 : 0;
            return cnt + Children.ToList().Sum(c => c.GetInQueueCount());
        }

        internal TimeSpan GetInQueueDuration()
        {
            var inQueueDuration = new TimeSpan();
            if (Duration.HasValue && InQueue && (Status == FileStatus.Running || Status == FileStatus.Found)) {
                inQueueDuration += Duration.Value;
            }
            return inQueueDuration + Children.Aggregate(new TimeSpan(), (a, b) => a + b.GetInQueueDuration()); ;
        }

        /// <summary>
        /// returns number of active files in the subtree after the operation
        /// </summary>
        /// <param name="cnt"></param>
        /// <returns></returns>
        internal int TakeTop(int cnt)
        {
            if (cnt == 0) {
                InQueue = false;
                return 0;
            }
            var f = GetFilesCount();
            if (f <= cnt)
            {
                InQueue = true;
                return f;
            }
            if (!isDir) {
                InQueue = true;
                return 1;
            }
            var r = 0;
            foreach (var c in Children)
            {
                var a = c.TakeTop(cnt);
                cnt -= a;
                r += a;
            }
            return r;
        }
    }
}

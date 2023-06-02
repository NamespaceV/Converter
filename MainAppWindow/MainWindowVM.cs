using Converter.Basic;
using Converter.icon;
using Converter.Logic;
using Converter.Windows;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Converter
{
    public class MainWindowVM : BindableBase, ILogger
    {
        private IconSwitcher switcher;

        public ObservableCollection<VideoFileVM> Files { get; set; } = new ObservableCollection<VideoFileVM>();

        public string Logs { get; set; } = "";
        public string Summary { get; set; }
        private string fileFps;
        public string FileFPS
        {
            get { return fileFps; }
            set {
                SetProperty(ref fileFps, value); 
                RefreshList();
            }
        }
        public string WorkingBasePath { get; set; }
        public ICommand AddFilesCommand { get; private set; }
        public ICommand SetParamsCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ShowDirCommand { get; private set; }
        public ICommand TakeTopCommand { get; private set; }
        public ICommand ToggleAllCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }
        public bool QueueActive { get; set; } = true;
        public ILogger ExtraLogger { get; set; }
        public ConversionFactory ConversionFactory { get; }
        public IFileLister FileLister { get; }
        public IBaseModel BaseModel { get; }

        public MainWindowVM(
            IFileLister fileLister,
            IBaseModel baseModel,
            FileLogger logger,
            ConversionFactory conversionFactory)
        {
            AddFilesCommand = new SimpleCommand(AddFiles);
            SetParamsCommand = new SimpleCommand(SetParams);
            RefreshCommand = new SimpleCommand(RefreshList);
            AboutCommand = new SimpleCommand(ShowAbout);
            TakeTopCommand = new SimpleCommand(TakeTopX);
            ToggleAllCommand = new SimpleCommand(ToggleAll);
            ShowDirCommand = new SimpleCommand(() => ConversionProcess.ShowProgressDir(baseModel));
            ExtraLogger = logger;
            ConversionFactory = conversionFactory;
            FileLister = fileLister;
            BaseModel = baseModel;
            RefreshList();
        }

        internal void SetIconSwitcher(IconSwitcher switcher)
        {
            this.switcher = switcher;
        }

        private void ShowAbout()
        {
            var aboutText = "Video format converter (opinionated ffmpeg runner)" + "\n\"Replace icon\" by Icons8 (icons8.com)";
            MessageBox.Show(aboutText, "About");
        }

        private void TakeTopX() {
            var popup = new Windows.TakeTopXPopup(this);
            popup.Show();
        }

        internal void OnClosing(CancelEventArgs e)
        {
            if (!Files.Any(f => f.Status == FileStatus.Running)) {
                return;
            }
            var result = MessageBox.Show(
                "There is a conversion running! Do you really want to close?",
                "Do you really want to close?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
            else
            {
                ExtraLogger?.Log("App terminated while job was running.");
            }
        }

        private void AddFiles()
        {
            using var d = new System.Windows.Forms.FolderBrowserDialog();
            var r = d.ShowDialog();
            if (r != System.Windows.Forms.DialogResult.OK) {
                return;
            }
            WorkingBasePath = d.SelectedPath;
            RefreshList();
        }

        private void SetParams()
        {
            var curArgs = BaseModel.ConversionCommandParamsBuilder().args;
            var defArgs = BaseModel.ConversionCommandParamsBuilder().DefArgs();
            var d = new FfmpegParamsWindow(curArgs, defArgs);
            var r = d.ShowDialog();
            if (r != true)
            {
                return;
            }
            BaseModel.ConversionCommandParamsBuilder().args = d.Result;
        }

        private void UpdateFilterDir()
        {
            if (int.TryParse(FileFPS, out var fpsInt))
            {
                FileLister.SetFps(fpsInt);
            }
            if (!string.IsNullOrWhiteSpace(WorkingBasePath)) {
                var dir = new System.IO.DirectoryInfo(WorkingBasePath);
                FileLister.SetDirectory(dir);
            }
        }

        private void RefreshList()
        {
            UpdateFilterDir();
            var newFiles = new ObservableCollection<VideoFileVM>();
            var oldFiles = Files;

            var filesList = FileLister.ListFiles();
            foreach (var f in filesList)
            {
                VideoFileVM match = null;
                foreach (var init in oldFiles) {
                    if (init.TryFind(f, out match)){
                        break;
                    }
                }
                if (match != null)
                {
                    match.Refresh();
                    Add(newFiles, match, f.DirPath);
                }
                else
                {
                    var newvfVM = new VideoFileVM(ConversionFactory, f.FileInfo, f.Fps, this);
                    newvfVM.PropertyChanged -= (o, e) => UpdateSummary();
                    newvfVM.PropertyChanged += (o, e) => UpdateSummary();
                    Add(newFiles, newvfVM, f.DirPath);
                }
            }
            Files = newFiles;
            RaisePropertyChanged(nameof(Files));
            UpdateSummary();
        }

        private void Add(ObservableCollection<VideoFileVM> files, VideoFileVM match, List<DirectoryInfo> dirs)
        {
            foreach (var dir in dirs) {
                var dVM = files.ToList().Find(vm => vm.IsFor(dir));
                if (dVM == null) {
                    dVM = new VideoFileVM(dir, this);
                    files.Add(dVM);
                }
                files = dVM.Children;
            }
            files.Add(match);
        }

        private void UpdateSummary()
        {
            var total = Files.Sum(f => f.GetFilesCount());

            int activeCount = Files.Sum(f => f.GetActiveCount());
            var activeDuration = Files.Aggregate(new TimeSpan(), (a, b) => a + b.GetActiveDuration());

            var inQueue = Files.Sum(f => f.GetInQueueCount());
            var inQueueDuration = Files.Aggregate(new TimeSpan(), (a, b) => a + b.GetInQueueDuration());

            Summary =
                  $"{total} files total"
                + $" || In queue {inQueue} -> {inQueueDuration.ToString("hh\\:mm\\ \\(ss\\)")} total length"
                + $" || Running: {activeCount} -> {activeDuration.ToString("hh\\:mm\\ \\(ss\\)")} total length";
            switcher?.SetActiveIcon(activeCount > 0);
            if (QueueActive && activeCount == 0) {
                var next = FindNextReadyFileVM();
                next?.ConvertCommand.Execute(null);
            }
            RaisePropertyChanged(nameof(Summary));
        }

        private VideoFileVM FindNextReadyFileVM()
        {
            var f = Files.FirstOrDefault(f => f.GetNextReadyFile() != null);
            return f?.GetNextReadyFile();
        }

        public void Log(string s)
        {
            if (!string.IsNullOrEmpty(Logs)) {
                Logs += "\n";
            }
            Logs += $"[{DateTime.Now.ToString("HH:mm")}] {s}";
            RaisePropertyChanged(nameof(Logs));
            ExtraLogger?.Log(s);
        }

        public void LogSameLine(string s)
        {
            Logs += $"{s}";
            RaisePropertyChanged(nameof(Logs));
            ExtraLogger?.LogSameLine(s);
        }

        internal void ToggleSelected()
        {
            var selected = Files.Where(f => f.IsSelected);
            var allInQueue = selected.All(selected=> selected.InQueue);
            foreach (var f in selected.Where(f => f.InQueue == allInQueue))
            {
                f.ToggleInQueue();
            }
        }

        internal void ToggleAll()
        {
            var selected = Files;
            var allInQueue = selected.All(selected => selected.InQueue);
            foreach (var f in selected.Where(f => f.InQueue == allInQueue))
            {
                f.ToggleInQueue();
            }
        }

        internal void TakeTop(int cnt)
        {
            foreach (var f in Files) {
                cnt -= f.TakeTop(cnt);
            }
        }
    }
}

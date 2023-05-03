using Converter.Basic;
using Converter.icon;
using Converter.Logic;
using Converter.Windows;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Converter.ViewModels
{
    public class MainWindowVM : BindableBase, ILogger
    {
        private IconSwitcher switcher;

        public List<VideoFileVM> Files { get; set; } = new List<VideoFileVM>();

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
        public string FFMpegParams { get; set; }
        public ICommand AddFilesCommand { get; private set; }
        public ICommand SetParamsCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ShowDirCommand { get; private set; }
        public ICommand TakeTopCommand { get; private set; }
        public ICommand ToggleAllCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }
        public bool QueueActive { get; set; } = true;
        public ILogger ExtraLogger { get; set; }
        public IFileLister FileLister { get; }

        public MainWindowVM(IFileLister fileLister)
        {
            AddFilesCommand = new SimpleCommand(AddFiles);
            SetParamsCommand = new SimpleCommand(SetParams);
            RefreshCommand = new SimpleCommand(RefreshList);
            AboutCommand = new SimpleCommand(ShowAbout);
            TakeTopCommand = new SimpleCommand(TakeTopX);
            ToggleAllCommand = new SimpleCommand(ToggleAll);
            ShowDirCommand = new SimpleCommand(() => ConversionProcess.ShowProgressDir());
            ExtraLogger = new FileLogger();
            FileLister = fileLister;
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
            var d = new TextInputWindow();
            var r = d.ShowDialog();
            if (r != true)
            {
                return;
            }
            FFMpegParams = d.Result;
        }

        private void UpdateFilterDir()
        {
            if (!int.TryParse(FileFPS, out var fpsInt))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(WorkingBasePath)) {
                return;
            }
            var dir = new System.IO.DirectoryInfo(WorkingBasePath);
            FileLister.SetDirectoryAndFps(dir, fpsInt);
        }

        private void RefreshList()
        {
            UpdateFilterDir();
            var newFiles = new List<VideoFileVM>();
            var oldFiles = Files;
            foreach (var f in FileLister.ListFiles())
            {
                var match = oldFiles.Find(of => of.Matches(f.FileInfo));
                if (match != null)
                {
                    newFiles.Add(match);
                    match.Refresh();
                }
                else
                {
                    newFiles.Add(new VideoFileVM(FileLister, f.FileInfo, f.Fps, this));
                }
            }
            Files = newFiles;
            foreach (var f in newFiles) {
                f.PropertyChanged += (o, e) => UpdateSummary();
            }
            RaisePropertyChanged(nameof(Files));
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            int activeCount = Files.Count(f => f.Status == FileStatus.Running);
            var inQueue = Files.Where(f => f.InQueue && (f.Status == FileStatus.Running || f.Status == FileStatus.Found));

            Summary =
                  $"{Files.Count} files -> {Files.Select(f => f.Duration).Aggregate(new TimeSpan(), (a, b) => a.Add(b)).ToString("hh\\:mm\\ \\(ss\\)")} total length"
                + $" || In queue {inQueue.Count()} -> {inQueue.Select(f => f.Duration).Aggregate(new TimeSpan(), (a, b) => a.Add(b)).ToString("hh\\:mm\\ \\(ss\\)")} total length"
                + $" || Running: {activeCount}";
            switcher?.SetActiveIcon(activeCount > 0);
            if (QueueActive && activeCount == 0) {
                var next = Files.FirstOrDefault(f => f.InQueue && f.Status == FileStatus.Found);
                next?.ConvertCommand.Execute(null);
            }
            RaisePropertyChanged(nameof(Summary));
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
            foreach (var f in Files.Take(cnt)) { f.InQueue = true; }
            foreach (var f in Files.Skip(cnt)) { f.InQueue = false; }
        }

    }
}

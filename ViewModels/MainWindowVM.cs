using Converter.Basic;
using Converter.icon;
using Converter.Logic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Converter.ViewModels
{
    public class MainWindowVM : INotifyPropertyChanged, ILogger
    {
        private readonly IconSwitcher switcher;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<VideoFileVM> Files { get; set; } = new List<VideoFileVM>();
        public string Logs { get; set; } = "";
        public string Summary { get; set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ShowDirCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }
        public bool QueueActive { get; set; }
        public ILogger ExtraLogger { get; set; }

        public MainWindowVM(IconSwitcher switcher)
        {
            RefreshCommand = new SimpleCommand(RefreshList);
            AboutCommand = new SimpleCommand(ShowAbout);
            ShowDirCommand = new SimpleCommand(() => ConversionProcess.ShowProgressDir());
            ExtraLogger = new FileLogger();
            RefreshList();
            this.switcher = switcher;
        }

        private void ShowAbout()
        {
            var aboutText = "Video format converter (opinionated ffmpeg runner)" + "\n\"Replace icon\" by Icons8 (icons8.com)";
            MessageBox.Show(aboutText, "About");
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

        private void RefreshList()
        {
            var newFiles = new List<VideoFileVM>();
            var oldFiles = Files;
            foreach (var f in FileListHelper.ListInputFiles())
            {
                var match = oldFiles.Find(of => of.Matches(f.FileInfo));
                if (match != null)
                {
                    newFiles.Add(match);
                    match.Refresh();
                }
                else
                {
                    newFiles.Add(new VideoFileVM(f.FileInfo, f.Fps, this));
                }
            }
            Files = newFiles;
            foreach (var f in newFiles) {
                f.PropertyChanged += (o, e) => UpdateSummary();
            }
            OnPropertyChanged(nameof(Files));
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
            OnPropertyChanged(nameof(Summary));
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Log(string s)
        {
            if (!string.IsNullOrEmpty(Logs)) {
                Logs += "\n";
            }
            Logs += $"[{DateTime.Now.ToString("HH:mm")}] {s}";
            OnPropertyChanged(nameof(Logs));
            ExtraLogger?.Log(s);
        }

        public void LogSameLine(string s)
        {
            Logs += $"{s}";
            OnPropertyChanged(nameof(Logs));
            ExtraLogger?.LogSameLine(s);
        }

        internal void OnListKeyDown(KeyEventArgs e, int selectedIndex)
        {
            if (e.Key == Key.Space && selectedIndex >= 0) {
                Files[selectedIndex].ToggleInQueue();
            }
        }
    }
}

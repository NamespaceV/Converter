using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Converter
{
    public class MainWindowVM : INotifyPropertyChanged, ILogger
    {
        private List<int> supportedFPS = new List<int>() { 30, 60 };

        public event PropertyChangedEventHandler PropertyChanged;

        public List<VideoFileVM> Files { get; set; } = new List<VideoFileVM>();
        public string Logs { get; set; } = "Logs:";
        public ICommand RefreshCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }

        public MainWindowVM() 
        {
            RefreshCommand = new SimpleCommand(RefreshList);
            AboutCommand = new SimpleCommand(ShowAbout);
            RefreshList();
        }

        private void ShowAbout()
        {
            var aboutText = "Video format converter (opinionated ffmpeg runner)" + "\n\"Replace icon\" by Icons8 (icons8.com)";
            MessageBox.Show(aboutText, "About");
        }

        private void RefreshList()
        {
            var newFiles = new List<VideoFileVM>();
            var oldFiles = Files;
            foreach (var fps in supportedFPS)
            {
                var files = Constants.baseDir.EnumerateDirectories()
                    .Single(d => d.Name.ToLowerInvariant() == "input" + fps.ToString())
                    .EnumerateFiles();
                foreach (var f in files)
                {
                    var match = oldFiles.Find(of => of.Matches(f));
                    if (match != null)
                    {
                        newFiles.Add(match);
                        match.Refresh();
                    }
                    else
                    {
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
}

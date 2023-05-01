using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Converter.Logic
{
    public interface IFileLister
    {
        bool SetDirectoryAndFps(DirectoryInfo directory, int fps);

        List<SourceFile> ListFiles();
        FileInfo GetBinaryFileFFMPG();
        DirectoryInfo GetProcessingDirectory();
        DirectoryInfo GetOutputDirectory();
    }

    public class FileLister : IFileLister
    {
        private DirectoryInfo dir;
        private int fps;

        public FileInfo GetBinaryFileFFMPG()
        {
            return new DirectoryInfo(SettingsProivider.GetBasePath).GetFiles()
                 .Single(f => f.Name == "ffmpeg.exe");
        }

        public DirectoryInfo GetProcessingDirectory()
        {
            if (dir == null) {
                return new DirectoryInfo(SettingsProivider.GetBasePath)
                    .EnumerateDirectories()
                    .Single(d => d.Name.ToLowerInvariant() == "processing");
            }
            return dir.EnumerateDirectories()
                .Single(d => d.Name.ToLowerInvariant() == "processing");
        }

        public DirectoryInfo GetOutputDirectory()
        {
            if (dir == null)
            {
                return new DirectoryInfo(SettingsProivider.GetBasePath)
                    .EnumerateDirectories()
                    .Single(d => d.Name.ToLowerInvariant() == "output");
            }
            return dir.EnumerateDirectories()
                .Single(d => d.Name.ToLowerInvariant() == "output");
        }

        public List<SourceFile> ListFiles()
        {
            if (dir == null || fps == 0)
            {
                return ListInputFilesDefault();
            }

            var result = new List<SourceFile>();

            result = dir
                .EnumerateDirectories()
                .Single(d => d.Name.ToLowerInvariant() == "input" + fps.ToString())
                .EnumerateFiles()
                .Select(f => new SourceFile() { Fps = fps, FileInfo = f })
                .ToList();

            return result;
        }

        public bool SetDirectoryAndFps(DirectoryInfo directory, int fps)
        {
            dir = directory;
            this.fps = fps;
            return true;
        }

        private List<SourceFile> ListInputFilesDefault()
        {
            var supportedFPS = new List<int>() { 24, 30, 60 };

            var result = new List<SourceFile>();
            foreach (var fps in supportedFPS)
            {

                var files = new DirectoryInfo(SettingsProivider.GetBasePath)
                    .EnumerateDirectories()
                    .Single(d => d.Name.ToLowerInvariant() == "input" + fps.ToString())
                    .EnumerateFiles()
                    .Select(f => new SourceFile() { Fps = fps, FileInfo = f });
                result.AddRange(files);
            }
            return result;
        }

    }
}
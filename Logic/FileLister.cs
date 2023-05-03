using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Converter.Logic
{
    public interface IFileLister
    {
        bool SetDirectory(DirectoryInfo directory);
        bool SetFps(int? fps);

        List<SourceFile> ListFiles();
        FileInfo GetBinaryFileFFMPG();
        DirectoryInfo GetProcessingDirectory();
        DirectoryInfo GetOutputDirectory();
    }

    public class FileLister : IFileLister
    {
        private int? fps;

        private IBaseModel BaseModel { get; }

        public FileLister(IBaseModel baseModel)
        {
            BaseModel = baseModel;
        }

        public FileInfo GetBinaryFileFFMPG()
        {
            return BaseModel.FfmpegBinary;
        }

        public DirectoryInfo GetProcessingDirectory()
        {
            return BaseModel.DataFilesDir.EnumerateDirectories()
                .Single(d => d.Name.ToLowerInvariant() == "processing");
        }

        public DirectoryInfo GetOutputDirectory()
        {
            return BaseModel.DataFilesDir.EnumerateDirectories()
               .Single(d => d.Name.ToLowerInvariant() == "output");
        }

        public List<SourceFile> ListFiles()
        {
            var supportedFPS = new List<int>() { 24, 30, 60 };
            if (fps.HasValue && !supportedFPS.Contains(fps.Value))
            {
                supportedFPS.Add(fps.Value);
            }

            var result = new List<SourceFile>();
            foreach (var fps in supportedFPS)
            {

                var files = BaseModel.DataFilesDir
                    .EnumerateDirectories()
                    .SingleOrDefault(d => d.Name.ToLowerInvariant() == "input" + fps.ToString())
                    ?.EnumerateFiles()
                    .Select(f => new SourceFile() { Fps = fps, FileInfo = f });
                if (files != null) {
                    result.AddRange(files);
                }
            }
            return result;
        }

        public bool SetDirectory(DirectoryInfo directory)
        {
            BaseModel.DataFilesDir = directory;
            return true;
        }

        public bool SetFps(int? fps )
        {
            this.fps = fps;
            return true;
        }
    }
}
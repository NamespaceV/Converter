using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Converter.Logic
{
    class SourceFile {
        public FileInfo FileInfo;
        public int Fps;
    }

    internal class FileListHelper
    {
        private static List<int> supportedFPS = new List<int>() { 24, 30, 60 };

        internal static List<SourceFile> ListInputFiles()
        {
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

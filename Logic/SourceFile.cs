using System.Collections.Generic;
using System.IO;

namespace Converter.Logic
{
    public class SourceFile {
        public FileInfo FileInfo;
        public List<DirectoryInfo> DirPath = new List<DirectoryInfo>();
        public int Fps;
    }
}

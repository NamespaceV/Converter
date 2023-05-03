using System;
using System.IO;
using Converter.Basic;

namespace Converter.Logic
{
    public class FileLogger : ILogger
    {
        Lazy<StreamWriter> _fileStream;

        public FileLogger(IBaseModel baseModel)
        {
            _fileStream = new Lazy<StreamWriter>(() =>
            {
                var fi = new FileInfo(Path.Combine(baseModel.LogFilesDir.FullName,
                    $"{DateTime.Now.ToString("yyyy-dd-MM--HH-mm-ss-fff")}-logs.txt"));
                return new StreamWriter(fi.FullName);
            });
        }

        public void Log(string s)
        {
            s = $"[{DateTime.Now.ToString("yyyy-dd-MM--HH-mm-ss-fff")}] " + s;
            if (_fileStream.Value.BaseStream.Position != 0) {
                s = "\n"+s;
            }
            _fileStream.Value.Write(s);
            _fileStream.Value.Flush();
        }

        public void LogSameLine(string s)
        {
            _fileStream.Value.Write(s);
            _fileStream.Value.Flush();
        }
    }
}

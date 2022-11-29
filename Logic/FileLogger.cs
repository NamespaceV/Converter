using System;
using System.IO;
using Converter.Basic;

namespace Converter.Logic
{
    internal class FileLogger : ILogger
    {
        Lazy<StreamWriter> _fileStream = new Lazy<StreamWriter>(createStream);

        private static StreamWriter createStream() {
            var path = Path.Combine(SettingsProivider.GetBasePath, "Logs", $"{DateTime.Now.ToString("yyyy-dd-MM--HH-mm-ss-fff")}-logs.txt");
            return new StreamWriter(path);
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

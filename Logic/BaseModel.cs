using System;
using System.Collections.Generic;
using System.IO;

namespace Converter.Logic
{
    public interface IBaseModel
    {
        Func<IConversionParamsBuilder> ConversionCommandParamsBuilder { get; set; }
        DirectoryInfo DataFilesDir { get; set; }
        FileInfo FfmpegBinary { get; set; }
        DirectoryInfo LogFilesDir { get; set; }
    }

    internal class BaseModel : IBaseModel
    {
        public Func<IConversionParamsBuilder> ConversionCommandParamsBuilder { get; set; }
            = () => new ConversionParamsBuilder();
        public FileInfo FfmpegBinary { get; set; } = new FileInfo(@"C:\NetworkShare\ffmpeg.exe");
        public DirectoryInfo DataFilesDir { get; set; } = new DirectoryInfo(@"C:\NetworkShare");
        public DirectoryInfo LogFilesDir { get; set; } = new DirectoryInfo(@"C:\NetworkShare\logs");
    }

    public interface IConversionParamsBuilder
    {
        IConversionParamsBuilder SetInputFile(string input);
        IConversionParamsBuilder SetOutputFile(string output);
        IConversionParamsBuilder SetFps(int fps);
        string Build();
    }

    internal class ConversionParamsBuilder: IConversionParamsBuilder
    {
        private string input;
        private string output;
        private int? fps;

        public IConversionParamsBuilder SetInputFile(string input) { this.input = input; return this; }
        public IConversionParamsBuilder SetOutputFile(string output) { this.output = output; return this; }
        public IConversionParamsBuilder SetFps(int fps) { this.fps = fps; return this; }

        public string Build() {
            if (!fps.HasValue) throw new System.Exception("fps not set");
            if (input == null) throw new System.Exception("input not set");
            if (output == null) throw new System.Exception("output not set");
            var args = new List<string>()
            {
                $"-i \"{input}\"",
                "-c:a libopus",
                "-b:a 16k",
//                "-b:a 64k",
                "-frame_duration 60",
                "-c:v libsvtav1",
                "-preset 4",
                "-crf 60",
                "-pix_fmt yuv420p10le",
                "-svtav1-params tune=0:film-grain=0",
                $"-g 720",
//                $"-g {fps * 30}",
//                $"-r {fps}",
                "-nostdin",
                $"\"{output}\"",
            };
            return string.Join(" ", args);
        }
    }
}

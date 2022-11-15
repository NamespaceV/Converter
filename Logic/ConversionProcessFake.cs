using Converter.Basic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Logic
{
    internal class ConversionProcessFake : ConversionProcess
    {
        private Task task;

        public ConversionProcessFake(FileInfo sourceFile, int fps, ILogger logger)
            : base(sourceFile, fps, logger)
        {
        }

        override public void  StartConversionProcess(Action onProcessingSuccess, Action<int?> onProcessingFailed) 
        {
            task = Task.Factory.StartNew(() =>
            {
                Thread.Sleep(2000);
                onProcessingSuccess();
            });
        }

    }
}

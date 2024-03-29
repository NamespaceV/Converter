﻿using Converter.Basic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Logic
{
    internal class ConversionProcessFake : ConversionProcess
    {
        private Task task;

        public ConversionProcessFake(IBaseModel baseModel, IFileLister fileLister, FileInfo sourceFile, int fps, ILogger logger)
            : base(baseModel, fileLister, sourceFile, fps, logger)
        {
        }

        override public void  StartConversionProcess(Action onProcessingSuccess, Action<int?> onProcessingFailed) 
        {
            task = Task.Factory.StartNew(() =>
            {
                Thread.Sleep(2000);
                onProcessingSuccess();
                //onProcessingFailed(5);
            });
        }
    }
}

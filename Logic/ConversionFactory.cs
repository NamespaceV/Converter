using Converter.Basic;

namespace Converter.Logic
{
    public class ConversionFactory
    {
        public ConversionFactory(IBaseModel baseModel, IFileLister fileLister )
        {
            BaseModel = baseModel;
            FileLister = fileLister;
        }

        public IBaseModel BaseModel { get; }
        public IFileLister FileLister { get; }

        public ConversionProcess CreateConversionFor(System.IO.FileInfo source, int fps, ILogger logger) {
            return SettingsProivider.UseFakeConversion
                    ? new ConversionProcessFake(BaseModel, FileLister, source, fps, logger)
                    : new ConversionProcess(BaseModel, FileLister, source, fps, logger);
        }
    }
}

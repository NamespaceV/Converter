using Converter.Logic;
using Prism.Ioc;
using Prism.Unity;
using System.Windows;

namespace Converter
{
    public partial class App : PrismApplication
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<MainWindow, MainWindow>();
            containerRegistry.RegisterSingleton<MainWindowVM, MainWindowVM>();

            containerRegistry.RegisterSingleton<IBaseModel, BaseModel>();
            containerRegistry.RegisterSingleton<IFileLister, FileLister>();
            containerRegistry.RegisterSingleton<FileLogger>();
            containerRegistry.RegisterSingleton<ConversionFactory>();

        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }
    }
}

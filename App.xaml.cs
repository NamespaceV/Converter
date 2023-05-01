using Converter.Logic;
using Converter.ViewModels;
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
            containerRegistry.RegisterSingleton<IFileLister, FileLister>();
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }
    }
}

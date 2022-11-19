using System.Runtime.InteropServices;

namespace Converter.Logic
{
    internal class WindowHelper
    {
        public enum ShowWindowEnum{
            Hide = 0,
            Show = 5,
        }

        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        public static void ShowWindow(int hwnd, ShowWindowEnum mode) {
            ShowWindow(hwnd, (int)mode);
        }

    }
}

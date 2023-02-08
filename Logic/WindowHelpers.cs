using System.Runtime.InteropServices;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static int? GetWindowHandle(Process p)
        {
            var handles = GetProcessWindows(p.Id);
            if (handles.Length == 0)
            {
                return null;
            }
            return handles.First().ToInt32();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr parentWindow, IntPtr previousChildWindow, string windowClass, string windowTitle);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr window, out int process);

        private static IntPtr[] GetProcessWindows(int process)
        {
            IntPtr[] apRet = (new IntPtr[256]);
            int iCount = 0;
            IntPtr pLast = IntPtr.Zero;
            do
            {
                pLast = FindWindowEx(IntPtr.Zero, pLast, null, null);
                int iProcess_;
                GetWindowThreadProcessId(pLast, out iProcess_);
                if (iProcess_ == process) apRet[iCount++] = pLast;
            } while (pLast != IntPtr.Zero);
            System.Array.Resize(ref apRet, iCount);
            return apRet;
        }
    }
}

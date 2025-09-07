using System;
using System.Runtime.InteropServices;

namespace IDS.Core.Common
{
    /// <summary>
    /// Class for intercepting low level Windows mouse hooks.
    /// </summary>
    public class WindowsMouseHook: IDisposable
    {
        // DON'T RENAME THE NAMING OF THE PARAMETER IN THIS REGION!!!
        // Follow the naming convention because easy to search the Window API online later
        #region WinAPI
        /// <summary>
        /// Internal callback processing function
        /// </summary>
        private delegate IntPtr MouseHookHandler(int code, IntPtr wParam, IntPtr lParam);

        private const int WH_MOUSE_LL = 7;

        private enum MouseMessagesProcess
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_LBUTTONDBLCLK = 0x0203,
            WM_MBUTTONDOWN = 0x0207,
            WM_MBUTTONUP = 0x0208
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseHookEventStruct
        {
            public POINT Point;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, MouseHookHandler lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        #endregion

        /// <summary>
        /// Function to be called when defined even occurs
        /// </summary>
        /// <wParam name="mouseEventStruct">MouseHookEventStruct mouse structure</wParam>
        public delegate void MouseHookCallback(MouseHookEventStruct mouseEventStruct);

        #region Events
        public event MouseHookCallback LeftButtonDown;
        public event MouseHookCallback LeftButtonUp;
        public event MouseHookCallback RightButtonDown;
        public event MouseHookCallback RightButtonUp;
        public event MouseHookCallback MouseMove;
        public event MouseHookCallback MouseWheel;
        public event MouseHookCallback DoubleClick;
        public event MouseHookCallback MiddleButtonDown;
        public event MouseHookCallback MiddleButtonUp;
        #endregion

        private MouseHookHandler _hookHandler;

        /// <summary>
        /// Low level mouse hook's ID
        /// </summary>
        private IntPtr _hookId = IntPtr.Zero;

        private readonly uint _appThreadId;

        public WindowsMouseHook(uint appThreadId)
        {
            _appThreadId = appThreadId;
        }

        private void ReleaseUnmanagedResources()
        {
            Uninstall();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Destructor. Unhook current hook
        /// </summary>
        ~WindowsMouseHook()
        {
            ReleaseUnmanagedResources();
        }

        /// <summary>
        /// Install low level mouse hook
        /// </summary>
        /// <wParam name="mouseHookCallbackFunc">Callback function</wParam>
        public void Install()
        {
            _hookHandler = HookFunc;
            _hookId = SetHook(_hookHandler);
        }

        /// <summary>
        /// Remove low level mouse hook
        /// </summary>
        public void Uninstall()
        {
            if (_hookId == IntPtr.Zero)
                return;

            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }

        /// <summary>
        /// Sets hook and assigns its ID for tracking
        /// </summary>
        /// <wParam name="proc">Internal callback function</wParam>
        /// <returns>Hook ID</returns>
        private IntPtr SetHook(MouseHookHandler proc)
        {

            return SetWindowsHookEx(WH_MOUSE_LL, proc, IntPtr.Zero, _appThreadId);
        }

        /// <summary>
        /// Callback function
        /// </summary>
        private IntPtr HookFunc(int code, IntPtr wParam, IntPtr lParam)
        {
            // parse system messages
            if (code >= 0)
            {
                if (MouseMessagesProcess.WM_LBUTTONDOWN == (MouseMessagesProcess)wParam)
                    if (LeftButtonDown != null)
                        LeftButtonDown((MouseHookEventStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookEventStruct)));
                if (MouseMessagesProcess.WM_LBUTTONUP == (MouseMessagesProcess)wParam)
                    if (LeftButtonUp != null)
                        LeftButtonUp((MouseHookEventStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookEventStruct)));
                if (MouseMessagesProcess.WM_RBUTTONDOWN == (MouseMessagesProcess)wParam)
                    if (RightButtonDown != null)
                        RightButtonDown((MouseHookEventStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookEventStruct)));
                if (MouseMessagesProcess.WM_RBUTTONUP == (MouseMessagesProcess)wParam)
                    if (RightButtonUp != null)
                        RightButtonUp((MouseHookEventStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookEventStruct)));
                if (MouseMessagesProcess.WM_MOUSEMOVE == (MouseMessagesProcess)wParam)
                    if (MouseMove != null)
                        MouseMove((MouseHookEventStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookEventStruct)));
                if (MouseMessagesProcess.WM_MOUSEWHEEL == (MouseMessagesProcess)wParam)
                    if (MouseWheel != null)
                        MouseWheel((MouseHookEventStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookEventStruct)));
                if (MouseMessagesProcess.WM_LBUTTONDBLCLK == (MouseMessagesProcess)wParam)
                    if (DoubleClick != null)
                        DoubleClick((MouseHookEventStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookEventStruct)));
                if (MouseMessagesProcess.WM_MBUTTONDOWN == (MouseMessagesProcess)wParam)
                    if (MiddleButtonDown != null)
                        MiddleButtonDown((MouseHookEventStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookEventStruct)));
                if (MouseMessagesProcess.WM_MBUTTONUP == (MouseMessagesProcess)wParam)
                    if (MiddleButtonUp != null)
                        MiddleButtonUp((MouseHookEventStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookEventStruct)));
            }
            return CallNextHookEx(_hookId, code, wParam, lParam);
        }
    }
}

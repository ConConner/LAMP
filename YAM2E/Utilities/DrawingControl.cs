using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LAMP.Utilities
{
    public static class DrawingControl
    {
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);

        private const int WM_SETREDRAW = 11;

        /// <summary>
        /// Temporarily stops the drawing of the control
        /// </summary>
        /// <param name="control"></param>
        public static void SuspendDrawing(this Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, false, 0);
        }

        /// <summary>
        /// Resumes the drawing of the control
        /// </summary>
        /// <param name="control"></param>
        public static void ResumeDrawing(this Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, true, 0);
            control.Refresh();
        }
    }
}

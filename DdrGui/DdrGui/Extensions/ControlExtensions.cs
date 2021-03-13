using System;
using System.Windows.Forms;

namespace DdrGui
{
    public static class ControlExtensions
    {
        public static void AutoInvoke(this Control ctrl, Action worker)
        {
            if (ctrl.InvokeRequired)
            {
                _ = ctrl?.Invoke((MethodInvoker)delegate { worker?.Invoke(); });
            }
            else
            {
                worker?.Invoke();
            }
        }

        public static void AutoInvoke<T>(this T ctrl, Action<T> worker)
            where T : Control
        {
            if (ctrl.InvokeRequired)
            {
                _ = ctrl?.Invoke((MethodInvoker)delegate { worker?.Invoke(ctrl); });
            }
            else
            {
                worker?.Invoke(ctrl);
            }
        }
    }
}

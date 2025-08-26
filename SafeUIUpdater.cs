using System;
using System.Windows.Forms;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 安全UI更新器
    /// 提供线程安全的UI更新方法，避免跨线程操作异常
    /// </summary>
    public static class SafeUIUpdater
    {
        /// <summary>
        /// 安全更新UI控件
        /// </summary>
        /// <param name="control">要更新的控件</param>
        /// <param name="updateAction">更新操作</param>
        public static void UpdateUI(Control control, Action updateAction)
        {
            if (control == null || updateAction == null) return;

            try
            {
                if (control.InvokeRequired)
                {
                    // 使用 BeginInvoke 进行异步调用，避免阻塞调用线程
                    control.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            updateAction();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"UI更新异常: {ex.Message}");
                        }
                    }));
                }
                else
                {
                    updateAction();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI更新调用异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全更新UI控件（同步方式）
        /// 在需要等待UI更新完成的场景下使用
        /// </summary>
        /// <param name="control">要更新的控件</param>
        /// <param name="updateAction">更新操作</param>
        public static void UpdateUISync(Control control, Action updateAction)
        {
            if (control == null || updateAction == null) return;

            try
            {
                if (control.InvokeRequired)
                {
                    // 使用 Invoke 进行同步调用
                    control.Invoke(new Action(() =>
                    {
                        try
                        {
                            updateAction();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"UI更新异常: {ex.Message}");
                        }
                    }));
                }
                else
                {
                    updateAction();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI更新调用异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查控件是否可以安全更新
        /// </summary>
        /// <param name="control">要检查的控件</param>
        /// <returns>是否可以安全更新</returns>
        public static bool CanUpdate(Control control)
        {
            return control != null && !control.IsDisposed && control.IsHandleCreated;
        }
    }
}
using System;
using System.Windows.Forms;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 安全的UI更新器 - 确保跨线程UI操作的安全性
    /// </summary>
    public class SafeUIUpdater
    {
        /// <summary>
        /// 静态方法：安全更新UI
        /// </summary>
        /// <param name="control">控件</param>
        /// <param name="action">要执行的操作</param>
        public static void UpdateUI(Control control, Action action)
        {
            if (control == null || action == null) return;

            try
            {
                if (control.InvokeRequired)
                {
                    control.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (ObjectDisposedException)
            {
                // 控件已被释放，忽略错误
            }
            catch (InvalidOperationException)
            {
                // 控件不在正确状态，忽略错误
            }
            catch (Exception ex)
            {
                // 记录其他错误但不中断程序
                Console.WriteLine($"UI更新时发生异常: {ex.Message}");
            }
        }
        /// <summary>
        /// 安全调用UI操作
        /// </summary>
        /// <param name="control">控件</param>
        /// <param name="action">要执行的操作</param>
        public void SafeInvoke(Control control, Action action)
        {
            if (control == null || action == null) return;

            try
            {
                if (control.InvokeRequired)
                {
                    control.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (ObjectDisposedException)
            {
                // 控件已被释放，忽略错误
            }
            catch (InvalidOperationException)
            {
                // 控件不在正确状态，忽略错误
            }
            catch (Exception ex)
            {
                // 记录其他错误但不中断程序
                Console.WriteLine($"UI更新时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全调用UI操作（带返回值）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="control">控件</param>
        /// <param name="func">要执行的函数</param>
        /// <returns>执行结果</returns>
        public T SafeInvoke<T>(Control control, Func<T> func)
        {
            if (control == null || func == null) return default(T);

            try
            {
                if (control.InvokeRequired)
                {
                    return (T)control.Invoke(func);
                }
                else
                {
                    return func();
                }
            }
            catch (ObjectDisposedException)
            {
                // 控件已被释放，返回默认值
                return default(T);
            }
            catch (InvalidOperationException)
            {
                // 控件不在正确状态，返回默认值
                return default(T);
            }
            catch (Exception ex)
            {
                // 记录其他错误但不中断程序
                Console.WriteLine($"UI更新时发生异常: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// 检查控件是否可以安全访问
        /// </summary>
        /// <param name="control">控件</param>
        /// <returns>是否可以安全访问</returns>
        public bool CanSafelyAccess(Control control)
        {
            if (control == null) return false;

            try
            {
                return !control.IsDisposed && control.IsHandleCreated;
            }
            catch
            {
                return false;
            }
        }
    }
}
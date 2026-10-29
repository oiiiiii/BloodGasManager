using BGManager.Views;
using BGManager.Services;
using System.Runtime.InteropServices;

namespace BGManager;

internal static class Program
{
    // 窗口消息常量
    private const int WM_USER = 0x0400;
    private const int WM_SHOW_MAINWINDOW = WM_USER + 1;

    // 互斥量，确保单实例运行
    private static Mutex? _mutex;

    // Windows API声明
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [STAThread]
    static void Main()
    {
        // 创建一个唯一的互斥量名称
        string mutexName = "BGManager_SingleInstance";
        string eventName = "BGManager_ShowWindowEvent";
        bool createdNew;

        // 尝试创建互斥量
        _mutex = new Mutex(true, mutexName, out createdNew);

        if (!createdNew)
        {
            // 互斥量已存在，说明已有实例在运行
            try
            {
                using (var showEvent = EventWaitHandle.OpenExisting(eventName))
                {
                    showEvent.Set();
                }
            }
            catch
            {
                // 如果事件不存在，尝试通过窗口消息
                ActivateExistingWindow();
            }

            // 释放互斥量并退出
            _mutex.ReleaseMutex();
            _mutex.Dispose();
            return;
        }

        ApplicationConfiguration.Initialize();

        // 注册 GB2312 等代码页编码（患者姓名拼音首字母识别需要）
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // 创建命名事件，用于接收显示窗口的通知
        using (var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName))
        {
            // 在单独线程中监听事件
            var eventThread = new Thread(() =>
            {
                while (true)
                {
                    showEvent.WaitOne();
                    // 通知主线程显示窗口
                    if (Application.OpenForms.Count > 0)
                    {
                        var mainForm = Application.OpenForms[0] as MainForm;
                        if (mainForm != null)
                        {
                            mainForm.Invoke(new Action(() =>
                            {
                                mainForm.ShowMainWindow();
                            }));
                        }
                    }
                }
            });
            eventThread.IsBackground = true;
            eventThread.Start();

            try
            {
                Application.Run(new MainForm());
            }
            finally
            {
                // 释放互斥量
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                }
            }
        }
    }

    private static void ActivateExistingWindow()
    {
        // 先尝试通过窗口标题查找
        IntPtr hWnd = FindWindow(null, "血气分析管理系统 V2");
        if (hWnd != IntPtr.Zero)
        {
            BringWindowToForeground(hWnd);
            return;
        }

        // 如果找不到，枚举所有窗口查找属于我们进程的窗口
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var processes = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName);
        foreach (var process in processes)
        {
            if (process.Id != currentProcess.Id)
            {
                // 尝试通过进程ID枚举窗口
                IntPtr foundWindow = IntPtr.Zero;
                EnumWindows((hWnd, lParam) =>
                {
                    GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                    if (windowProcessId == process.Id)
                    {
                        foundWindow = hWnd;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);

                if (foundWindow != IntPtr.Zero)
                {
                    BringWindowToForeground(foundWindow);
                    return;
                }

                // 最后尝试使用MainWindowHandle
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    BringWindowToForeground(process.MainWindowHandle);
                    return;
                }
            }
        }
    }

    private static void BringWindowToForeground(IntPtr hWnd)
    {
        const int SW_RESTORE = 9;
        const int SW_SHOW = 5;

        // 显示窗口
        if (!IsWindowVisible(hWnd))
        {
            ShowWindow(hWnd, SW_SHOW);
        }

        // 如果窗口是最小化状态，还原它
        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, SW_RESTORE);
        }

        // 使用AttachThreadInput确保窗口能正确激活
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != hWnd)
        {
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            uint currentThreadId = GetCurrentThreadId();
            uint windowThreadId = GetWindowThreadProcessId(hWnd, out _);

            if (foregroundThreadId != windowThreadId)
            {
                AttachThreadInput(currentThreadId, windowThreadId, true);
            }

            SetForegroundWindow(hWnd);

            if (foregroundThreadId != windowThreadId)
            {
                AttachThreadInput(currentThreadId, windowThreadId, false);
            }
        }

        // 发送自定义消息
        PostMessage(hWnd, WM_SHOW_MAINWINDOW, IntPtr.Zero, IntPtr.Zero);
    }
}
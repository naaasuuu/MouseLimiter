using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MouseRangeLimiter
{
    public partial class MainWindow : Window
    {
        // 导入user32.dll中的GetCursorPos函数，用于获取鼠标当前位置
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        // 导入user32.dll中的CreateWindowEx函数，用于创建限制窗口
        [DllImport("user32.dll")]
        static extern IntPtr CreateWindowEx(
            uint dwExStyle,       // 窗口扩展样式
            string lpClassName,    // 窗口类名
            string lpWindowName,   // 窗口名称
            uint dwStyle,         // 窗口样式
            int x,               // 窗口左上角x坐标
            int y,               // 窗口左上角y坐标
            int nWidth,          // 窗口宽度
            int nHeight,         // 窗口高度
            IntPtr hWndParent,   // 父窗口句柄
            IntPtr hMenu,        // 菜单句柄
            IntPtr hInstance,    // 实例句柄
            IntPtr lpParam);     // 创建参数

        // 导入user32.dll中的DestroyWindow函数，用于销毁窗口
        [DllImport("user32.dll")]
        static extern bool DestroyWindow(IntPtr hWnd);

        // 导入user32.dll中的ClipCursor函数，用于限制光标移动范围(RECT版本)
        [DllImport("user32.dll")]
        static extern bool ClipCursor(ref RECT lpRect);

        // 导入user32.dll中的ClipCursor函数，用于释放光标限制(IntPtr版本)
        [DllImport("user32.dll")]
        static extern bool ClipCursor(IntPtr lpRect);

        // 导入user32.dll中的GetWindowRect函数，用于获取窗口矩形区域
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // 导入user32.dll中的GetAsyncKeyState函数，用于获取按键状态
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private POINT referencePoint;       // 存储参考点坐标
        private bool isReferenceSet = false; // 是否已设置参考点标志
        private bool isLimitationActive = false; // 是否激活限制标志
        private IntPtr restrictionWindow = IntPtr.Zero; // 限制窗口句柄
        private DispatcherTimer keyCheckTimer; // 键盘检测定时器

        /// <summary>
        /// 主窗口构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            InitializeKeyCheckTimer(); // 初始化键盘检测定时器
        }

        /// <summary>
        /// 初始化键盘检测定时器
        /// </summary>
        private void InitializeKeyCheckTimer()
        {
            keyCheckTimer = new DispatcherTimer();
            keyCheckTimer.Interval = TimeSpan.FromMilliseconds(50); // 设置检测间隔50ms
            keyCheckTimer.Tick += CheckKeyboardShortcuts; // 绑定检测事件
            keyCheckTimer.Start(); // 启动定时器
        }

        /// <summary>
        /// 检测键盘快捷键
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private void CheckKeyboardShortcuts(object sender, EventArgs e)
        {
            // 检测F2键是否按下(0x71是F2的虚拟键码)
            if ((GetAsyncKeyState(0x71) & 0x8000) != 0)
            {
                SetReference_Click(null, null); // 调用设置参考点方法
            }

            // 检测F5键是否按下(0x74是F5的虚拟键码)
            if ((GetAsyncKeyState(0x74) & 0x8000) != 0)
            {
                ToggleLimitation_Click(null, null); // 调用切换限制状态方法
            }
        }

        /// <summary>
        /// 设置参考点按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private void SetReference_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前鼠标位置
            if (GetCursorPos(out referencePoint))
            {
                isReferenceSet = true; // 设置参考点标志
                statusText.Text = $"参考点已设置: X={referencePoint.X}, Y={referencePoint.Y}"; // 更新状态栏
            }
            else
            {
                statusText.Text = "无法获取鼠标位置"; // 获取失败提示
            }
        }

        /// <summary>
        /// 切换限制状态按钮点击事件处理
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private void ToggleLimitation_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否已设置参考点
            if (!isReferenceSet)
            {
                statusText.Text = "请先设置参考点";
                return;
            }

            isLimitationActive = !isLimitationActive; // 切换限制状态

            if (isLimitationActive)
            {
                CreateRestrictionWindow(); // 创建限制窗口
                btnToggleLimit.Content = "禁用限制 (F5)"; // 更新按钮文本
                statusText.Text = $"限制已激活 - {GetCurrentModeName()}"; // 更新状态栏
            }
            else
            {
                ReleaseRestriction(); // 释放限制
                btnToggleLimit.Content = "启用限制 (F5)"; // 更新按钮文本
                statusText.Text = "限制已停用"; // 更新状态栏
            }
        }

        /// <summary>
        /// 创建限制窗口
        /// </summary>
        private void CreateRestrictionWindow()
        {
            // 如果已有限制窗口，先销毁
            if (restrictionWindow != IntPtr.Zero)
            {
                DestroyWindow(restrictionWindow);
            }

            // 定义窗口样式常量
            uint WS_POPUP = 0x80000000; // 弹出窗口
            uint WS_EX_TOOLWINDOW = 0x00000080; // 工具窗口(不在任务栏显示)
            uint WS_EX_TRANSPARENT = 0x00000020; // 透明窗口
            uint WS_EX_LAYERED = 0x00080000; // 分层窗口
            uint WS_EX_NOACTIVATE = 0x08000000; // 不激活窗口

            int width = 1;  // 默认宽度(点模式)
            int height = 1; // 默认高度(点模式)
            int xPos = referencePoint.X; // 默认x坐标(参考点x)
            int yPos = referencePoint.Y; // 默认y坐标(参考点y)

            // 根据选择的模式设置窗口尺寸和位置
            if (rbHorizontal.IsChecked == true)
            {
                width = (int)SystemParameters.PrimaryScreenWidth; // 水平模式: 屏幕宽度
                height = 1; // 高度1像素
                xPos = 0; // 水平模式窗口靠左对齐
            }
            else if (rbVertical.IsChecked == true)
            {
                width = 1; // 宽度1像素
                height = (int)SystemParameters.PrimaryScreenHeight; // 垂直模式: 屏幕高度
                yPos = 0; // 垂直模式窗口靠顶对齐
            }

            // 创建限制窗口
            restrictionWindow = CreateWindowEx(
                WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE,
                "Static", // 使用静态控件类
                "MouseLimiterRestriction", // 窗口名称
                WS_POPUP, // 窗口样式
                xPos, // x坐标(已根据模式调整)
                yPos, // y坐标(已根据模式调整)
                width, // 宽度
                height, // 高度
                IntPtr.Zero, // 无父窗口
                IntPtr.Zero, // 无菜单
                IntPtr.Zero, // 无实例句柄
                IntPtr.Zero); // 无额外参数

            // 如果窗口创建成功，限制光标范围
            if (restrictionWindow != IntPtr.Zero)
            {
                RECT rect;
                GetWindowRect(restrictionWindow, out rect); // 获取窗口矩形

                // 对于水平模式，保持原始Y坐标限制
                if (rbHorizontal.IsChecked == true)
                {
                    rect.Top = referencePoint.Y;
                    rect.Bottom = referencePoint.Y + 1;
                }
                // 对于垂直模式，保持原始X坐标限制
                else if (rbVertical.IsChecked == true)
                {
                    rect.Left = referencePoint.X;
                    rect.Right = referencePoint.X + 1;
                }

                ClipCursor(ref rect); // 限制光标在窗口范围内
            }
        }

        /// <summary>
        /// 释放鼠标限制
        /// </summary>
        private void ReleaseRestriction()
        {
            if (restrictionWindow != IntPtr.Zero)
            {
                ClipCursor(IntPtr.Zero); // 释放光标限制
                DestroyWindow(restrictionWindow); // 销毁窗口
                restrictionWindow = IntPtr.Zero; // 重置窗口句柄
            }
        }

        /// <summary>
        /// 获取当前限制模式名称
        /// </summary>
        /// <returns>模式名称字符串</returns>
        private string GetCurrentModeName()
        {
            if (rbHorizontal.IsChecked == true) return "水平条模式";
            if (rbVertical.IsChecked == true) return "垂直条模式";
            return "点模式";
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        /// <param name="e">事件参数</param>
        protected override void OnClosed(EventArgs e)
        {
            ReleaseRestriction(); // 确保释放限制
            keyCheckTimer.Stop(); // 停止定时器
            base.OnClosed(e); // 调用基类方法
        }
    }

    /// <summary>
    /// POINT结构体，表示屏幕上的一个点
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X; // x坐标
        public int Y; // y坐标
    }

    /// <summary>
    /// RECT结构体，表示一个矩形区域
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;   // 左边界
        public int Top;    // 上边界
        public int Right;  // 右边界
        public int Bottom; // 下边界
    }
}
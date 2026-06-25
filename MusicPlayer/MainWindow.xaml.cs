using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using MusicPlayer.ViewModels;

namespace MusicPlayer;

public partial class MainWindow : Window
{
    // DWM API 导入
    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int leftWidth;
        public int rightWidth;
        public int topHeight;
        public int bottomHeight;
    }

    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        StateChanged += MainWindow_StateChanged;
        InitializeTrayIcon();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        EnableBlur();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Cleanup();
        }

        _notifyIcon?.Dispose();
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            _notifyIcon!.Visible = true;
        }
        else
        {
            _notifyIcon!.Visible = false;
        }
    }

    private void EnableBlur()
    {
        var windowHelper = new WindowInteropHelper(this);
        var hwnd = windowHelper.Handle;

        var margins = new MARGINS
        {
            leftWidth = -1,
            rightWidth = -1,
            topHeight = -1,
            bottomHeight = -1
        };

        DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "Music Player",
            Visible = false
        };

        // 创建简单的图标
        var icon = CreateSimpleIcon();
        _notifyIcon.Icon = icon;

        _notifyIcon.DoubleClick += (s, e) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };

        // 创建右键菜单
        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("显示主窗口", null, (s, e) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        });
        contextMenu.Items.Add("迷你模式", null, (s, e) =>
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SwitchToMiniMode();
            }
        });
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("退出", null, (s, e) =>
        {
            System.Windows.Application.Current.Shutdown();
        });

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private System.Drawing.Icon CreateSimpleIcon()
    {
        // 创建一个简单的音符图标
        var bitmap = new System.Drawing.Bitmap(16, 16);
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.Clear(System.Drawing.Color.Transparent);
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(137, 180, 250)))
            {
                graphics.DrawString("♪", new System.Drawing.Font("Arial", 10), brush, 0, 0);
            }
        }

        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    // 窗口控制按钮事件
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SwitchToMini_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SwitchToMiniMode();
        }
    }

    private void ShowMainWindow_Click(object sender, RoutedEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DataContext is MainViewModel viewModel && sender is Slider slider)
        {
            if (slider.IsMouseCaptureWithin)
            {
                viewModel.IsSeeking = true;
                viewModel.SeekToPercentage(slider.Value);
                viewModel.CurrentPosition = TimeSpan.FromSeconds(viewModel.Duration.TotalSeconds * slider.Value / 100);
            }
        }
    }

    private void ProgressSlider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.IsSeeking = false;
        }
    }

    private void SongListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && SongListBox.SelectedItem is Models.Song song)
        {
            viewModel.PlaySongCommand.Execute(song);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(SongListBox.ItemsSource);
        if (view == null) return;

        var keyword = SearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(keyword))
        {
            view.Filter = null;
        }
        else
        {
            view.Filter = item =>
            {
                if (item is Models.Song song)
                {
                    return (song.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (song.Artist?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false);
                }
                return false;
            };
        }
    }
}

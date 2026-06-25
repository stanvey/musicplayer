using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Windows;

public partial class MiniPlayerWindow : Window
{
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
    
    private readonly MainViewModel _viewModel;
    
    public MiniPlayerWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        Loaded += MiniPlayerWindow_Loaded;
    }
    
    private void MiniPlayerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        EnableBlur();
        
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        Left = screenWidth - Width - 20;
        Top = screenHeight - Height - 60;
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
    
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
    
    private void SwitchToMain_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;
        mainWindow.Show();
        mainWindow.Activate();
        Close();
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.StopCommand.Execute(null);
        System.Windows.Application.Current.Shutdown();
    }
}

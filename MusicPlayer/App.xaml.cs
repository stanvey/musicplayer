using System.Windows;

namespace MusicPlayer;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 设置应用程序关闭模式
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }
}
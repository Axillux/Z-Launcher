using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ZLauncher.Services;
using ZLauncher.ViewModels;
using ZLauncher.Views;
using AvaloniaWebView; // Required for WebView initialization


namespace ZLauncher;

public partial class App : Application
{
public static ConfigService Config { get; } = new ConfigService();

public override void Initialize()
{
    AvaloniaXamlLoader.Load(this);
}

public override void RegisterServices()
{
    base.RegisterServices();
    AvaloniaWebViewBuilder.Initialize(default);
}

public override void OnFrameworkInitializationCompleted()
{
    try
    {
        var colorCode = Config.CurrentConfig.ThemeColor;
        if (!string.IsNullOrEmpty(colorCode))
        {
            var color = Color.Parse(colorCode);
            Resources["PrimaryGreen"] = new SolidColorBrush(color);
        }
    }
    catch { }

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow = new MainWindow
        {
            DataContext = new MainViewModel()
        };
    }
    else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
    {
        singleViewPlatform.MainView = new MainView
        {
            DataContext = new MainViewModel()
        };
    }

    base.OnFrameworkInitializationCompleted();
}

}

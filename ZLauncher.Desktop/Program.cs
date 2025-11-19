using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.WebView.Desktop; // <--- REQUIRED for UseDesktopWebView


namespace ZLauncher.Desktop;

class Program
{
[STAThread]
public static void Main(string[] args) => BuildAvaloniaApp()
.StartWithClassicDesktopLifetime(args);

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace()
        .UseReactiveUI()
        .UseDesktopWebView(); // <--- THIS IS THE CORRECT METHOD NAME

}

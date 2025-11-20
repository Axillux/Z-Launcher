using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using ReactiveUI;
using ZLauncher.Services;


namespace ZLauncher.ViewModels;

public class MainViewModel : ViewModelBase
{
private ViewModelBase _currentPage;
private readonly MinecraftService _minecraftService;
private readonly ConfigService _configService;
    private Bitmap? _logoBitmap;
    private WindowIcon? _windowIcon;

    private bool _isPaneOpen = false;

public HomeViewModel HomePage { get; }
public SettingsViewModel SettingsPage { get; }
public ProfileViewModel ProfilePage { get; }
public ModsViewModel ModsPage { get; }

public event Func<Task<Bitmap?>>? RequestLogoPicker;

public MainViewModel()
{
    _minecraftService = new MinecraftService();
    _configService = App.Config;

    ProfilePage = new ProfileViewModel(_minecraftService);
    HomePage = new HomeViewModel(this, _minecraftService);
    SettingsPage = new SettingsViewModel(this, _configService);
    ModsPage = new ModsViewModel();

    _currentPage = HomePage;
    
    TogglePaneCommand = ReactiveCommand.Create(() => IsPaneOpen = !IsPaneOpen);
    
    OpenDiscordCommand = ReactiveCommand.Create(() => OpenUrl("https://discord.gg/M3qWvcbDaq"));
    
    LoadSavedLogo();
}

public bool IsPaneOpen
{
    get => _isPaneOpen;
    set => this.RaiseAndSetIfChanged(ref _isPaneOpen, value);
}

public ICommand TogglePaneCommand { get; }
public ICommand OpenDiscordCommand { get; }

private void OpenUrl(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    catch (Exception)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }
}

    private void LoadSavedLogo()
    {
        var path = _configService.CurrentConfig.LogoPath;
        
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                using var stream = File.OpenRead(path);
                LogoBitmap = new Bitmap(stream);
                
                using var streamIcon = File.OpenRead(path);
                WindowIcon = new WindowIcon(new Bitmap(streamIcon));
                return;
            }
            catch
            {
            }
        }

        var defaultLogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.png");
        if (File.Exists(defaultLogoPath))
        {
            try
            {
                using var stream = File.OpenRead(defaultLogoPath);
                LogoBitmap = new Bitmap(stream);
                
                using var streamIcon = File.OpenRead(defaultLogoPath);
                WindowIcon = new WindowIcon(new Bitmap(streamIcon));
            }
            catch 
            { 
            }
        }
    }

public ViewModelBase CurrentPage
{
    get => _currentPage;
    set => this.RaiseAndSetIfChanged(ref _currentPage, value);
}

    public Bitmap? LogoBitmap
    {
        get => _logoBitmap;
        set => this.RaiseAndSetIfChanged(ref _logoBitmap, value);
    }

    public WindowIcon? WindowIcon
    {
        get => _windowIcon;
        set => this.RaiseAndSetIfChanged(ref _windowIcon, value);
    }

    public void NavigateHome() => CurrentPage = HomePage;
public void NavigateSettings() => CurrentPage = SettingsPage;
public void NavigateProfile() => CurrentPage = ProfilePage;
public void NavigateMods() => CurrentPage = ModsPage;

public async void TriggerLogoPicker()
{
    if (RequestLogoPicker != null)
    {
        var bmp = await RequestLogoPicker.Invoke();
        if (bmp != null)
        {
            LogoBitmap = bmp;
        }
    }
}

    public void SetLogo(string path)
    {
        try 
        {
            using var stream = File.OpenRead(path);
            LogoBitmap = new Bitmap(stream);
            
            using var streamIcon = File.OpenRead(path);
            WindowIcon = new WindowIcon(new Bitmap(streamIcon));
            
            _configService.CurrentConfig.LogoPath = path;
            _configService.Save();
        }
        catch { }
    }

}

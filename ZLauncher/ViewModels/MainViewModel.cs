using System;
using System.IO;
using System.Threading.Tasks;
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

public HomeViewModel HomePage { get; }
public SettingsViewModel SettingsPage { get; }
public ProfileViewModel ProfilePage { get; }
public ModsViewModel ModsPage { get; }

public event Func<Task<Bitmap?>>? RequestLogoPicker;

public MainViewModel()
{
    _minecraftService = new MinecraftService();
    _configService = App.Config; // Use the global singleton loaded in App.cs

    // Initialize Pages
    ProfilePage = new ProfileViewModel(_minecraftService);
    HomePage = new HomeViewModel(this, _minecraftService);
    SettingsPage = new SettingsViewModel(this, _configService);
    ModsPage = new ModsViewModel();

    _currentPage = HomePage;
    
    // LOAD SAVED LOGO
    LoadSavedLogo();
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
        }
        catch
        {
            // If file is corrupted, ignore
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

public void NavigateHome() => CurrentPage = HomePage;
public void NavigateSettings() => CurrentPage = SettingsPage;
public void NavigateProfile() => CurrentPage = ProfilePage;
public void NavigateMods() => CurrentPage = ModsPage;

public async void TriggerLogoPicker()
{
    if (RequestLogoPicker != null)
    {
        // This gets the Bitmap for the View
        var bmp = await RequestLogoPicker.Invoke();
        if (bmp != null)
        {
            LogoBitmap = bmp;
            // NOTE: The actual file path saving is tricky here because we returned a Bitmap stream.
            // To save the path, we will rely on the SettingsViewModel to handle the path logic separately 
            // or assume the user just picked it. 
            // See SettingsViewModel for the saving logic.
        }
    }
}

// Helper to set logo directly from SettingsViewModel
public void SetLogo(string path)
{
    try 
    {
        using var stream = File.OpenRead(path);
        LogoBitmap = new Bitmap(stream);
        
        // Save to config
        _configService.CurrentConfig.LogoPath = path;
        _configService.Save();
    }
    catch { }
}

}

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ZLauncher.Services;


namespace ZLauncher.ViewModels;

public class SettingsViewModel : ViewModelBase
{
private readonly MainViewModel _mainVm;
private readonly ConfigService _configService;

private int _selectedRam;
private string _customJavaPath = "";
private string _customHexColor = "#39FF14";

// Main Constructor
public SettingsViewModel(MainViewModel mainVm, ConfigService configService)
{
    _mainVm = mainVm;
    _configService = configService;
    
    // Load Initial Values
    _selectedRam = _configService.CurrentConfig.RamMb;
    _customJavaPath = _configService.CurrentConfig.JavaPath ?? "";
    _customHexColor = _configService.CurrentConfig.ThemeColor;

    ChangeLogoCommand = ReactiveCommand.CreateFromTask(ChangeLogoAsync);
    ApplyHexColorCommand = ReactiveCommand.Create(ApplyCustomHex);
    
    // Pre-defined
    SetGreenCommand = ReactiveCommand.Create(() => ChangeColor("#39FF14"));
    SetRedCommand = ReactiveCommand.Create(() => ChangeColor("#FF0000"));
    SetBlueCommand = ReactiveCommand.Create(() => ChangeColor("#00FFFF"));
}

// Designer Constructor
public SettingsViewModel() 
{
     _mainVm = null!;
     _configService = new ConfigService();
     ChangeLogoCommand = ReactiveCommand.Create(() => { });
     ApplyHexColorCommand = ReactiveCommand.Create(() => { });
     SetGreenCommand = ReactiveCommand.Create(() => { });
     SetRedCommand = ReactiveCommand.Create(() => { });
     SetBlueCommand = ReactiveCommand.Create(() => { });
}

public int SelectedRam
{
    get => _selectedRam;
    set
    {
        this.RaiseAndSetIfChanged(ref _selectedRam, value);
        _configService.CurrentConfig.RamMb = value;
        _configService.Save();
    }
}

public string CustomJavaPath
{
    get => _customJavaPath;
    set
    {
         this.RaiseAndSetIfChanged(ref _customJavaPath, value);
         _configService.CurrentConfig.JavaPath = value;
         _configService.Save();
    }
}

public string CustomHexColor
{
    get => _customHexColor;
    set => this.RaiseAndSetIfChanged(ref _customHexColor, value);
}

public ICommand ChangeLogoCommand { get; }
public ICommand ApplyHexColorCommand { get; }
public ICommand SetGreenCommand { get; }
public ICommand SetRedCommand { get; }
public ICommand SetBlueCommand { get; }

private void ChangeColor(string hex)
{
    try
    {
        var color = Color.Parse(hex);
        if (Application.Current != null)
        {
            Application.Current.Resources["PrimaryGreen"] = new SolidColorBrush(color);
        }
        
        // Save
        CustomHexColor = hex; // Update text box
        _configService.CurrentConfig.ThemeColor = hex;
        _configService.Save();
    }
    catch
    {
        // Invalid color
    }
}

private void ApplyCustomHex()
{
    if (!string.IsNullOrEmpty(CustomHexColor))
    {
        // Add # if missing
        if (!CustomHexColor.StartsWith("#")) CustomHexColor = "#" + CustomHexColor;
        ChangeColor(CustomHexColor);
    }
}

private async Task ChangeLogoAsync()
{
    // We do the file picking here to get the path directly for saving
    var app = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
    if (app?.MainWindow == null) return;

    var files = await app.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
    {
        Title = "Выберите логотип",
        AllowMultiple = false,
        FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
    });

    if (files.Count >= 1)
    {
        var path = files[0].Path.LocalPath;
        _mainVm.SetLogo(path); // Updates View and Saves Config
    }
}

}

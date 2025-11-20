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

public SettingsViewModel(MainViewModel mainVm, ConfigService configService)
{
    _mainVm = mainVm;
    _configService = configService;
    
    _selectedRam = _configService.CurrentConfig.RamMb;
    _customJavaPath = _configService.CurrentConfig.JavaPath ?? "";
    _customHexColor = _configService.CurrentConfig.ThemeColor;

    ChangeLogoCommand = ReactiveCommand.CreateFromTask(ChangeLogoAsync);
    ApplyHexColorCommand = ReactiveCommand.Create(ApplyCustomHex);
    
    SetGreenCommand = ReactiveCommand.Create(() => ChangeColor("#39FF14"));
    SetRedCommand = ReactiveCommand.Create(() => ChangeColor("#FF0000"));
    SetBlueCommand = ReactiveCommand.Create(() => ChangeColor("#00FFFF"));
}

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
        
        CustomHexColor = hex;
        _configService.CurrentConfig.ThemeColor = hex;
        _configService.Save();
    }
    catch
    {
    }
}

private void ApplyCustomHex()
{
    if (!string.IsNullOrEmpty(CustomHexColor))
    {
        if (!CustomHexColor.StartsWith("#")) CustomHexColor = "#" + CustomHexColor;
        ChangeColor(CustomHexColor);
    }
}

private async Task ChangeLogoAsync()
{
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
        _mainVm.SetLogo(path);
    }
}

}

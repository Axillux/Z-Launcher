using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ZLauncher.Services;


namespace ZLauncher.ViewModels;

public class HomeViewModel : ViewModelBase
{
private readonly MinecraftService _service;
private readonly MainViewModel _mainVm;
private string _statusText = "Готов к запуску";
private double _progress = 0;
private string _serverStatus = "Онлайн: 1";


public HomeViewModel(MainViewModel mainVm, MinecraftService service)
{
    _mainVm = mainVm;
    _service = service;
    
    LaunchCommand = ReactiveCommand.CreateFromTask(LaunchGame);

    _service.StatusChanged += (s) => StatusText = s;
    _service.ProgressChanged += (p) => Progress = p;
}

public string StatusText
{
    get => _statusText;
    set => this.RaiseAndSetIfChanged(ref _statusText, value);
}

public double Progress
{
    get => _progress;
    set => this.RaiseAndSetIfChanged(ref _progress, value);
}

public string ServerStatus
{
     get => _serverStatus;
     set => this.RaiseAndSetIfChanged(ref _serverStatus, value);
}

public ICommand LaunchCommand { get; }

private async Task LaunchGame()
{
    if (_service.CurrentSession is null)
    {
        StatusText = "Ошибка: Вы не вошли в аккаунт (Профиль)";
        return;
    }
    
    var settings = _mainVm.SettingsPage;
    
    // Launch using the session stored in Service
    await _service.LaunchGameAsync(settings.SelectedRam, settings.CustomJavaPath);
}

}

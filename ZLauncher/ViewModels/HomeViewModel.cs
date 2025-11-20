using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ZLauncher.Models;
using ZLauncher.Services;


namespace ZLauncher.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly MinecraftService _service;
    private readonly MainViewModel _mainVm;
    private readonly NewsService _newsService;
    private readonly ServerStatusService _serverService;
    
    private string _statusText = "Готов к запуску";
    private double _progress = 0;
    private string _serverStatus = "Поиск...";
    
    public ObservableCollection<NewsItem> News { get; } = new();


    public HomeViewModel(MainViewModel mainVm, MinecraftService service)
    {
        _mainVm = mainVm;
        _service = service;
        _newsService = new NewsService();
        _serverService = new ServerStatusService();
        
        LaunchCommand = ReactiveCommand.CreateFromTask(LaunchGame);

        _service.StatusChanged += (s) => StatusText = s;
        _service.ProgressChanged += (p) => Progress = p;
        
        LoadNews();
        StartServerPolling();
    }

    private async void StartServerPolling()
    {
        await CheckServer();
        
        while (true)
        {
            await Task.Delay(30000);
            await CheckServer();
        }
    }

    private async Task CheckServer()
    {
        var (status, count) = await _serverService.PingServerAsync("0.0.0.0", 25565);
        ServerStatus = status == "Онлайн" ? $"Онлайн: {count}" : "Оффлайн";
    }
    
    private async void LoadNews()
    {
        var news = await _newsService.GetNewsAsync();
        foreach (var item in news)
        {
            News.Add(item);
        }
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
    
    var mods = _mainVm.ModsPage.Mods;
    await _service.LaunchGameAsync(settings.SelectedRam, settings.CustomJavaPath, mods);
}

}

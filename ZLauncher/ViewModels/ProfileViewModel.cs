using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ZLauncher.Models;
using ZLauncher.Services;


namespace ZLauncher.ViewModels;

public class ProfileViewModel : ViewModelBase
{
private readonly MinecraftService _minecraftService;
private string _username = "Guest";
private string _uuid = "N/A";
private IImage? _currentSkinBitmap;
private bool _isLoggedIn = false;
private bool _isBusy = false;
private string _accountStatus = "OFFLINE";
private string _offlineUsernameInput = "Player";
private Uri? _skin3dUrl;

public ObservableCollection<SkinItem> SavedSkins { get; } = new();

public ProfileViewModel(MinecraftService service)
{
    _minecraftService = service;
    LoginMicrosoftCommand = ReactiveCommand.CreateFromTask(LoginMicrosoft);
    LoginOfflineCommand = ReactiveCommand.CreateFromTask(LoginOffline);
    LogoutCommand = ReactiveCommand.Create(Logout);
    AddSkinCommand = ReactiveCommand.CreateFromTask(AddSkinAsync);
    
    UpdateSkin3D("https://textures.minecraft.net/texture/334c364567814a2e57e78e67b5f12c76d179919f3228830b8749404813e31709");
    
    _ = InitializeSession();
}

public ProfileViewModel() 
{
    _minecraftService = new MinecraftService();
    LoginMicrosoftCommand = ReactiveCommand.Create(() => {});
    LoginOfflineCommand = ReactiveCommand.Create(() => {});
    LogoutCommand = ReactiveCommand.Create(() => {});
    AddSkinCommand = ReactiveCommand.Create(() => {});
}

public string Username
{
    get => _username;
    set => this.RaiseAndSetIfChanged(ref _username, value);
}

public string Uuid
{
    get => _uuid;
    set => this.RaiseAndSetIfChanged(ref _uuid, value);
}

public IImage? CurrentSkinBitmap
{
    get => _currentSkinBitmap;
    set => this.RaiseAndSetIfChanged(ref _currentSkinBitmap, value);
}

public Uri? Skin3dUrl
{
    get => _skin3dUrl;
    set => this.RaiseAndSetIfChanged(ref _skin3dUrl, value);
}

public bool IsLoggedIn
{
    get => _isLoggedIn;
    set => this.RaiseAndSetIfChanged(ref _isLoggedIn, value);
}

public bool IsBusy
{
    get => _isBusy;
    set => this.RaiseAndSetIfChanged(ref _isBusy, value);
}

public string AccountStatus
{
    get => _accountStatus;
    set => this.RaiseAndSetIfChanged(ref _accountStatus, value);
}

public string OfflineUsernameInput
{
    get => _offlineUsernameInput;
    set => this.RaiseAndSetIfChanged(ref _offlineUsernameInput, value);
}

public ICommand LoginMicrosoftCommand { get; }
public ICommand LoginOfflineCommand { get; }
public ICommand LogoutCommand { get; }
public ICommand AddSkinCommand { get; }

private async Task InitializeSession()
{
    IsBusy = true;
    var session = await _minecraftService.AutoLoginAsync();
    if (session != null)
    {
        Username = session.Username ?? "User";
        Uuid = session.UUID ?? "N/A";
        AccountStatus = "ONLINE (Saved)";
        IsLoggedIn = true;
        var url = $"https://visage.surgeplay.com/skin/{Uuid}";
        UpdateSkin3D(url);
    }
    IsBusy = false;
}

private async Task LoginMicrosoft()
{
    IsBusy = true;
    try
    {
        var session = await _minecraftService.LoginMicrosoftAsync();
        Username = session.Username ?? "Microsoft User";
        Uuid = session.UUID ?? "Unknown";
        AccountStatus = "ONLINE (MS)";
        IsLoggedIn = true;
        var url = $"https://visage.surgeplay.com/skin/{Uuid}";
        UpdateSkin3D(url);
    }
    catch (Exception)
    {
        AccountStatus = "ERROR";
    }
    finally
    {
        IsBusy = false;
    }
}

private async Task LoginOffline()
{
    if(string.IsNullOrWhiteSpace(OfflineUsernameInput)) return;
    
    IsBusy = true;
    var session = await _minecraftService.LoginOfflineAsync(OfflineUsernameInput);
    Username = session.Username ?? OfflineUsernameInput;
    Uuid = session.UUID ?? "Offline-UUID";
    AccountStatus = "OFFLINE";
    IsLoggedIn = true;
    IsBusy = false;
}

private void Logout()
{
    Username = "Guest";
    Uuid = "N/A";
    IsLoggedIn = false;
    AccountStatus = "OFFLINE";
}

private void UpdateSkin3D(string textureUrl)
{
    string htmlContent = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8'>
        <style>
            body {{ 
                margin: 0; 
                padding: 0; 
                overflow: hidden; 
                background-color: #111111;
                display: flex;
                justify-content: center;
                align-items: center;
                height: 100vh;
            }} 
            canvas {{ outline: none; }}
        </style>
        <script src='https://bs-community.github.io/skinview3d/js/skinview3d.bundle.js'></script>
    </head>
    <body>
        <canvas id='skin_container'></canvas>
        <script>
            let skinViewer = new skinview3d.SkinViewer({{
                canvas: document.getElementById('skin_container'),
                width: 300,
                height: 350,
                skin: '{textureUrl}'
            }});
            
            skinViewer.width = window.innerWidth;
            skinViewer.height = window.innerHeight;
            skinViewer.camera.position.x = 20;
            skinViewer.camera.position.y = 15;
            skinViewer.camera.position.z = 40;
            skinViewer.zoom = 0.9;
            skinViewer.fov = 70;
            
            skinViewer.animation = new skinview3d.WalkingAnimation();
            skinViewer.animation.speed = 0.5;
        </script>
    </body>
    </html>";
    
    string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlContent));
    Skin3dUrl = new Uri($"data:text/html;base64,{base64}");
}

private async Task AddSkinAsync()
{
    var app = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
    if (app?.MainWindow == null) return;

    var files = await app.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
    {
        Title = "Выберите скин (.png)",
        FileTypeFilter = new[] { FilePickerFileTypes.ImageAll },
        AllowMultiple = false
    });

    if (files.Any())
    {
        var path = files[0].Path.LocalPath;
        try
        {
            // 1. Generate Cropped Head for UI List (Safe Method)
            var headPreview = GenerateHeadPreview(path);

            // 2. Convert for 3D View
            var bytes = await File.ReadAllBytesAsync(path);
            var base64Skin = Convert.ToBase64String(bytes);
            var dataUri = $"data:image/png;base64,{base64Skin}";
            
            var skinItem = new SkinItem
            {
                FilePath = dataUri, 
                PreviewImage = headPreview, 
                IsSelected = false
            };
            
            SavedSkins.Add(skinItem);
            SelectSkin(skinItem);
        }
        catch (Exception ex) { 
            Console.WriteLine("Error loading skin: " + ex.Message);
        }
    }
}

// Safe Method: Uses standard CroppedBitmap
// When displayed with "LowQuality" mode in XAML, this 8x8 crop looks perfect pixelated
private IImage GenerateHeadPreview(string path)
{
    // Load the full skin
    using var stream = File.OpenRead(path);
    var source = new Bitmap(stream);
    
    // Crop the Face Area: X=8, Y=8, Width=8, Height=8
    return new CroppedBitmap(source, new PixelRect(8, 8, 8, 8));
}

public void SelectSkin(SkinItem item)
{
    foreach(var s in SavedSkins) s.IsSelected = false;
    item.IsSelected = true;
    UpdateSkin3D(item.FilePath);
}

}

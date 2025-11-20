using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using ZLauncher.Models;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using XboxAuthNet.Game.Msal;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Installer.Forge;


namespace ZLauncher.Services;

public class MinecraftService
{
private readonly MinecraftLauncher _launcher;
private readonly MinecraftPath _path;
private readonly JELoginHandler _loginHandler;

private const string McVersion = "1.20.1";
private const string ForgeVersion = "47.2.0"; 
private const string ClientID = "e13835a1-00a6-492d-ad23-b6d07dfe5186";

public MSession? CurrentSession { get; private set; }

public event Action<string>? StatusChanged;
public event Action<double>? ProgressChanged;

    public MinecraftService()
    {
        // кастомный путь 
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var zLauncherPath = Path.Combine(appData, "ZLauncher");
        
        _path = new MinecraftPath(zLauncherPath);
        _launcher = new MinecraftLauncher(_path);
        
        var builder = new JELoginHandlerBuilder()
            .WithAccountManager(Path.Combine(zLauncherPath, "cml_accounts.json"));
            
        _loginHandler = builder.Build();

        _launcher.FileProgressChanged += (sender, e) =>
        {
            StatusChanged?.Invoke($"[{e.EventType}] {e.Name}");
            if (e.TotalTasks > 0)
            {
                ProgressChanged?.Invoke((double)e.ProgressedTasks / e.TotalTasks * 100);
            }
        };
    }

public async Task<MSession?> AutoLoginAsync()
{
    try
    {
        var accounts = _loginHandler.AccountManager.GetAccounts();
        var lastAccount = accounts.FirstOrDefault();
        
        if (lastAccount != null)
        {
            StatusChanged?.Invoke("Добро пожаловать!");
            
            var app = await MsalClientHelper.BuildApplicationWithCache(ClientID);
            var authenticator = _loginHandler.CreateAuthenticator(lastAccount, default);
            
            authenticator.AddMsalOAuth(app, msal => msal.Silent());
            authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
            authenticator.AddJEAuthenticator();

            var session = await authenticator.ExecuteForLauncherAsync();
            CurrentSession = session;
            StatusChanged?.Invoke($"Рады видеть вас снова, {session.Username}!");
            return session;
        }
    }
    catch (Exception ex)
    {
        StatusChanged?.Invoke($"Добро пожаловать: {ex.Message}");
    }
    return null;
}

public async Task<MSession> LoginMicrosoftAsync()
{
    try
    {
        StatusChanged?.Invoke("Добро пожаловать!");
        
        var app = await MsalClientHelper.BuildApplicationWithCache(ClientID);
        var authenticator = _loginHandler.CreateAuthenticatorWithNewAccount(default);
        
        authenticator.AddMsalOAuth(app, msal => msal.Interactive());
        authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
        authenticator.AddJEAuthenticator();

        var session = await authenticator.ExecuteForLauncherAsync();
        
        CurrentSession = session;
        StatusChanged?.Invoke($"Добро пожаловать, {session.Username}!");
        return session;
    }
    catch (Exception ex)
    {
        StatusChanged?.Invoke($"Добро пожаловать: {ex.Message}");
        throw;
    }
}

public async Task<MSession> LoginOfflineAsync(string username)
{
    StatusChanged?.Invoke($"Добро пожаловать, {username}!");
    CurrentSession = MSession.CreateOfflineSession(username);
    return await Task.FromResult(CurrentSession);
}

    public async Task LaunchGameAsync(int ramMb, string? javaPath, IEnumerable<ModItem> mods)
    {
        if (CurrentSession == null)
        {
            StatusChanged?.Invoke("Пожалуйста, сначала войдите в систему!");
            return;
        }

        try
        {
            await ManageModsAsync(mods);

            StatusChanged?.Invoke("Проверяем Forge 1.20.1...");
        
        var forgeInstaller = new ForgeInstaller(_launcher);
        var forgeVersionName = await forgeInstaller.Install(McVersion, ForgeVersion);

        if (string.IsNullOrEmpty(forgeVersionName))
        {
            StatusChanged?.Invoke("Установка Forge не удалась!");
            return;
        }

        var launchOption = new MLaunchOption
        {
            MaximumRamMb = ramMb,
            Session = CurrentSession,
            GameLauncherName = "ZLauncher",
            GameLauncherVersion = "1.0.0"
        };

        if (!string.IsNullOrEmpty(javaPath)) launchOption.JavaPath = javaPath;

        StatusChanged?.Invoke($"Запускаем {forgeVersionName}...");
        
        var process = await _launcher.InstallAndBuildProcessAsync(forgeVersionName, launchOption);
        
        process.Start();
        StatusChanged?.Invoke("Майнкрафт запущен!");
    }
    catch (Exception ex)
    {
        StatusChanged?.Invoke($"Ошибка запуска: {ex.Message}");
    }
}

    private async Task ManageModsAsync(IEnumerable<ModItem> mods)
    {
        var modsDir = Path.Combine(_path.BasePath, "mods");
        Directory.CreateDirectory(modsDir);

        using var httpClient = new HttpClient();

        foreach (var mod in mods)
        {
            if (string.IsNullOrEmpty(mod.FileName)) continue;

            var filePath = Path.Combine(modsDir, mod.FileName);
            var disabledPath = filePath + ".disabled";

            if (mod.IsEnabled)
            {
                
                if (File.Exists(disabledPath))
                {
                    if (File.Exists(filePath)) File.Delete(filePath);
                    File.Move(disabledPath, filePath);
                }

                if (!File.Exists(filePath) && !string.IsNullOrEmpty(mod.DownloadUrl))
                {
                    try
                    {
                        StatusChanged?.Invoke($"Скачивание мода: {mod.Name}...");
                        var data = await httpClient.GetByteArrayAsync(mod.DownloadUrl);
                        await File.WriteAllBytesAsync(filePath, data);
                    }
                    catch (Exception ex)
                    {
                        StatusChanged?.Invoke($"Ошибка скачивания {mod.Name}: {ex.Message}");
                    }
                }
            }
            else
            {
                if (File.Exists(filePath))
                {
                    if (File.Exists(disabledPath)) File.Delete(disabledPath);
                    File.Move(filePath, disabledPath);
                }
            }
        }
    }

}

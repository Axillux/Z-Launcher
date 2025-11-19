using System;
using System.Linq;
using System.Threading;
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
    _path = new MinecraftPath();
    _launcher = new MinecraftLauncher(_path);
    
    var builder = new JELoginHandlerBuilder()
        .WithAccountManager(_path.ToString() + "/cml_accounts.json");
        
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
            StatusChanged?.Invoke("Silent Login...");
            
            var app = await MsalClientHelper.BuildApplicationWithCache(ClientID);
            var authenticator = _loginHandler.CreateAuthenticator(lastAccount, default);
            
            authenticator.AddMsalOAuth(app, msal => msal.Silent());
            authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
            authenticator.AddJEAuthenticator();

            var session = await authenticator.ExecuteForLauncherAsync();
            CurrentSession = session;
            StatusChanged?.Invoke($"Welcome back, {session.Username}!");
            return session;
        }
    }
    catch (Exception ex)
    {
        StatusChanged?.Invoke($"Auto-login failed: {ex.Message}");
    }
    return null;
}

public async Task<MSession> LoginMicrosoftAsync()
{
    try
    {
        StatusChanged?.Invoke("Microsoft Login...");
        
        var app = await MsalClientHelper.BuildApplicationWithCache(ClientID);
        var authenticator = _loginHandler.CreateAuthenticatorWithNewAccount(default);
        
        authenticator.AddMsalOAuth(app, msal => msal.Interactive());
        authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
        authenticator.AddJEAuthenticator();

        var session = await authenticator.ExecuteForLauncherAsync();
        
        CurrentSession = session;
        StatusChanged?.Invoke($"Logged in: {session.Username}");
        return session;
    }
    catch (Exception ex)
    {
        StatusChanged?.Invoke($"Login Error: {ex.Message}");
        throw;
    }
}

public async Task<MSession> LoginOfflineAsync(string username)
{
    StatusChanged?.Invoke($"Offline Login: {username}");
    CurrentSession = MSession.CreateOfflineSession(username);
    return await Task.FromResult(CurrentSession);
}

public async Task LaunchGameAsync(int ramMb, string? javaPath)
{
    if (CurrentSession == null)
    {
        StatusChanged?.Invoke("Error: Please log in first!");
        return;
    }

    try
    {
        StatusChanged?.Invoke("Checking Forge 1.20.1...");
        
        var forgeInstaller = new ForgeInstaller(_launcher);
        var forgeVersionName = await forgeInstaller.Install(McVersion, ForgeVersion);

        if (string.IsNullOrEmpty(forgeVersionName))
        {
            StatusChanged?.Invoke("Forge Installation Failed!");
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

        StatusChanged?.Invoke($"Launching {forgeVersionName}...");
        
        var process = await _launcher.InstallAndBuildProcessAsync(forgeVersionName, launchOption);
        
        process.Start();
        StatusChanged?.Invoke("Game Started!");
    }
    catch (Exception ex)
    {
        StatusChanged?.Invoke($"Launch Error: {ex.Message}");
    }
}

}

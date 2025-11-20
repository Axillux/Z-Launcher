using System.Collections.ObjectModel;
using ReactiveUI;
using ZLauncher.Models;


namespace ZLauncher.ViewModels;

public class ModsViewModel : ViewModelBase
{
public ObservableCollection<ModItem> Mods { get; }


    public ModsViewModel()
    {
        Mods = new ObservableCollection<ModItem>
        {
             new ModItem 
            { 
                Name = "Embeddium", 
                IsEnabled = true, 
                Description = "Рубидиум но для форджа",
                IsRequired = false,
                FileName = "embeddium-0.3.31+mc1.20.1.jar",
                DownloadUrl = "https://cdn.modrinth.com/data/sk9rgfiA/versions/UTbfe5d1/embeddium-0.3.31%2Bmc1.20.1.jar"
            },
            new ModItem 
            { 
                Name = "Oculus", 
                IsEnabled = true, 
                Description = "Шейдеры",
                IsRequired = false,
                FileName = "oculus-mc1.20.1-1.8.0.jar",
                DownloadUrl = "https://cdn.modrinth.com/data/GchcoXML/versions/iQ1SwGc3/oculus-mc1.20.1-1.8.0.jar"
            },
            new ModItem 
            { 
                Name = "Server Core", 
                IsEnabled = true, 
                Description = "Да",
                IsRequired = true,
                FileName = "servercore.jar",
                DownloadUrl = "" // Добавлю позже
            },
             new ModItem 
            { 
                Name = "Voice Chat", 
                IsEnabled = true, 
                Description = "Войс чат",
                IsRequired = false,
                FileName = "voicechat.jar",
                DownloadUrl = "" // Добавлю позже
            },
        };
    }

}

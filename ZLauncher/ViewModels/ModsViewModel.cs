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
        new ModItem { Name = "Rubidium", IsEnabled = true },
    };
}

}

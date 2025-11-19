using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using System;
using System.Threading.Tasks;
using ZLauncher.ViewModels;


namespace ZLauncher.Views;

public partial class MainView : UserControl
{
public MainView()
{
InitializeComponent();
}

protected override void OnDataContextChanged(EventArgs e)
{
    base.OnDataContextChanged(e);
    if (DataContext is MainViewModel vm)
    {
        vm.RequestLogoPicker += OpenLogoPicker;
    }
}

private async Task<Bitmap?> OpenLogoPicker()
{
    var topLevel = TopLevel.GetTopLevel(this);
    if (topLevel == null) return null;

    var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
    {
        Title = "Выберите логотип",
        AllowMultiple = false,
        FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
    });

    if (files.Count >= 1)
    {
        try
        {
            using var stream = await files[0].OpenReadAsync();
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
    return null;
}

}

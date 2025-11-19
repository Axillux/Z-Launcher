using Avalonia.Media;


namespace ZLauncher.Models;

public class SkinItem
{
public string FilePath { get; set; } = string.Empty;
public IImage? PreviewImage { get; set; }
public bool IsSelected { get; set; }
}

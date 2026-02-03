using System.Globalization;
using System.Resources;

namespace MIC.Desktop.Avalonia.Resources;

public static class ResourceHelper
{
    private static readonly ResourceManager ResourceManager = new ResourceManager("MIC.Desktop.Avalonia.Resources.Resources", typeof(ResourceHelper).Assembly);

    public static string GetString(string key)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}

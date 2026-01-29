using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MIC.Desktop.Avalonia.Controls;

public partial class UserProfilePanel : UserControl
{
    public UserProfilePanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

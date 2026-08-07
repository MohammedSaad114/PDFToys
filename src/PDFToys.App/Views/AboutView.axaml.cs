using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PDFToys.App.Views;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}

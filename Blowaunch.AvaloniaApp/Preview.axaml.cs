using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Blowaunch.AvaloniaApp
{
    public class Preview : Window
    {
        public Preview()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
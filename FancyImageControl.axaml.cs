using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Dynaframe3
{
    public partial class FancyImageControl : UserControl
    {
        public FancyImageControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

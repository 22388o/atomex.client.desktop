using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Atomex.Client.Desktop.ViewModels;

namespace Atomex.Client.Desktop.Views
{
    public class ConversionView : UserControl
    {
        public ConversionView()
        {
            InitializeComponent();

            var dgConversions = this.FindControl<DataGrid>("DgConversions");

            dgConversions.CellPointerPressed += (sender, args) =>
            {
                var cellIndex = args.Row.GetIndex();
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (DataContext is ConversionViewModel viewModel)
                        viewModel.CellPointerPressed(cellIndex);
                });
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void HelpClickHandler(object sender, RoutedEventArgs e)
        {
            var source = e.Source as Button;

            source?.SetValue(ToolTip.IsOpenProperty, true);
        }
    }
}
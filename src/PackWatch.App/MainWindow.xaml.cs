using PackWatch.App.ViewModels;
using System.Windows;

namespace PackWatch.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

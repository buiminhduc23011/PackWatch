using System.Windows.Controls;

namespace PackWatch.App.Views;

public partial class HistoryPage : UserControl
{
    public HistoryPage()
    {
        InitializeComponent();
        Loaded += (s, e) => SearchTextBox.Focus();
    }
}

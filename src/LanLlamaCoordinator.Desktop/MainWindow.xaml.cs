using System.Windows;
using LanLlamaCoordinator.Desktop.ViewModels;

namespace LanLlamaCoordinator.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new CoordinatorViewModel();
    }
}

using System.Windows;
using PragmataCoop.ViewModels;

namespace PragmataCoop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.StartMapping();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.StopAll();
    }

    private void PromoImage_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _viewModel.PromoUrl,
            UseShellExecute = true
        });
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _viewModel.StopAll();
    }
}

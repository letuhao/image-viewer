using System.Windows;
using ImageViewer.Tools.ZipRecover.ViewModels;

namespace ImageViewer.Tools.ZipRecover.Views;

/// <summary>
/// Main Window for the ZIP Recovery Tool
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    
    /// <summary>
    /// Copy the entire report to clipboard
    /// </summary>
    private void CopyReportToClipboard(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            if (!string.IsNullOrEmpty(viewModel.RecoveryReport))
            {
                Clipboard.SetText(viewModel.RecoveryReport);
                viewModel.AddLogEntry("Information", "Report copied to clipboard");
            }
            else
            {
                viewModel.AddLogEntry("Warning", "No report available. Generate report first.");
            }
        }
    }
}

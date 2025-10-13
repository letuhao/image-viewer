using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.IO;
using ImageViewer.Tools.ZipRecover.ViewModels;
using ImageViewer.Tools.ZipRecover.Views;
using ImageViewer.Tools.ZipRecover.Services;

namespace ImageViewer.Tools.ZipRecover;

/// <summary>
/// WPF ZIP Recovery Tool - Main Application Entry Point
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _host = CreateHostBuilder(e.Args).Build();
        
        try
        {
            await _host.StartAsync();
            
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start application: {ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        
        base.OnExit(e);
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<ZipRecoveryOptions>(
                    context.Configuration.GetSection("ZipRecovery"));
                
                // Services
                services.AddSingleton<IInputParser, InputParser>();
                services.AddSingleton<IArchiveHealthValidator, ArchiveHealthValidator>();
                services.AddSingleton<IZipProcessor, ZipProcessor>();
                services.AddSingleton<IExportService, ExportService>();
                
                // ViewModels
                services.AddTransient<MainViewModel>();
                
                // Views
                services.AddTransient<MainWindow>();
            });
}

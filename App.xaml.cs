using System.Configuration;
using System.Data;
using System.Windows;

namespace PanelTextor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length > 0 && e.Args[0] == "--self-test")
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try { SelfTest.Run(e.Args.Length > 1 ? e.Args[1] : "test-output"); Shutdown(0); }
            catch (Exception ex) { System.IO.Directory.CreateDirectory("test-output"); System.IO.File.WriteAllText("test-output/failure.txt", ex.ToString()); Shutdown(1); }
            return;
        }
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            var settings = Storage.LoadConfig(); UiLanguage.Set(settings.UiLanguage);
            if (!DistributionTerms.Prompt()) { Shutdown(0); return; }
            var window = new MainWindow(); MainWindow = window;
            ShutdownMode = ShutdownMode.OnMainWindowClose; window.Show();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "PanelTextor", MessageBoxButton.OK, MessageBoxImage.Error); Shutdown(1); }
    }
}



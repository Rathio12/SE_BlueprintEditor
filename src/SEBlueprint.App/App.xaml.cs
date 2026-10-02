using System.Windows;
using System.Windows.Threading;
using SEBlueprint.App.Services;
using SEBlueprint.Core;

namespace SEBlueprint.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Storage.Root = Storage.ChoosePortableRoot(AppContext.BaseDirectory);

        DispatcherUnhandledException += OnUiException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log.Write($"Unhandled: {args.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Write($"Unobserved task error: {args.Exception}");
            args.SetObserved();
        };
        Log.Write($"Started SE Blueprint Inspector {typeof(App).Assembly.GetName().Version}");
        base.OnStartup(e);
    }

    void OnUiException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Write($"UI error: {e.Exception}");
        e.Handled = true;
        if (MainWindow is { IsLoaded: true })
        {
            AppState.Current.ErrorMessage = $"{e.Exception.Message} (details in the log file)";
            return;
        }

        var message = "SE Blueprint Inspector could not start:" + Environment.NewLine + Environment.NewLine
                      + e.Exception.GetBaseException().Message + Environment.NewLine + Environment.NewLine
                      + "Details: " + Log.FilePath;
        MessageBox.Show(message, "SE Blueprint Inspector", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }
}

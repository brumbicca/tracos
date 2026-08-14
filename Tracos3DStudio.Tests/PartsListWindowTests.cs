using System.Windows;
using System.Windows.Threading;
using Xunit;

namespace Tracos3DStudio.Tests;

public class PartsListWindowTests
{
    [Fact]
    public void AbreSemExcecao_ComCozinhaEmL()
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                var project = Phase2AcceptanceTests.BuildKitchenLProject();
                var window = new PartsListWindow(project, () => { });
                window.Loaded += (_, _) =>
                    window.Dispatcher.BeginInvoke(() => window.Close(), DispatcherPriority.Background);
                window.ShowDialog();
                app.Shutdown();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        Assert.Null(caught);
    }
}

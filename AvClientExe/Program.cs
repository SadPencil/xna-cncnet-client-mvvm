using System;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientExe;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AvClientViewModel.PreStartup.Initialize();
        AvClientView.Startup.Run(BuildServiceProvider(), args);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        AvClientViewModel.PreStartup.ConfigureServices(services);
        AvClientView.PreStartup.ConfigureServices(services);

        return services.BuildServiceProvider();
    }

}

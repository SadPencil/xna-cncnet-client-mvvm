using System;

using Microsoft.Extensions.DependencyInjection;

namespace AvClientExe;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AvClientView.Startup.Run(args, BuildServiceProvider);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        AvClientViewModel.PreStartup.Initialize();

        var services = new ServiceCollection();
        AvClientView.PreStartup.ConfigureServices(services);
        AvClientViewModel.PreStartup.ConfigureServices(services);

        return services.BuildServiceProvider();
    }
}

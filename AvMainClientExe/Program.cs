using System;

using Microsoft.Extensions.DependencyInjection;

namespace AvMainClientExe;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AvMainClientViewModel.PreStartup.Initialize();
        AvMainClientView.Startup.Run(BuildServiceProvider(), args);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        AvMainClientViewModel.PreStartup.ConfigureServices(services);
        AvMainClientView.PreStartup.ConfigureServices(services);

        return services.BuildServiceProvider();
    }

}

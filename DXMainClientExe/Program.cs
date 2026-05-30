using System;

using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientExe;

internal class Program
{
    [STAThread]
    public static void Main(string[] args) => DXMainClientView.Startup.Run(BuildServiceProvider(), args);

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        DXMainClientViewModel.PreStartup.ConfigureServices(services);
        DXMainClientView.PreStartup.ConfigureServices(services);

        return services.BuildServiceProvider();
    }

}

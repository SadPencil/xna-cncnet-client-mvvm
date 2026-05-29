using System;
using System.Text;
using Avalonia;
using ClientCore;
using DXMainClientView.Services;
using DXMainClientViewModel;
using DXMainClientViewModel.Domain;
using DXMainClientViewModel.Generic;
using DXMainClientViewModel.Generic.OptionPanels;
using DXMainClientViewModel.Online;
using DXMainClientViewModel.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DXMainClientView;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Non-UI initialization (ViewModel project)
        var services = PreStartup.Initialize();

        // Settings (static singleton already initialized by PreStartup)
        services.AddSingleton(UserINISettings.Instance);

        // Domain services
        services.AddSingleton<IGameProcessSettingsService, StubGameProcessSettingsService>();
        services.AddSingleton<DirectDrawWrapperManager>();
        services.AddSingleton<PrivateMessageHandler>();

        // View-layer services
        services.AddSingleton<IUIThreadMarshaller, AvaloniaUIThreadMarshaller>();
        services.AddSingleton<DXMainClientViewModel.IUpdateService, StubUpdateService>();
        services.AddSingleton<DXMainClientViewModel.Domain.IUpdateService, StubDomainUpdateService>();
        services.AddSingleton<IIniLayoutOverlayService, IniLayoutOverlayService>();

        // Stub services (no real implementations in Avalonia view-only mode)
        services.AddSingleton<IGameProcessService, StubGameProcessService>();
        services.AddSingleton<IMusicPlayerService, StubMusicPlayerService>();
        services.AddSingleton<IDiscordHandlerService, StubDiscordHandlerService>();
        services.AddSingleton<IResolutionProvider, StubResolutionProvider>();

        // Option panel ViewModels
        services.AddSingleton<DisplayOptionsPanelViewModel>();
        services.AddSingleton<IDisplayOptionsPanelViewModel>(sp => sp.GetRequiredService<DisplayOptionsPanelViewModel>());
        services.AddSingleton<AudioOptionsPanelViewModel>();
        services.AddSingleton<IAudioOptionsPanelViewModel>(sp => sp.GetRequiredService<AudioOptionsPanelViewModel>());
        services.AddSingleton<GameOptionsPanelViewModel>();
        services.AddSingleton<IGameOptionsPanelViewModel>(sp => sp.GetRequiredService<GameOptionsPanelViewModel>());
        services.AddSingleton<CnCNetOptionsPanelViewModel>();
        services.AddSingleton<ICnCNetOptionsPanelViewModel>(sp => sp.GetRequiredService<CnCNetOptionsPanelViewModel>());
        services.AddSingleton<UpdaterOptionsPanelViewModel>();
        services.AddSingleton<IUpdaterOptionsPanelViewModel>(sp => sp.GetRequiredService<UpdaterOptionsPanelViewModel>());
        services.AddSingleton<ComponentsPanelViewModel>();
        services.AddSingleton<IComponentsPanelViewModel>(sp => sp.GetRequiredService<ComponentsPanelViewModel>());

        // Main ViewModels
        services.AddSingleton<OptionsWindowViewModel>();
        services.AddSingleton<IOptionsWindowViewModel>(sp => sp.GetRequiredService<OptionsWindowViewModel>());
        services.AddSingleton<ITopBarViewModel, TopBarViewModel>();
        services.AddSingleton<IMainMenuViewModel, MainMenuViewModel>();

        App.ServiceProvider = services.BuildServiceProvider();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

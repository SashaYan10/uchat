using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Logging;
using uchat.Services.ChatService;
using uchat.ViewModels;

namespace uchat;
using uchat.Views;  
using uchat.ViewModels;
using uchat.Services;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
			.UseMauiCommunityToolkitMarkup()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

        RegisterServices(builder.Services);
        RegisterViews(builder.Services);
        RegisterViewModels(builder.Services);

        return builder.Build();
	}

    static void RegisterViews(in IServiceCollection services)
    {
        services.AddSingleton<AppShell>();
        services.AddTransient<MainPage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<RegistrationPage>();
        services.AddTransient<ChatPage>();
    }

    static void RegisterViewModels(in IServiceCollection services)
    {
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegistrationViewModel>();
        services.AddTransient<ChatViewModel>();
    }

    static void RegisterServices(in IServiceCollection services)
    {
        services.AddSingleton<Services.NetworkService.Network>();
        services.AddSingleton<Services.AuthService.Auth>();
        services.AddSingleton<IChatService, ChatService>();
    }
}

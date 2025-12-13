namespace uchat;

public partial class AppShell : Shell
{
	public AppShell()
	{
        Application.Current.UserAppTheme = AppTheme.Light;
        Routing.RegisterRoute(nameof(RegistrationPage), typeof(RegistrationPage));
        Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
        InitializeComponent();
    }
}

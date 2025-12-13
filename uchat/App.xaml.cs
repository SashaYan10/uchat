using CommunityToolkit.Mvvm.DependencyInjection;
using uchat.ViewModels;

namespace uchat;

public partial class App : Application
{
    public App()
	{
		InitializeComponent();
    }

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
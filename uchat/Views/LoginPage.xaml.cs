using uchat.ViewModels;
using uchat.Views.Base;

namespace uchat;

public partial class LoginPage
{
	public LoginPage(LoginViewModel loginViewModel) : base(loginViewModel)
    {
		InitializeComponent();
    }
}
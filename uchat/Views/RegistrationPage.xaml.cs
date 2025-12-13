using uchat.ViewModels;
using uchat.Views.Base;

namespace uchat;

public partial class RegistrationPage
{
	public RegistrationPage(RegistrationViewModel registrationViewModel) : base(registrationViewModel)
	{
		InitializeComponent();
    }
}
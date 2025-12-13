using uchat.ViewModels;
using uchat.Views.Base;

namespace uchat;

public partial class MainPage
{
	public MainPage(MainViewModel mainViewModel) : base(mainViewModel)
	{
		InitializeComponent();
	}
}

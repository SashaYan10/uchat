using uchat.ViewModels;

namespace uchat;

public partial class ChatPage
{
	public ChatPage(ChatViewModel chatViewModel) : base(chatViewModel)
	{
		InitializeComponent();
	}

    private void Entry_Completed(object sender, EventArgs e)
    {
		BindingContext.SendMessageCommand.Execute(null);
    }
}
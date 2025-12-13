using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using uchat.Models;
using uchat.Services.ChatService;
using AuthClass = uchat.Services.AuthService.Auth;
using NetworkClass = uchat.Services.NetworkService.Network;

namespace uchat.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IChatService _chatService;
    private readonly NetworkClass _network;
    private readonly AuthClass _auth;

    [ObservableProperty]
    private string _connectionStatusText;

    [ObservableProperty]
    private Color _connectionStatusColor;

    [ObservableProperty]
    private string _username;

    public ICommand JoinCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand OpenChatCommand { get; }

    private ObservableCollection<Chat> _chats = [];
    public ObservableCollection<Chat> Chats
    {
        get => _chats;
        set => SetProperty(ref _chats, value);
    }

    private string _newChatName;

    public string NewChatName
    {
        get => _newChatName;
        set => SetProperty(ref _newChatName, value);
    }

    public MainViewModel(IChatService chatService, NetworkClass network, AuthClass auth)
    {
        _chatService = chatService;
        _network = network;
        _auth = auth;

        UpdateConnectionStatus(_network.IsConnected);

        _network.ConnectionStatusChanged += OnConnectionStatusChanged;
        _network.OnReconnected += OnReconnectedHandler;

        JoinCommand = new AsyncRelayCommand(JoinChatAsync); 
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        OpenChatCommand = new AsyncRelayCommand<Chat>(OpenChatAsync);

        _ = LoadData();
    }

    private async Task LogoutAsync()
    {
        _network.Disconnect();
        AuthClass.Logout();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }

    private async Task LoadData()
    {
        var userData = await AuthClass.GetSavedUserDataAsync();
        Chats = await _chatService.GetAllChatsObservable();
        
        Username = userData.username;
    }

    private async Task JoinChatAsync()
    {
        if (string.IsNullOrWhiteSpace(_newChatName))
        {
            return;
        }

        await _chatService.AddChat(NewChatName);
        NewChatName = string.Empty;
    }

    private async Task OpenChatAsync(Chat? chat)
    {
        var paramaters = new Dictionary<string, object>()
        {
            {nameof(Chat), chat},
        };

        await Shell.Current.GoToAsync(nameof(ChatPage), paramaters);
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        if (isConnected)
        {
            ConnectionStatusText = "Connected";
            ConnectionStatusColor = Colors.Green;
        } else
        {
            ConnectionStatusText = "Disconnected. Trying to reconnect...";
            ConnectionStatusColor = Colors.Red;
        }
    }

    private void OnConnectionStatusChanged(bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateConnectionStatus(isConnected);
        });
    }

    private void OnReconnectedHandler()
    {
        Task.Run(async () =>
        {
            var userData = await AuthClass.GetSavedUserDataAsync();
            if (!string.IsNullOrEmpty(userData.username))
            {
                await _auth.LoginUserAsync(userData.username, userData.password);
            }
        });
    }
}
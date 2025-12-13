using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Channels;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using uchat.Models;
using AuthClass = uchat.Services.AuthService.Auth;
using NetworkClass = uchat.Services.NetworkService.Network;

namespace uchat.ViewModels;

public class ChatViewModel : ObservableObject, IQueryAttributable
{
    private readonly NetworkClass _network;
    private readonly AuthClass _auth;

    public ChatViewModel(NetworkClass networkClass, AuthClass authClass)
    {
        _network = networkClass;
        _auth = authClass;

        _ = Task.Run(async () =>
        {
            var userData = await AuthClass.GetSavedUserDataAsync();
            if (!string.IsNullOrEmpty(userData.username))
            {
                MainThread.BeginInvokeOnMainThread(() => Nickname = userData.username);
            }
        });

        _network.OnPacketReceived += OnPacketReceived;
        _network.OnReconnected += OnReconnectedHandler;

        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
        DeleteMessageCommand = new AsyncRelayCommand<Message>(DeleteMessage);
        EditMessageCommand = new AsyncRelayCommand(EditMessage);
        SelectMesageToEditCommand = new RelayCommand<Message>(SelectMessageToEdit);
        DeselectMesageToEditCommand = new RelayCommand(DeselectMessageToEdit);
    }

    private void OnPacketReceived(uchat_shared.Net.PacketModel obj)
    {
        System.Diagnostics.Debug.WriteLine($"[ChatVM] Received packet: {obj.Type}, ChatId: {obj.TargetChatId}, Text: {obj.Text}");

        if (obj.Type == uchat_shared.Net.PacketType.ChatMessage)
        {
            if (CurrentChat == null || obj.TargetChatId != CurrentChat.Id) return;

            if (obj.Username == Nickname) return;

            var message = new Message()
            {
                SenderUsername = obj.Username,
                Timestamp = FormatTimestamp(obj.Timestamp),
                Text = obj.Text,
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Messages.Add(message);
            });
        }

        if (obj.Type == uchat_shared.Net.PacketType.ChatHistory)
        {
            var json = JsonSerializer.Deserialize<IEnumerable<Message>>(obj.Text);
            var history = json ?? Enumerable.Empty<Message>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Messages?.Clear();
                foreach (var item in history)
                {
                    item.Timestamp = FormatTimestamp(item.Timestamp);
                    Messages?.Add(item);
                }
            });
        }

        if(obj.Type == uchat_shared.Net.PacketType.DeleteChatMessage)
        {
            var itemToRemove = Messages.First(x=> x.Id == obj.TargetMessageId);
            MainThread.BeginInvokeOnMainThread(() => Messages.Remove(itemToRemove));
        }

        if (obj.Type == uchat_shared.Net.PacketType.EditChatMessage)
        {
            var itemToEdit = Messages.First(x => x.Id == obj.TargetMessageId);
            var itemIndex = Messages.IndexOf(itemToEdit);
            itemToEdit.Text = obj.Text;
            MainThread.BeginInvokeOnMainThread(() => {
                Messages.RemoveAt(itemIndex);
                Messages.Insert(itemIndex, itemToEdit);
            });
        }
    }

    public ICommand GoBackCommand { get; }
    public ICommand SendMessageCommand { get; }
    public ICommand DeleteMessageCommand { get; }
    public ICommand EditMessageCommand { get; }
    public ICommand SelectMesageToEditCommand { get; }
    public ICommand DeselectMesageToEditCommand { get; }

    private string nickname;
    public string Nickname
    {
        get => nickname;
        set => SetProperty(ref nickname, value);
    }

    private ObservableCollection<Message> _messages = new();
    public ObservableCollection<Message> Messages
    {
        get => _messages;
        set => SetProperty(ref _messages, value);
    }

    private Chat _currentChat;
    public Chat CurrentChat
    {
        get => _currentChat;
        set => SetProperty(ref _currentChat, value);
    }

    private string _text;
    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    private int? messageIdToEdit;
    public int? MessageIdToEdit
    {
        get => messageIdToEdit;
        set => SetProperty(ref messageIdToEdit, value);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(nameof(Chat), out object chat))
        {
            CurrentChat = (Chat)chat;
            Messages.Clear();
            _ = LoadData();
        }
    }

    private async Task LoadData()
    {
        var userData = await AuthClass.GetSavedUserDataAsync();
        Nickname = userData.username;

        await _network.SendPacketAsync(new uchat_shared.Net.PacketModel()
        {
            Type = uchat_shared.Net.PacketType.ChatHistory,
            TargetChatId = _currentChat.Id,
        });
    }

    private Task GoBackAsync()
    {
        return Shell.Current.GoToAsync("..");
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(Text)) return;

        if(MessageIdToEdit != null)
        {
            await EditMessage();
            return;
        }

        var textToSend = Text;
        var currentTime = DateTime.Now.ToShortTimeString();

        var localMessage = new Message
        {
            SenderUsername = Nickname,
            Text = textToSend,
            Timestamp = currentTime,
            ChatId = CurrentChat.Id
        };

        Messages.Add(localMessage);

        Text = string.Empty;

        await _network.SendPacketAsync(new uchat_shared.Net.PacketModel()
        {
            Type = uchat_shared.Net.PacketType.ChatMessage,
            TargetChatId = _currentChat.Id,
            Text = textToSend,
            Username = Nickname,
            Timestamp = currentTime
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

            if (_currentChat != null)
            {
                await _network.SendPacketAsync(new uchat_shared.Net.PacketModel()
                {
                    Type = uchat_shared.Net.PacketType.ChatHistory,
                    TargetChatId = _currentChat.Id,
                });
            }
        });
    }

    private string FormatTimestamp(string? timestamp)
    {
        if (string.IsNullOrEmpty(timestamp))
            return DateTime.Now.ToShortTimeString();

        if (DateTime.TryParse(timestamp, out DateTime date))
        {
            return date.ToLocalTime().ToString("HH:mm");
        }

        return timestamp;
    }

    private async Task DeleteMessage(Message message)
    {
        var isConfirmed = await Application.Current!.MainPage!.DisplayAlert("Are you sure", "Are you sure to delete your message", "Yes", "No");
        if (!isConfirmed)
        {
            return;
        }

        await _network.SendPacketAsync(new uchat_shared.Net.PacketModel() 
        {
            Type = uchat_shared.Net.PacketType.DeleteChatMessage,
            TargetMessageId = message.Id,
            TargetChatId = message.ChatId,
        });
    }

    private async Task EditMessage()
    {
        if (string.IsNullOrWhiteSpace(Text) || MessageIdToEdit == null) return;

        await _network.SendPacketAsync(new uchat_shared.Net.PacketModel()
        {
            Type = uchat_shared.Net.PacketType.EditChatMessage,
            TargetMessageId = (int)MessageIdToEdit,
            TargetChatId = _currentChat.Id,
            Text = Text
        });

        Text = string.Empty;
        MessageIdToEdit = null;
    }

    private void SelectMessageToEdit(Message message)
    {
        MessageIdToEdit = message.Id;
        Text = message.Text;
    }

    private void DeselectMessageToEdit()
    {
        Text = string.Empty;
        MessageIdToEdit = null;
    }
}
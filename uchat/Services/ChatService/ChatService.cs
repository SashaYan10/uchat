using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using uchat.Enums;
using uchat.Models;
using NetworkClass = uchat.Services.NetworkService.Network;

namespace uchat.Services.ChatService
{
    public class ChatService : IChatService
    {
        private ObservableCollection<Chat> _chats = new ();
        private bool isPacketRecived = false;
        private readonly NetworkClass _network;

        public ChatService(NetworkClass network)
        {
            _network = network;
            network.OnPacketReceived += OnPacketReceived;
        }

        private void OnPacketReceived(uchat_shared.Net.PacketModel obj)
        {
            switch (obj.Type)
            {
                case uchat_shared.Net.PacketType.ChatList:
                    Task.Run(() => LoadChatList(obj));
                    isPacketRecived = true;
                    break;
                case uchat_shared.Net.PacketType.ChatCreated:
                    Task.Run(() => AddChatFromServer(obj));
                    break;
            }
        }

        private void LoadChatList(uchat_shared.Net.PacketModel obj)
        {
            var json = JsonSerializer.Deserialize<List<Chat>>(obj.Text);
            if (json != null)
            {
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _chats.Clear();
                    foreach (var item in json)
                    {
                        if (!_chats.Any(x => x.Id == item.Id))
                            _chats.Add(item);
                    }
                });
            }
        }

        private void AddChatFromServer(uchat_shared.Net.PacketModel obj)
        {
            var chat = new Chat
            {
                Id = obj.TargetChatId,
                Name = obj.Text,
                IsGroup = false
            };

            MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!_chats.Any(c => c.Id == chat.Id))
                    _chats.Add(chat);
            });
        }


        public async Task<ObservableCollection<Chat>> GetAllChatsObservable()
        {
            isPacketRecived = false;
            
            await _network.SendPacketAsync(new uchat_shared.Net.PacketModel()
            {
                Type = uchat_shared.Net.PacketType.ChatList,
            });

            return this._chats;
        }

        public async Task AddChat(string chatName)
        {
            bool isChatPrivate = true;
            if (chatName.StartsWith('#'))
            {
                isChatPrivate = false;
                chatName = chatName.Remove(0, 1);
            }

            await _network.SendPacketAsync(new uchat_shared.Net.PacketModel()
            {
                TargetUsername = chatName,
                Type = isChatPrivate ? uchat_shared.Net.PacketType.CreatePrivateChat : uchat_shared.Net.PacketType.CreateGroupChat,
            });
        }
        }
}

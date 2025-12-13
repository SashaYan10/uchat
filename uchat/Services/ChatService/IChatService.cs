using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uchat.Models;

namespace uchat.Services.ChatService
{
    public interface IChatService
    {
        public Task<ObservableCollection<Chat>> GetAllChatsObservable();

        public Task AddChat(string chatName);
    }
}

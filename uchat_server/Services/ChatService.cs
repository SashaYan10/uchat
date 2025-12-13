using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uchat_server.DAL;
using uchat_server.Model;

namespace uchat_server.Services
{
    public class ChatService
    {
        private readonly MessageRepository _messageRepository;

        public ChatService(MessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public IEnumerable<MessageModel> GetChatHistory(int chatId)
        {
            return _messageRepository.GetChatHistory(chatId);
        }
    }
}

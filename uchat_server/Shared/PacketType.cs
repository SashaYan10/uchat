using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace uchat_shared.Net
{
    public enum PacketType : byte
    {
        Connect = 0,
        Login = 1,
        Register = 2,
        AuthSuccess = 3,
        AuthFailure = 4,
        ChatHistory = 5,
        UserConnected = 10,
        Message = 20,
        Disconnect = 25,
        Error = 30,
        ChatList = 50,
        CreatePrivateChat = 51,
        CreateGroupChat = 52,
        ChatCreated = 53,
        ChatMessage = 54,
        DeleteChatMessage = 55,
        EditChatMessage = 56,
    }

    public class PacketModel
    {
        public PacketType Type { get; set; }
        public string Username { get; set; }
        public string Text { get; set; }
        public string UID { get; set; }
        public string Password { get; set; }
        public string Timestamp { get; set; }
        public int TargetChatId { get; set; }
        public int TargetMessageId { get; set; }
        public string TargetUsername { get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static PacketModel FromJson(string json)
        {
            return JsonSerializer.Deserialize<PacketModel>(json);
        }
    }
}

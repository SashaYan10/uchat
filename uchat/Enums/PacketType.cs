using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uchat.Enums
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
        Error = 30
    }
}

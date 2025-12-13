using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uchat.Enums;

namespace uchat.Models
{
    public class Chat
    {
        //public ChatType Type { get; set; }
        //public string Name { get; set; }
        //public string LastMassege { get; set; }
        //public DateTime? LastMassegeTime { get; set; }

        //public Chat() { }

        //public Chat(ChatType type, string name, string lastMassege, DateTime? lastMassegeTime)
        //{
        //    Type = type;
        //    Name = name;
        //    LastMassege = lastMassege;
        //    LastMassegeTime = lastMassegeTime;
        //}

        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsGroup { get; set; }
    }
}

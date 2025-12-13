using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uchat_server.Model
{
    public class ChatModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsGroup { get; set; }
    }
}

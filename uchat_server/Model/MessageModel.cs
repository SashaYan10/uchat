using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uchat_server.Model
{
    public class MessageModel
    {
        public int Id { get; set; }
        public string SenderUsername { get; set; }
        public int ChatId { get; set; }
        public string Text { get; set; }
        public string Timestamp { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
    }
}

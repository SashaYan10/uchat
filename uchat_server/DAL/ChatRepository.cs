using Dapper;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using uchat_server.Model;

namespace uchat_server.DAL
{
    public class ChatRepository
    {
        private readonly string _connString;
        public ChatRepository(string connString) { _connString = connString; }

        public int CreateChat(string name, bool isGroup)
        {
            Console.WriteLine("creating chat");
            using var db = new SqliteConnection(_connString);

            var parameters = new { Name = name, IsGroup = isGroup, Date = DateTime.UtcNow.ToString("o") };

            var list = db.Query<int>("SELECT Id FROM Chats WHERE Name = @Name AND IsGroup = @IsGroup;", parameters);
            if (list?.Any() ?? false)
            {
                return list.First();
            }

            var sql = @$"INSERT INTO Chats (Name, IsGroup, CreatedAt) 
                         VALUES (@Name, @IsGroup, @Date);
                         SELECT last_insert_rowid();";

            return db.QuerySingle<int>(sql, parameters);
        }

        public void AddMember(int chatId, string username)
        {
            using var db = new SqliteConnection(_connString);
            var sql = "INSERT OR IGNORE INTO ChatMembers (ChatId, Username) VALUES (@C, @U)";
            db.Execute(sql, new { C = chatId, U = username });
        }

        public IEnumerable<ChatModel> GetUserChats(string username)
        {
            using var db = new SqliteConnection(_connString);
            var sql = @"SELECT c.* FROM Chats c
                        JOIN ChatMembers cm ON c.Id = cm.ChatId
                        WHERE cm.Username = @Username";
            return db.Query<ChatModel>(sql, new { Username = username });
        }

        public IEnumerable<string> GetChatMembers(int chatId)
        {
            using var db = new SqliteConnection(_connString);
            return db.Query<string>("SELECT Username FROM ChatMembers WHERE ChatId = @Id", new { Id = chatId });
        }
    }
}

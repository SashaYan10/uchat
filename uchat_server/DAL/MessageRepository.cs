using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uchat_server.Model;

namespace uchat_server.DAL
{
    public class MessageRepository
    {
        private readonly string _connectionString;

        public MessageRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqliteConnection DBConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public int SaveMessage(int chatId, string username, string text)
        {
            using var connection = DBConnection();
            var sql = $@"INSERT INTO Messages (ChatId, SenderUsername, Text, Timestamp) VALUES ({chatId}, '{username}', '{text}', '{DateTime.UtcNow.ToString("o")}');";

            return connection.Execute(sql);
        }

        public IEnumerable<MessageModel> GetChatHistory(int chatId)
        {
            using var connection = DBConnection();
            var sql = "SELECT * FROM Messages WHERE ChatId = @ChatId AND IsDeleted = 0 ORDER BY Id ASC";
            return connection.Query<MessageModel>(sql, new { ChatId = chatId });
        }

        public bool EditMessage(int messageId, string newText)
        {
            using var connection = DBConnection();
            var sql = @"UPDATE Messages
                        SET Text = @NewText, IsEdited = 1
                        WHERE Id = @MessageId AND IsDeleted = 0";

            var rowsAffected = connection.Execute(sql, new { MessageId = messageId, NewText = newText });
            return rowsAffected > 0;
        }

        public bool DeleteMessage(int messageId)
        {
            using var connection = DBConnection();
            var sql = "UPDATE Messages SET IsDeleted = 1 WHERE Id = @MessageId";

            var rowsAffected = connection.Execute(sql, new { MessageId = messageId });
            return rowsAffected > 0;
        }
    }
}

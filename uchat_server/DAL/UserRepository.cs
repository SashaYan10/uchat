using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uchat_shared.Net;

namespace uchat_server.DAL
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        private SqliteConnection DBConnection()
        {
            return new SqliteConnection( _connectionString );
        }

        public void AddUser(string username, string passHash, string uid)
        {
            using var connection = DBConnection();
            var sql = @"INSERT INTO Users (Username, PasswordHash, UID)
                        VALUES (@Username, @PasswordHash, @UID)";
            connection.Execute(sql, new { Username =  username, PasswordHash = passHash, UID = uid });
        }

        public User GetUserByUsername(string username)
        {
            using var connection = DBConnection();
            var sql = "SELECT * FROM Users WHERE Username = @Username";

            return connection.QueryFirstOrDefault<User>(sql, new { Username = username});
        }

        public User GetUserByUid(string uid)
        {
            using var connection = DBConnection();
            var sql = "SELECT * FROM Users WHERE UID = @UID";
            return connection.QueryFirstOrDefault<User>(sql, new { UID = uid });
        }

        public bool IsUsernameTaken(string username)
        {
            using var connection = DBConnection();
            var sql = "SELECT COUNT(Id) FROM Users WHERE Username = @Username";

            var count = connection.ExecuteScalar<int>(sql, new { Username = username });
            return count > 0;
        }

        public void UpdateUserUID(string username, string newUid)
        {
            using var connection = DBConnection();
            var sql = "UPDATE Users SET UID = @NewUid WHERE Username = @Username";

            connection.Execute(sql, new { NewUid = newUid, Username = username });
        }
    }
}

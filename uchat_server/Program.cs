using System;
using System.Net;
using System.Net.Sockets;
using uchat_shared.Net;
using Microsoft.Data.Sqlite;
using Dapper;
using uchat_server.DAL;
using uchat_server.Services;

namespace uchat_server
{
    class Program
    {
        public static List<Client> _users;
        static TcpListener _listener;
        public static readonly object _lock = new object();

        private const int DefaultPort = 7891;

        private const string DbFile = "db.db";
        private const string ConnectionString = $"Data Source={DbFile}";

        public static UserRepository UsersRepo { get; private set; }
        public static MessageRepository MessageRepo { get; private set; }
        public static AuthService Auth { get; private set; }
        public static ChatService Chat { get; private set; }
        public static ChatRepository ChatRepo { get; private set; }

        static void Main(string[] args)
        {
            if (args.Length != 1 || !int.TryParse(args[0], out int port))
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Usage: uchat_server <port>");
                    return;
                }
                else
                {
                    Console.WriteLine($"Error: Invalid port number provided: '{args[0]}'");
                    Console.WriteLine("Usage: uchat_server <port>");
                    return;
                }
            }

            ChatRepo = new ChatRepository(ConnectionString);

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var createUsersTbl = @"CREATE TABLE IF NOT EXISTS Users (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    Username TEXT NOT NULL UNIQUE,
                                    PasswordHash TEXT NOT NULL,
                                    UID TEXT NOT NULL
                                );";
            connection.Execute(createUsersTbl);

            var createChatsTbl = @"CREATE TABLE IF NOT EXISTS Chats (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        IsGroup INTEGER DEFAULT 0,
                        CreatedAt TEXT NOT NULL
                      );";
            connection.Execute(createChatsTbl);

            var createMembersTbl = @"CREATE TABLE IF NOT EXISTS ChatMembers (
                          ChatId INTEGER NOT NULL,
                          Username TEXT NOT NULL,
                          PRIMARY KEY(ChatId, Username)
                        );";
            connection.Execute(createMembersTbl);

            var createMsgTbl = @"CREATE TABLE IF NOT EXISTS Messages (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ChatId INTEGER NOT NULL,
                        SenderUsername TEXT NOT NULL,
                        Text TEXT NOT NULL,
                        Timestamp TEXT NOT NULL,
                        IsEdited INTEGER DEFAULT 0,
                        IsDeleted INTEGER DEFAULT 0
                     );";
            connection.Execute(createMsgTbl);


            var createUsrTbl = @"CREATE TABLE IF NOT EXISTS Users (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Username Text NOT NULL,
                        PasswordHash Text NOT NULL,
                        UID Text NOT NULL
                    );";
            connection.Execute(createUsrTbl);

            UsersRepo = new UserRepository(ConnectionString);
            MessageRepo = new MessageRepository(ConnectionString);
            Auth = new AuthService(UsersRepo);
            Chat = new ChatService(MessageRepo);

            _users = new List<Client>();

            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            Console.WriteLine($"Server PID: {processId}");
            Console.WriteLine("---");

            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                Console.WriteLine($"Server started and listening on {IPAddress.Any}:{port}...");

                while (true)
                {
                    var tcpClient = _listener.AcceptTcpClient();
                    Console.WriteLine($"[INFO] New connection request from: {tcpClient.Client.RemoteEndPoint}");
                    try
                    {
                        var client = new Client(tcpClient);

                        Task.Run(() => client.RunAsync());
                        //_users.Add(client);
                        //BroadcastConnection();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Client rejected: " + ex.Message);
                        tcpClient.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal server error: {ex.Message}");
            }
        }

        public static void BroadcastConnection()
        {
            var packet = new PacketModel
            {
                Type = PacketType.UserConnected,
            };
            var broadcastPacket = new PacketBuilder();

            List<Client> usersSnapshot;
            lock (_lock)
            {
                usersSnapshot = _users.ToList();
            }

            foreach (var targetUser in usersSnapshot)
            {
                try
                {
                    foreach (var usr in usersSnapshot)
                    {
                        packet.Username = usr.Username;
                        packet.UID = usr.UID;

                        broadcastPacket = new PacketBuilder();
                        broadcastPacket.WritePacket(packet);

                        targetUser.ClientSocket?.Client?.Send(broadcastPacket.GetPacketBytes());
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionAborted || ex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    Console.WriteLine($"[BROADCAST ABORTED] Connection reset for user {targetUser.Username}. Removing client.");
                    BroadcastDisconnect(targetUser.UID);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BROADCAST ERROR] Error sending to {targetUser.Username}: {ex.Message}");
                    BroadcastDisconnect(targetUser.UID);
                }
            }
        }

        public static void BroadcastMessage(PacketModel packet)
        {
            foreach (var user in _users)
            {
                var msgPacket = new PacketBuilder();
                msgPacket.WritePacket(packet);
                user.ClientSocket.Client.Send(msgPacket.GetPacketBytes());
            }
        }

        public static void BroadcastDisconnect(string uid)
        {
            Client disconnectedUser = null;
            List<Client> remainingUsers;

            lock (_lock)
            {
                disconnectedUser = _users.FirstOrDefault(x => x.UID == uid);
                if (disconnectedUser != null)
                {
                    _users.Remove(disconnectedUser);
                    remainingUsers = _users.ToList();
                }
                else
                {
                    return;
                }
            }

            foreach (var user in remainingUsers)
            {
                try
                {
                    var packet = new PacketModel
                    {
                        Type = PacketType.Disconnect,
                        UID = uid
                    };
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WritePacket(packet);

                    user?.ClientSocket?.Client?.Send(broadcastPacket.GetPacketBytes());
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionAborted || ex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    Console.WriteLine($"[BROADCAST DISCONNECT ABORTED] Connection reset for user {user.Username}. Removing client.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BROADCAST DISCONNECT ERROR] Error sending to {user.Username}: {ex.Message}");
                }
            }

            if (disconnectedUser != null)
                BroadcastMessage(new PacketModel
                {
                    Type = PacketType.Message,
                    Text = $"[{disconnectedUser.Username}] Disconnected!"
                });
        }

        public static bool IsUsernameTaken(string username)
        {
            return _users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
    }
}
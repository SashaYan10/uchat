using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using uchat_shared.Net;
using uchat_server.Services;

namespace uchat_server
{
    class Client
    {
        public string Username { get; set; }
        public string UID { get; set; }
        public TcpClient ClientSocket { get; set; }
        PacketReaderAsync _packetReader;

        public Client(TcpClient client)
        {
            ClientSocket = client;
            _packetReader = new PacketReaderAsync(ClientSocket.GetStream());
        }

        public async Task RunAsync()
        {
            try
            {
                if (!await AuthenticateAsync())
                {
                    ClientSocket.Close();
                    return;
                }

                Console.WriteLine($"Client authenticated: {Username}");

                lock (Program._lock) { Program._users.Add(this); }

                Program.BroadcastConnection();

                await SendUserChatsAsync();

                await ProcessAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}: {ex.StackTrace}");
                Program.BroadcastDisconnect(UID);
            }
            finally
            {
                if (ClientSocket.Connected)
                {
                    ClientSocket.Close();
                }

                if (!string.IsNullOrEmpty(UID))
                {
                    lock (Program._lock)
                    {
                        var clientToRemove = Program._users.FirstOrDefault(c => c.UID == this.UID);
                        if (clientToRemove != null)
                        {
                            Console.WriteLine($"[INFO] Client {Username} removed after RunAsync exit.");
                            Program._users.Remove(clientToRemove);
                            Program.BroadcastDisconnect(UID);
                        }
                    }
                }
            }
        }

        private async Task<bool> AuthenticateAsync()
        {
            try
            {
                var packet = await _packetReader.ReadPacketAsync();
                if (packet == null) return false;
                User user = null;

                if (packet.Type == PacketType.Login)
                {
                    user = Program.Auth.Login(packet.Username, packet.Password);
                    if (user == null)
                    {
                        Console.WriteLine($"[AUTH FAIL] Login failed for user: {packet.Username}");
                        await SendPacketAsync(new PacketModel { Type = PacketType.AuthFailure, Text = "Wrong credentials" });
                        return false;
                    }
                }
                else if (packet.Type == PacketType.Register)
                {
                    user = Program.Auth.Register(packet.Username, packet.Password);
                    if (user == null)
                    {
                        Console.WriteLine($"[AUTH FAIL] Registration failed for user: {packet.Username} (Taken)");
                        await SendPacketAsync(new PacketModel { Type = PacketType.AuthFailure, Text = "Taken" });
                        return false; }
                }
                else
                {
                    Console.WriteLine($"[AUTH FAIL] Unexpected packet type: {packet.Type}");
                    await SendPacketAsync(new PacketModel { Type = PacketType.AuthFailure, Text = "Protocol error" });
                    return false;
                }

                    InitializeClient(user);
                await SendPacketAsync(new PacketModel { Type = PacketType.AuthSuccess, UID = user.UID, Username = Username });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH CRITICAL] Error during authentication: {ex.Message}");
                return false;
            }
        }

        private void InitializeClient(User user)
        {
            Username = user.Username;
            UID = user.UID;
            Console.WriteLine($"Client connected: {Username} ({UID})");
        }
        public async Task SendPacketAsync(PacketModel packet)
        {
            var builder = new PacketBuilder();
            builder.WritePacket(packet);
            await ClientSocket.GetStream().WriteAsync(builder.GetPacketBytes());
        }

        private async Task SendUserChatsAsync()
        {
            var chats = Program.ChatRepo.GetUserChats(this.Username);
            Console.WriteLine($"chats for {this.Username}: {System.Text.Json.JsonSerializer.Serialize(chats)}");
            var packet = new PacketModel
            {
                Type = PacketType.ChatList,
                Text = System.Text.Json.JsonSerializer.Serialize(chats)
            };
            await SendPacketAsync(packet);
        }

        async Task ProcessAsync()
        {
            while (ClientSocket.Connected)
            {
                try
                {
                    var packet = await _packetReader.ReadPacketAsync();
                    if (packet == null) break;

                    Console.WriteLine(packet.ToJson());
                    switch (packet.Type)
                    {
                        case PacketType.CreatePrivateChat:
                            int pChatId = Program.ChatRepo.CreateChat($"{this.Username} - {packet.TargetUsername}", false);
                            Program.ChatRepo.AddMember(pChatId, this.Username);
                            Program.ChatRepo.AddMember(pChatId, packet.TargetUsername);

                            await NotifyChatCreatedAsync(pChatId, packet.TargetUsername, this.Username);
                            await NotifyChatCreatedAsync(pChatId, this.Username, packet.TargetUsername);
                            break;

                        case PacketType.CreateGroupChat:
                            Console.WriteLine(packet.ToJson());
                            int chatId = Program.ChatRepo.CreateChat(packet.TargetUsername, true);
                            Program.ChatRepo.AddMember(chatId, this.Username);

                            await NotifyChatCreatedAsync(chatId, packet.TargetUsername, this.Username);
                            break;

                        case PacketType.ChatList:
                            await SendUserChatsAsync();
                            break;

                        case PacketType.ChatMessage:
                            Program.MessageRepo.SaveMessage(packet.TargetChatId, Username, packet.Text);

                            packet.Username = Username;
                            packet.Timestamp = DateTime.UtcNow.ToString("o");

                            await NorifyAllChatMembersAsync(packet);
                            break;

                        case PacketType.DeleteChatMessage:
                            Program.MessageRepo.DeleteMessage(packet.TargetMessageId);
                            await NorifyAllChatMembersAsync(packet);
                            break;

                        case PacketType.EditChatMessage:
                            Program.MessageRepo.EditMessage(packet.TargetMessageId, packet.Text);
                            await NorifyAllChatMembersAsync(packet);
                            break;

                        case PacketType.ChatHistory:
                            var history = Program.Chat.GetChatHistory(packet.TargetChatId);

                            var historyPacket = new PacketModel
                            {
                                Type = PacketType.ChatHistory,
                                TargetChatId = packet.TargetChatId,
                                Text = System.Text.Json.JsonSerializer.Serialize(history)
                            };
                            await SendPacketAsync(historyPacket);
                            break;

                        case PacketType.Disconnect:
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}: {ex.StackTrace}");
                    Program.BroadcastDisconnect(UID);
                    ClientSocket.Close();
                    break;
                }
            }
        }

        private async Task NotifyChatCreatedAsync(int chatId, string chatName, string targetUser)
        {
            var client = Program._users.FirstOrDefault(u => u.Username == targetUser);
            if (client != null)
            {
                await client.SendPacketAsync(new PacketModel
                {
                    Type = PacketType.ChatCreated,
                    TargetChatId = chatId,
                    Text = chatName
                });
            }
        }

        private async Task NorifyAllChatMembersAsync(PacketModel packet)
        {
            var members = Program.ChatRepo.GetChatMembers(packet.TargetChatId);
            foreach (var memberName in members)
            {
                var userClient = Program._users.FirstOrDefault(u => u.Username == memberName);
                if (userClient != null) await userClient.SendPacketAsync(packet);
            }
        }
    }
}
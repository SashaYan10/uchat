using System.Net.Http;
using System.Net.Sockets;
using uchat_shared.Net;

namespace uchat.Services.NetworkService
{
    class Server : IDisposable
    {
        private TcpClient _client;
        private PacketReaderAsync _packetReader;

        private bool _isDisposed;

        public PacketReaderAsync PacketReader { get => _packetReader; set => _packetReader = value; }

        public event Action<PacketModel> PacketReceived;
        public Server()
        {
            _client = new TcpClient();
        }

        public async Task ConnectToServer(string username)
        {
            if (!_client.Connected)
            {
                await _client.ConnectAsync("127.0.0.1", 7891);
                PacketReader = new PacketReaderAsync(_client.GetStream());

                var connectPacket = new PacketModel
                {
                    Type = PacketType.Connect,
                    Username = username
                };
                var builder = new PacketBuilder();
                builder.WritePacket(connectPacket);
                await _client.GetStream().WriteAsync(builder.GetPacketBytes());

                _ = ReadPacketsAsync();
            }
        }

        private async Task ReadPacketsAsync()
        {
            /*Task.Run(() =>*/
            while (true)
            {
                try
                {
                    var packet = await PacketReader.ReadPacketAsync();

                    if (packet == null)
                    {
                        break;
                    }
                    PacketReceived?.Invoke(packet);
                }
                catch { break; }
            }
        }

        public async Task SendMessageToServer(string message, string username)
        {
            if (!_client.Connected) return;

            var packet = new PacketModel
            {
                Type = PacketType.Message,
                Username = username,
                Text = message
            };
            var messagePacket = new PacketBuilder();
            messagePacket.WritePacket(packet);
            await _client.GetStream().WriteAsync(messagePacket.GetPacketBytes());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                try
                {
                    _client.Close();
                    _client.Dispose();
                    _client = null;
                    _packetReader = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                _isDisposed = true;
            }
        }
    }
}

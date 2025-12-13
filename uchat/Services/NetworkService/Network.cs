using System.Net.Sockets;
using uchat_shared.Net;

namespace uchat.Services.NetworkService
{
    public class Network : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private PacketReaderAsync _packetReader;

        private bool _connected;

        private bool _isReconnecting;
        private int _lastPort;
        private string _lastIp;

        private bool _isDisposed;

        public event Action<PacketModel> OnPacketReceived;
        public event Action<bool> ConnectionStatusChanged;
        public event Action OnReconnected;

        public bool IsConnected => _connected;

        public async Task<bool> ConnectToServerAsync(string ip, int port)
        {
            if (_tcpClient != null && _tcpClient.Connected && _connected)
            {
                return true;
            }

            try
            {
                _lastIp = ip;
                _lastPort = port;

                _tcpClient?.Close();
                _tcpClient = new TcpClient();

                var connectTask = _tcpClient.ConnectAsync(ip, port);
                var timeout = Task.Delay(5000);

                if(await Task.WhenAny(connectTask, timeout) == timeout)
                {
                    return false;
                }

                await connectTask;

                _networkStream = _tcpClient.GetStream();
                _packetReader = new PacketReaderAsync(_networkStream);

                _connected = true;
                ConnectionStatusChanged?.Invoke(true);

                _ = StartReceiveLoopAsync();
                return true;
            }
            catch (Exception ex)
            {
                _connected = false;
                ConnectionStatusChanged?.Invoke(false);
                Console.WriteLine($"Unable connect to server: {ex}");
                return false;
            }
        }

        private async Task StartReceiveLoopAsync()
        {
            /*await Task.Run(() =>*/
            while (_connected)
            {
                try
                {
                    var packetModel = await _packetReader.ReadPacketAsync();

                    if (packetModel == null)
                    {
                        throw new SocketException();
                    }
                    OnPacketReceived?.Invoke(packetModel);
                }
                catch (Exception ex)
                {
                    if (!_connected)
                    {
                        Console.WriteLine("Client disconnected intentionally.");
                        break;
                    }
                    Console.WriteLine($"Server error: {ex}");
                    
                    _connected = false;
                    ConnectionStatusChanged?.Invoke(false);
                    _ = StartReconnectionLoopAsync();
                    
                    break;
                }
            }
        }

        private async Task StartReconnectionLoopAsync()
        {
            if (_isReconnecting) return;
            _isReconnecting = true;

            while (!_connected)
            {
                try
                {
                    _tcpClient = new TcpClient();
                    await _tcpClient.ConnectAsync(_lastIp, _lastPort);

                    _networkStream = _tcpClient.GetStream();
                    _packetReader = new PacketReaderAsync(_networkStream);

                    _connected = true;
                    ConnectionStatusChanged?.Invoke(true);

                    OnReconnected?.Invoke();

                    _ = StartReceiveLoopAsync();
                } 
                catch
                {
                    await Task.Delay(5000);
                }
            }
            _isReconnecting = false;
        }

        public async Task SendPacketAsync(PacketModel packetModel)
        {
            if (_connected)
            {
                try
                {
                    var builder = new PacketBuilder();
                    builder.WritePacket(packetModel);
                    await _networkStream.WriteAsync(builder.GetPacketBytes());
                } 
                catch
                {
                    if (!_connected) return;

                    _connected = false;
                    ConnectionStatusChanged?.Invoke(false);
                    _ = StartReconnectionLoopAsync();
                }
            }
        }

        public void Disconnect()
        {
            _connected = false;
            _isReconnecting = false;

            try
            {
                _packetReader = null;

                _tcpClient?.Close();

                _networkStream?.Close();
                _networkStream = null;

                ConnectionStatusChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting: {ex.Message}");
            }
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
                    _connected = false;
                    _isReconnecting = false;

                    _tcpClient.Close();
                    _tcpClient.Dispose();

                    _networkStream?.Close();
                    _networkStream?.Dispose();

                    _networkStream = null;
                    _tcpClient = null;
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

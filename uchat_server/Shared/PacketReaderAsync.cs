using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace uchat_shared.Net
{
    public class PacketReaderAsync
    {
        private readonly NetworkStream _ns;
        public PacketReaderAsync(NetworkStream ns)
        {
            _ns = ns;
        }

        public async Task<PacketModel> ReadPacketAsync()
        {
            byte[] typeBuffer = new byte[1];
            if (await ReadExactlyAsync(typeBuffer, 0, 1) == 0)
            {
                return null;
            }
            var type = (PacketType)typeBuffer[0];

            byte[] lengthBuffer = new byte[4];
            await ReadExactlyAsync(lengthBuffer, 0, 4);
            int length = BitConverter.ToInt32(lengthBuffer, 0);

            var buffer = new byte[length];
            await ReadExactlyAsync(buffer, 0, length);

            var json = Encoding.UTF8.GetString(buffer);
            var packet = PacketModel.FromJson(json);
            packet.Type = type;

            return packet;
        }

        private async Task<int> ReadExactlyAsync(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await _ns.ReadAsync(buffer, offset + totalRead, count - totalRead);

                if (read == 0)
                {
                    if (totalRead == 0) return 0;
                    throw new IOException("Connection closed unexpectedly.");
                }
                totalRead += read;
            }
            return totalRead;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uchat_shared.Net
{
    public class PacketBuilder
    {
        private readonly MemoryStream _ms;
        public PacketBuilder()
        {
            _ms = new MemoryStream();
        }
        
        public void WritePacket(PacketModel packet)
        {
            var json = packet.ToJson();
            var bytes = Encoding.UTF8.GetBytes(json);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);

            _ms.WriteByte((byte)packet.Type);
            _ms.Write(lengthBytes, 0, lengthBytes.Length);
            _ms.Write(bytes, 0, bytes.Length);
        }

        public byte[] GetPacketBytes()
        {
            return _ms.ToArray();
        }
    }
}

using JProtocol.Serializer;

namespace JProtocol.JPackets;

[Serializable]
public class JPacketHandshake
{
    [JField(1)] public byte Handshake;
}
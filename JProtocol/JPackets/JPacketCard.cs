using JProtocol.Serializer;

namespace JProtocol.JPackets;

[Serializable]
public class JPacketCard
{
    [JField(1)] public byte CardId;
    [JField(2)] public byte ToPlayerId;

    public JPacketCard()
    {
    }

    public JPacketCard(byte cardId, byte toPlayerId = 10)
    {
        CardId = cardId;
        ToPlayerId = toPlayerId;
    }
}
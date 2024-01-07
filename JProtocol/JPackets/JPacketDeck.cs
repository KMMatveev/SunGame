using JProtocol.Serializer;

namespace JProtocol.JPackets;

[Serializable]
public class JPacketDeck
{
    [JField(1)] public List<byte> CardDeck;
    [JField(2)] public byte ToPlayerId;

    public JPacketDeck()
    {
    }

    public JPacketDeck(List<byte> cardDeck, byte toPlayerId = 10)
    {
        CardDeck = cardDeck;
        ToPlayerId = toPlayerId;
    }
}
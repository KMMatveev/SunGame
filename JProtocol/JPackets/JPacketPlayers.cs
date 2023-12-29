using JProtocol.Serializer;

namespace JProtocol.JPackets;

[Serializable]
public class JPacketPlayers
{
    [JField(1)] public List<(byte, string)>? Players;

    public JPacketPlayers()
    {
    }

    public JPacketPlayers(List<(byte, string)>? players) => Players = players;
}
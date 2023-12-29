namespace JProtocol;

public static class JPacketTypeManager
{
    private static readonly Dictionary<JPacketType, Tuple<byte, byte>> TypeDictionary = new();

    static JPacketTypeManager()
    {
        RegisterType(JPacketType.Connection, 0, 0);
        RegisterType(JPacketType.UpdatedPlayerProperty, 1, 0);
        RegisterType(JPacketType.PlayersList, 2, 0);
        RegisterType(JPacketType.Turn, 3, 0);
        RegisterType(JPacketType.CardToTable, 4, 0);
        RegisterType(JPacketType.CardToPlayer, 4, 1);
        RegisterType(JPacketType.Win, 5, 0);
        RegisterType(JPacketType.Lose, 5, 1);
    }

    private static void RegisterType(JPacketType type, byte btype, byte bsubtype)
    {
        if (TypeDictionary.ContainsKey(type))
            throw new Exception($"Packet type {type:G} is already registered.");

        TypeDictionary.Add(type, Tuple.Create(btype, bsubtype));
    }

    public static Tuple<byte, byte> GetType(JPacketType type)
    {
        if (!TypeDictionary.TryGetValue(type, out var value))
            throw new Exception($"Packet type {type:G} is not registered.");

        return value;
    }

    public static JPacketType GetTypeFromPacket(JPacket packet)
    {
        var type = packet.PacketType;
        var subtype = packet.PacketSubtype;

        foreach (var (xPacketType, tuple) in TypeDictionary)
        {
            if (tuple.Item1 == type && tuple.Item2 == subtype)
                return xPacketType;
        }

        return JPacketType.Unknown;
    }
}
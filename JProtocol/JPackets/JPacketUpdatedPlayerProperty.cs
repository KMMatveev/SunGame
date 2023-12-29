using JProtocol.Serializer;

namespace JProtocol.JPackets;

[Serializable]
public class JPacketUpdatedPlayerProperty
{
    [JField(1)] public byte PlayerId;

    [JField(2)] public string? PropertyName;

    [JField(3)] public Type? PropertyType;

    [JField(4)] public object? PropertyValue;

    public JPacketUpdatedPlayerProperty()
    {
    }

    public JPacketUpdatedPlayerProperty(byte playerId, string? propertyName, Type? propertyType, object? propertyValue)
    {
        PlayerId = playerId;
        PropertyName = propertyName;
        PropertyType = propertyType;
        PropertyValue = propertyValue;
    }
}
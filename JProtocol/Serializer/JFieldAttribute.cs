namespace JProtocol.Serializer;

[AttributeUsage(AttributeTargets.Field)]
public class JFieldAttribute : Attribute
{
    public byte FieldId { get; }

    public JFieldAttribute(byte fieldId) => FieldId = fieldId;
}
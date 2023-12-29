namespace JProtocol;

public enum JPacketType
{
    Unknown,
    Connection,
    PlayersList,
    UpdatedPlayerProperty,
    CardToTable,
    CardToPlayer,
    Turn,
    Win,
    Lose
}
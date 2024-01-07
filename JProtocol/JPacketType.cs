namespace JProtocol;

public enum JPacketType
{
    Unknown,
    Connection,
    PlayersList,
    UpdatedPlayerProperty,
    CardsCount,
    TableDeck,
    PlayerDeck,
    CardToTable,
    CardToPlayer,
    Turn,
    Win,
    Lose
}
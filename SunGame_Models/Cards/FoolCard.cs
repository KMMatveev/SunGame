namespace SunGame_Models.Cards;

public class FoolCard
{
    public FoolCard(byte id,string? name, byte number, string suit)
    {
        Id = id;
        Name = name;
        Number = number;
        CardSuit = suit;
    }

    public FoolCard()
    {
    }

    public byte Id { get; }
    
    public string? Name { get; }
    
    public byte Number { get; }
    
    public string CardSuit { get; }
}
namespace SunGame_Server.Services;

public class GeneratorService
{
    private readonly Random _random = new();

    public Stack<byte>  GenerateDecks()
    {
        var cards = new List<byte>();
        for (byte id = 0; id < 52; id++) 
            cards.Add(id);
        
        var cardsDeck = new Stack<byte>();
        cards = cards.OrderBy(x => _random.Next()).ToList();
        foreach (var card in cards)
            cardsDeck.Push(card);

        return cardsDeck;
    }
    
    internal Stack<byte> GetNewDeck(IEnumerable<byte> reset)
    {
        var cards = reset.OrderBy(x => _random.Next()).ToList();
        var newDeck = new Stack<byte>();
        foreach (var card in cards) 
            newDeck.Push(card);
        return newDeck;
    }
}
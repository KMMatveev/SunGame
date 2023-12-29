using System.Drawing;
using SunGame_Models.Cards;

namespace SunGame_Models;

public static class CardsGenerator
{
    public static Dictionary<byte, FoolCard> GeneratePlayCards()
    {
        var playCards = new Dictionary<byte, FoolCard>();

        var suits = new[] { "0","1","2","3" };

        byte id = 0;

        foreach (var suit in suits)
        {
            for (var i = 0; i < 13; i++)
                playCards[id++] = new FoolCard(id, $"{suit}.{i}", (byte)i,suit);
        }
        
        return playCards;
    }
}
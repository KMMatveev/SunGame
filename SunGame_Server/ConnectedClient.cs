using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using JProtocol;
using JProtocol.JPackets;
using JProtocol.Serializer;
using SunGame_Models.Cards;

namespace SunGame_Server;

internal sealed class ConnectedClient : INotifyPropertyChanged
{
    private Socket Client { get; }

    private readonly Queue<byte[]> _packetSendingQueue = new();

    public readonly byte Id;
    private bool _turn;
    private string? _name;
    private byte _cardsCount;
    private bool _winStatus=false;

    private List<byte> _unknownCards;
    public List<byte> UnknownCards
    {
        get => _unknownCards;
        set
        {
            _unknownCards = value;
            //OnPropertyChanged();
        }
    }

    private FoolCard _upperCard;
    public FoolCard UpperCard
    {
        get => _upperCard;
        set
        {
            _upperCard = value;
            //OnPropertyChanged();
        }
    }

    public bool WinStatus
    {
        get => _winStatus;
        set
        {
            _winStatus = value;
            OnPropertyChanged();
        }
    }

    private Stack<byte> _cardsOnTable;
    public Stack<byte> CardsOnTable
    {
        get=> _cardsOnTable;
        set
        {
            _cardsOnTable = value;
            //OnPropertyChanged();
        }
    }

    private Stack<byte> _cards;
    
    public Stack<byte> Cards
    {
        get => _cards;
        private set
        {
            _cards = value;
        }
    }

    public string? Name
    {
        get => _name;
        private set
        {
            _name = value;
            Console.WriteLine($"Connected player with name: {Name}");
            SendPlayers();
        }
    }

    //private List<byte>? Cards { get; }

    public byte CardsCount
    {
        get => _cardsCount;
        set
        {
            _cardsCount = value;
            OnPropertyChanged();
        }
    }

    public bool Turn
    {
        get => _turn;
        set
        {
            _turn = value;
            OnPropertyChanged();
        }
    }

    private byte _cardOnTable;

    public byte CardOnTable
    {
        get => _cardOnTable;
        set
        {
            _cardOnTable = value;
            OnPropertyChanged();
        }
    }
    public FoolCard UpperFoolCard
    {
        get
        {
            return JServer.PlayCards[_cardOnTable];
        }
    }

    public bool Connected { get; set; } = false;

    public byte ToPlayerId { get; set; } = 10;
    
    public bool IsNeedCard { get; set; }
    private bool _isReady;
    public bool IsReady
    {
        get => _isReady;
        set
        {
            _isReady = value;
        }
    } 

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public ConnectedClient(Socket client, byte id,List<byte> unknowns)
    {
        Client = client;
        Id = id;
        Cards = new Stack<byte>();
        UnknownCards = unknowns;
        IsReady = false;
        Task.Run(ReceivePackets);
        Task.Run(SendPackets);
    }

    private void ReceivePackets()
    {
        while (true)
        {
            try
            {
                var buff = new byte[1024];
                Client.Receive(buff);

                var decryptedBuff = JProtocolEncryptor.Decrypt(buff);

                buff = decryptedBuff.TakeWhile((b, i) =>
                {
                    if (b != 0xFF) return true;
                    return decryptedBuff[i + 1] != 0;
                }).Concat(new byte[] { 0xFF, 0 }).ToArray();

                var parsed = JPacket.Parse(buff);

                if (parsed != null!) ProcessIncomingPacket(parsed);
            }
            catch {  Console.WriteLine("Can't recieve packet, client closed"); }//Client.Close(); break; }
        }
    }

    private void ProcessIncomingPacket(JPacket packet)
    {
        var type = JPacketTypeManager.GetTypeFromPacket(packet);

        switch (type)
        {
            case JPacketType.Connection:
                ProcessConnection(packet);
                break;
            case JPacketType.UpdatedPlayerProperty:
                ProcessUpdatingProperty(packet);
                break;
            case JPacketType.Turn:
                ProcessEndTurn();
                Console.WriteLine("EndTurn");
                break;
            case JPacketType.CardToTable:
                ProcessGettingCardOnTable(packet);
                break;
            case JPacketType.TableDeck:
                break;
            case JPacketType.PlayerDeck:
                break;
            case JPacketType.PlayersList:
                break;
            case JPacketType.CardToPlayer:
                break;
            case JPacketType.Win:
                break;
            case JPacketType.Lose:
                break;
            case JPacketType.Unknown:
                break;
            default:
                throw new ArgumentException("Получен неизвестный пакет");
        }
    }

    private void ProcessGettingCardOnTable(JPacket packet)
    {
        var packetCard = JPacketConverter.Deserialize<JPacketCard>(packet);
        //Cards!.Pop();
        //CardsCount = (byte)Cards.Count;
        //if (packetCard.ToPlayerId != 10)
        //     ToPlayerId = packetCard.ToPlayerId;
        //CardOnTable = packetCard.CardId;
        if (packetCard.ToPlayerId == Id) 
        {
            Cards.Push(packetCard.CardId);//JServer._cardsDeck.Pop());//packetCard.CardId);
            IsReady = true;
        }
        Console.WriteLine($"Card id ={packetCard.CardId}, from player {packetCard.ToPlayerId}");

        foreach (var client in JServer.ConnectedClients)
            client.GiveCard(packetCard.CardId);

        Thread.Sleep(1000);
        _turn = false;
    }

    private void ProcessUpdatingProperty(JPacket packet)
    {
        var packetProperty = JPacketConverter.Deserialize<JPacketUpdatedPlayerProperty>(packet);
        switch (packetProperty.PropertyName)
        {
            case "Name":
            {
                Name = Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!) as string;
                break;
            }
            default:
            {
                var property = typeof(ConnectedClient).GetProperty(packetProperty.PropertyName!);
                property!.SetValue(this,
                    Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!));
                OnPropertyChanged(property.Name);
                break;
            }
        }
    }

    private void ProcessEndTurn() => Turn = false;

    private void ProcessConnection(JPacket packet)
    {
        var packetConnection = JPacketConverter.Deserialize<JPacketHandshake>(packet);
        packetConnection.Handshake += 15;
        Connected= true;
        QueuePacketSend(JPacketConverter.Serialize(JPacketType.Connection, packetConnection).ToPacket());

        QueuePacketSend(JPacketConverter.Serialize(JPacketType.UpdatedPlayerProperty,
            new JPacketUpdatedPlayerProperty(Id, nameof(Id), Id.GetType(), Id)).ToPacket());
    }

    private void QueuePacketSend(byte[] packet)
        => _packetSendingQueue.Enqueue(packet);

    private void SendPackets()
    {
        while (true)
        {
            if (_packetSendingQueue.Count == 0)
                continue;

            var packet = _packetSendingQueue.Dequeue();
            var encryptedPacket = JProtocolEncryptor.Encrypt(packet);

            if (encryptedPacket.Length > 1024)
                Console.WriteLine("Max packet size is 1024 bytes.");//throw new Exception("Max packet size is 1024 bytes.");

            Client.Send(encryptedPacket);
            //Console.WriteLine($"Sended packet to client with id={Id}");
            Thread.Sleep(100);
        }
    }

    internal void UpdatePlayerProperty(byte id, string? objectName, object? obj)
        => QueuePacketSend(JPacketConverter.Serialize(JPacketType.UpdatedPlayerProperty,
                new JPacketUpdatedPlayerProperty(id, objectName, obj!.GetType(), obj))
            .ToPacket());

    private (byte, string) GetPlayerParameters() => (Id, Name)!;

    private static void SendPlayers()
    {
        var players = JServer.ConnectedClients.Select(x => x.GetPlayerParameters()).ToList();
        var packet = JPacketConverter.Serialize(JPacketType.PlayersList,
            new JPacketPlayers { Players = players });
        var bytePacket = packet.ToPacket();
        foreach (var client in JServer.ConnectedClients)
            client.QueuePacketSend(bytePacket);
    }

    private void ProcessCardToPlayer()
    {
        IsReady=true;
        IsNeedCard = true;
    }

    public void GiveCard(byte cardId)
    {
        var packetCard = JPacketConverter.Serialize(JPacketType.CardToPlayer, new JPacketCard(cardId,Id)).ToPacket();
        QueuePacketSend(packetCard);
    }
    public void GiveCard(byte cardId,byte ToId)
    {
        var packetCard = JPacketConverter.Serialize(JPacketType.CardToPlayer, new JPacketCard(cardId, ToId)).ToPacket();
        QueuePacketSend(packetCard);
    }

    public void SendTable(List<byte> cards)
    {
        var packetCard = JPacketConverter.Serialize(JPacketType.TableDeck, new JPacketDeck(cards)).ToPacket();
        QueuePacketSend(packetCard);
    }

    public void SendHand(List<byte> cards)
    {
        var packetCard = JPacketConverter.Serialize(JPacketType.PlayerDeck, new JPacketDeck(cards)).ToPacket();
        QueuePacketSend(packetCard);
    }

    public void StartTurn()
    {
        Turn = true;
        var packetTurn = JPacketConverter.Serialize(JPacketType.Turn, new JPacketEmpty()).ToPacket();
        QueuePacketSend(packetTurn);
    }

    public void Win() => QueuePacketSend(JPacketConverter.Serialize(JPacketType.Win, new JPacketEmpty()).ToPacket());
    public void Lose() => QueuePacketSend(JPacketConverter.Serialize(JPacketType.Lose, new JPacketEmpty()).ToPacket());
}
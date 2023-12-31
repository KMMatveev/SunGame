﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SunGame_Models;
using SunGame_Models.Cards;
using JProtocol;
using JProtocol.JPackets;
using JProtocol.Serializer;

namespace SunGame.Models;

public sealed class Player : INotifyPropertyChanged
{
    internal readonly Dictionary<byte, FoolCard> PlayCards = new();

    private ObservableCollection<byte> _unknownCards = new() {(byte)13,(byte)1};
    public ObservableCollection<byte> UnknownCards
    {
        get => _unknownCards;
        set
        {
            _unknownCards = value;
            OnPropertyChanged();
        }
    }

    private Stack<byte> _cardsOnTable;
    public Stack<byte> CardsOnTable
    {
        get => _cardsOnTable;
        set
        {
            _cardsOnTable = value;
            OnPropertyChanged();
        }
    }

    private Stack<byte> _cards;

    public Stack<byte> Cards
    {
        get => _cards;
        private set
        {
            _cards = value;
            OnPropertyChanged();
        }
    }

    private byte _id;

    public byte Id
    {
        get => _id;
        set
        {
            _id = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (value.Contains(' ')) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public byte CardsCount
    {
        get => _cardsCount;
        set
        {
            _cardsCount = value;
            OnPropertyChanged();
        }
    }

    private bool _turn;

    public bool Turn
    {
        get => _turn;
        set
        {
            _turn = value;
            OnPropertyChanged();
        }
    }
    
    private bool _win;

    public bool Win
    {
        get => _win;
        set
        {
            _win = value;
            OnPropertyChanged();
        }
    }
    
    private bool _lose;

    public bool Lose
    {
        get => _lose;
        set
        {
            _lose = value;
            OnPropertyChanged();
        }
    }

    private FoolCard _cardOnTable = null!;

    public FoolCard CardOnTable
    {
        get => _cardOnTable;
        set
        {
            _cardOnTable = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private Player(byte id) => Id = id;

    private Player(byte id, string name)
    {
        Id = id;
        Name = name;
    }

    public Player()
    {
        PlayersList = new ObservableCollection<Player> { new(0), new(1), new(2), new(3) };
        Name = "";
        CardOnTable = new FoolCard();

        PlayCards = CardsGenerator.GeneratePlayCards();
    }

    private ObservableCollection<Player> _playersList = null!;

    public ObservableCollection<Player> PlayersList
    {
        get => _playersList;
        set
        {
            _playersList = value;
            OnPropertyChanged();
        }
    }

    private readonly Queue<byte[]> _packetSendingQueue = new();

    private Socket? _socket;
    private IPEndPoint? _serverEndPoint;
    private byte _cardsCount;
    private string _name = null!;
    private const byte HandShake = 10;

    //public ObservableCollection<FoolCard> Cards { get; set; } = new();

    internal void Connect()
    {
        try
        {
            Connect("127.0.0.1", 1410);

            QueuePacketSend(JPacketConverter.Serialize(JPacketType.Connection,
                new JPacketHandshake
                {
                    Handshake = HandShake
                }).ToPacket());

            Thread.Sleep(300);

            QueuePacketSend(JPacketConverter.Serialize(JPacketType.UpdatedPlayerProperty,
                    new JPacketUpdatedPlayerProperty(Id, nameof(Name), Name.GetType(), Name))
                .ToPacket());

            while (true)
            {
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void Connect(string ip, int port) => Connect(new IPEndPoint(IPAddress.Parse(ip), port));

    private void Connect(IPEndPoint? server)
    {
        _serverEndPoint = server;

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(_serverEndPoint!);

        Task.Run(ReceivePackets);
        Task.Run(SendPackets);
    }

    private void ReceivePackets()
    {
        while (true)
        {
            var buff = new byte[1024];
            _socket!.Receive(buff);

            var decryptedBuff = JProtocolEncryptor.Decrypt(buff);

            var packetBuff = decryptedBuff.TakeWhile((b, i) =>
            {
                if (b != 0xFF) return true;
                return decryptedBuff[i + 1] != 0;
            }).Concat(new byte[] { 0xFF, 0 }).ToArray();
            var parsed = JPacket.Parse(packetBuff);

            if (parsed != null!) ProcessIncomingPacket(parsed);
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
            case JPacketType.PlayersList:
                ProcessGettingPlayers(packet);
                break;
            case JPacketType.CardToPlayer:
                ProcessGettingCard(packet);
                break;
            case JPacketType.Turn:
                ProcessStartingTurn(packet);
                break;
            case JPacketType.Win:
                ProcessWinning();
                break;
            case JPacketType.Lose:
                ProcessLosing();
                break;
            case JPacketType.Unknown:
                break;
            default:
                throw new ArgumentException("Получен неизвестный пакет");
        }
    }

    private void ProcessLosing() => Lose = true;

    private void ProcessWinning() => Win = true;

    private void ProcessStartingTurn(JPacket packet) => Turn = true;

    private void ProcessGettingCard(JPacket packet)
    {
        var packetCard = JPacketConverter.Deserialize<JPacketCard>(packet);
        Cards.Push(PlayCards[packetCard.CardId].Id);
        CardsCount = (byte)Cards.Count;
        OnPropertyChanged(nameof(Cards));
    }

    private void ProcessGettingPlayers(JPacket packet)
    {
        var packetPlayer = JPacketConverter.Deserialize<JPacketPlayers>(packet);
        var playersFromPacket = packetPlayer.Players;
        var playersList = playersFromPacket!.Select(x => new Player(x.Item1, x.Item2)).ToList();
        foreach (var player in playersList)
        {
            PlayersList[player.Id] = playersList[player.Id];
            OnPropertyChanged(nameof(PlayersList));
        }

        playersList[Id] = this;
        OnPropertyChanged(nameof(PlayersList));
    }

    private static void ProcessConnection(JPacket packet)
    {
        var connection = JPacketConverter.Deserialize<JPacketHandshake>(packet);

        if (connection.Handshake - 15 == HandShake)
            Console.WriteLine("Handshake successful!");
    }

    private void ProcessUpdatingProperty(JPacket packet)
    {
        var packetProperty = JPacketConverter.Deserialize<JPacketUpdatedPlayerProperty>(packet);

        switch (packetProperty.PropertyName!)
        {
            case "Id":
            {
                Id = (byte)Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!)!;
                break;
            }
            case "CardOnTable":
            {
                var value = (byte)Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!)!;
                CardOnTable = value == 0 ? new FoolCard() : PlayCards[value];
                break;
            }
            default:
            {
                var property = GetType().GetProperty(packetProperty.PropertyName!);
                property!.SetValue(PlayersList[packetProperty.PlayerId],
                    Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!));
                OnPropertyChanged(nameof(PlayersList));
                if (packetProperty.PlayerId == Id)
                {
                    property.SetValue(this,
                        Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!)!);
                    OnPropertyChanged(property.Name);
                }

                break;
            }
        }
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
                throw new Exception("Max packet size is 1024 bytes.");

            _socket!.Send(encryptedPacket);

            Thread.Sleep(100);
        }
    }

    internal void DropCardOnTable(byte id, byte playerId = 10)
    {
        Cards.Pop();//PlayCards[id]);
        CardsCount = (byte)Cards.Count;
        OnPropertyChanged(nameof(Cards));
        Turn = false;
        var packet = JPacketConverter.Serialize(JPacketType.CardToTable, new JPacketCard(id, playerId)).ToPacket();
        QueuePacketSend(packet);
    }

    internal void TakeCard(byte id)
    {
        UnknownCards.Remove(id);
        var packet = JPacketConverter.Serialize(JPacketType.CardToPlayer, new JPacketEmpty()).ToPacket();
        QueuePacketSend(packet);
        Turn = false;
    }
}
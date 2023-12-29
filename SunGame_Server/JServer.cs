using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using SunGame_Models;
using SunGame_Models.Cards;
using SunGame_Server.Services;

namespace SunGame_Server;

internal class JServer
{
    private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    internal static readonly List<ConnectedClient> ConnectedClients = new();

    private bool _listening;
    private bool _stopListening;
    private bool _isGameOver;
    
    internal static Dictionary<byte, FoolCard> PlayCards = new();

    private static Stack<byte> _cardsDeck = new();
    private static Stack<byte> reset = new();
    private static Stack<byte> _tableDeck = new();

    private int _activePlayerId;
    private string? _activeColor;
    private byte _activeNumber;

    public Task StartAsync()
    {
        try
        {
            if (_listening)
                throw new Exception("Server is already listening incoming requests.");

            _socket.Bind(new IPEndPoint(IPAddress.Any, 1410));
            _socket.Listen(4);

            _listening = true;

            Console.WriteLine("Server has been started");
            var stopThread = new Thread(() =>
            {
                while (_listening)
                    if (Console.ReadLine() == "stop")
                        Stop();
            });
            stopThread.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return Task.CompletedTask;
    }

    private void Stop()
    {
        if (!_listening)
            throw new Exception("Server is already not listening incoming requests.");
        _stopListening = true;
        _listening = false;
        _socket.Close();
        Console.WriteLine("Server have been stopped");
    }

    public void AcceptClients()
    {
        while (true)
        {
            if (_stopListening)
                return;
            Socket client;

            try
            {
                client = _socket.Accept();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(10000);
                continue;
            }

            Console.WriteLine($"[!] Accepted client from {(IPEndPoint)client.RemoteEndPoint!}");

            var c = new ConnectedClient(client, (byte)ConnectedClients.Count);

            ConnectedClients.Add(c);


            c.PropertyChanged += Client_PropertyChanged!;

            if (ConnectedClients.Count == 4)
                break;
        }
    }

    private static void Client_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var client = sender as ConnectedClient;
        foreach (var connectedClient in ConnectedClients)
        {
            var id = client!.Id;
            var propertyName = e.PropertyName;
            var type = client.GetType();
            var property = type.GetProperty(e.PropertyName!);
            var value = property!.GetValue(client);
            connectedClient.UpdatePlayerProperty(id, propertyName, value);
        }
    }

    private static void InitializeGame()
    {
        var cardsDeck = new GeneratorService().GenerateDecks();
        _cardsDeck = cardsDeck;

        PlayCards = CardsGenerator.GeneratePlayCards();
    }

    private static void SendCard(ConnectedClient connectedClient)
    {
        if (_cardsDeck.Count == 0)
        {
            _cardsDeck = new GeneratorService().GetNewDeck(reset);
            reset = new Stack<byte>();
        }

        var card = _cardsDeck.Pop();
        connectedClient.GiveCard(card);
    }

    public void StartGame()
    {
        InitializeGame();

        while (true)
        {
            if (!ConnectedClients.All(x => x.IsReady))
            {
                Thread.Sleep(1000);
                continue;
            }

            break;
        }

        //foreach (var client in ConnectedClients)
        //{
        //    for (var i = 0; i < 8; i++)
        //        client.GiveCard(_cardsDeck.Pop());
        //}

        _activePlayerId = new Random().Next(0, 3);
        int _playersInGame = 4;
        _isGameOver = false;
        

        while (!_isGameOver)
        {

            foreach (var player in ConnectedClients)
            {
                player.UnknownCards = _cardsDeck.ToList();
            }

            if (ConnectedClients.Count <= 3)
                break;
            
            var activePlayer = ConnectedClients[_activePlayerId % _playersInGame];
            
            Thread.Sleep(300);
            _tableDeck.Push(_cardsDeck.Pop());

            activePlayer.StartTurn();

            Console.WriteLine($"{activePlayer.Name}'s turn");
            do
            {
                if(!activePlayer.IsReady)
                {
                    continue;
                }
                if (activePlayer.Cards.Count==0 && _cardsDeck.Count!=0)
                {
                    SendCard(activePlayer);
                    continue;
                }
                else if(activePlayer.Cards.Count == 0 && _cardsDeck.Count == 0)
                {
                    activePlayer.WinStatus = true;
                    var lastplayer = ConnectedClients[_playersInGame-1];
                    ConnectedClients[_playersInGame-1] = activePlayer;
                    ConnectedClients[_activePlayerId] = lastplayer;
                    _playersInGame--;
                    _activePlayerId--;
                    break;
                }
                if(_playersInGame==1)
                {
                    Console.WriteLine($"Проиграл игрок {ConnectedClients[0].Name}");
                    _isGameOver=true;
                    break;
                }

                if (activePlayer.Cards.Peek()/13==_tableDeck.Peek()/13|| activePlayer.Cards.Peek() % 13 == _tableDeck.Peek() % 13)
                {
                    var bufferDeck = _tableDeck;
                    var newCount=_tableDeck.Count;
                    _tableDeck.Clear();
                    _tableDeck.Push(activePlayer.Cards.Pop());
                    foreach(var bufferCard in bufferDeck)
                        activePlayer.Cards.Push(bufferCard);
                }
                else
                {
                    _tableDeck.Push(activePlayer.Cards.Pop());
                }
                //if (activePlayer.CardOnTable == 0) continue;

                //var cardId = activePlayer.CardOnTable;
                //var card = PlayCards[cardId];
                //_activeNumber = card.Number;

                //reset.Push(activePlayer.CardOnTable);
            } 
            while (activePlayer.Turn);

            Console.WriteLine($"Player {activePlayer.Name} has finished his turn");
            _activePlayerId += 1;
        }

        Console.WriteLine("Game over");
    }
}
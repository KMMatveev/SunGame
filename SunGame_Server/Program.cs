using SunGame_Server;

Console.Title = "XServer";

var server = new JServer();
await server.StartAsync();
server.AcceptClients();
server.StartGame();
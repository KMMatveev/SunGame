using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using SunGame.Models;

namespace SunGame.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public Player Player { get; }

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

    public ReactiveCommand<byte, Unit> DropCardOnTableCommand { get; }


    public ReactiveCommand<byte, Unit> TakeCardClickCommand { get; }

    public MainWindowViewModel()
    {
        Player = new Player();
        TakeCardClickCommand = ReactiveCommand.Create<byte>(TakeCardClick);
        ConnectCommand = ReactiveCommand.Create(Connect);
        DropCardOnTableCommand = ReactiveCommand.Create<byte>(DropCardOnTable);
    }

    private void TakeCardClick(byte id)=>Player.TakeCard(id);

    private void DropCardOnTable(byte cardId) => Player.DropCardOnTable(cardId);

    private void Connect() => Task.Run(() => Player.Connect());
}
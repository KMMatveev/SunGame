using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SunGame.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void CloseWindow_OnClick(object? sender, RoutedEventArgs e) => Close();

    private void OpenMainMenu_OnClick(object? sender, RoutedEventArgs e)
    {
        MainMenuPage.IsVisible = true;
        NickInput.IsVisible = false;
        Nickname.Text = "";
    }

    private void StartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        MainMenuPage.IsVisible = false;
        NickInput.IsVisible = true;
    }

    private void TakeCardClick(object? sender, RoutedEventArgs e)
    {
        MainMenuPage.IsVisible = false;
        NickInput.IsVisible = true;
    }

    private void PlayerJoin_OnClick(object? sender, RoutedEventArgs e)
    {
        NickInput.IsVisible = false;
        GameGrid.IsVisible = true;
        Background = null;
    }
}
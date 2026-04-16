using RankingApp.Core.Models;
using System.Windows.Input;

namespace RankingApp.Views;

public partial class GameListViewPart : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable<IGame>),
            typeof(GameListViewPart),
            default(IEnumerable<IGame>));

    public IEnumerable<IGame> ItemsSource
    {
        get => (IEnumerable<IGame>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty RightPrimaryCommandProperty =
    BindableProperty.Create(
        nameof(RightPrimaryCommand),
        typeof(ICommand),
        typeof(GameListViewPart));

    public ICommand RightPrimaryCommand
    {
        get => (ICommand)GetValue(RightPrimaryCommandProperty);
        set => SetValue(RightPrimaryCommandProperty, value);
    }

    public static readonly BindableProperty RightSecondaryCommandProperty =
        BindableProperty.Create(
            nameof(RightSecondaryCommand),
            typeof(ICommand),
            typeof(GameListViewPart));

    public ICommand RightSecondaryCommand
    {
        get => (ICommand)GetValue(RightSecondaryCommandProperty);
        set => SetValue(RightSecondaryCommandProperty, value);
    }

    public static readonly BindableProperty SwipePrimaryTextProperty =
    BindableProperty.Create(
        nameof(Swipe1Text),
        typeof(string),
        typeof(GameListViewPart),
        "Action1");

    public string Swipe1Text
    {
        get => (string)GetValue(SwipePrimaryTextProperty);
        set => SetValue(SwipePrimaryTextProperty, value);
    }

    public static readonly BindableProperty SwipeSecondaryTextProperty =
    BindableProperty.Create(
        nameof(Swipe2Text),
        typeof(string),
        typeof(GameListViewPart),
        "Action1");

    public string Swipe2Text
    {
        get => (string)GetValue(SwipeSecondaryTextProperty);
        set => SetValue(SwipeSecondaryTextProperty, value);
    }

    public static readonly BindableProperty Swipe1ColorProperty =
    BindableProperty.Create(
        nameof(Swipe1Color),
        typeof(Color),
        typeof(GameListViewPart),
        Colors.Gray);

    public Color Swipe1Color
    {
        get => (Color)GetValue(Swipe1ColorProperty);
        set => SetValue(Swipe1ColorProperty, value);
    }

    public static readonly BindableProperty Swipe2ColorProperty =
    BindableProperty.Create(
        nameof(Swipe2Color),
        typeof(Color),
        typeof(GameListViewPart),
        Colors.Blue);

    public Color Swipe2Color
    {
        get => (Color)GetValue(Swipe2ColorProperty);
        set => SetValue(Swipe2ColorProperty, value);
    }

    public GameListViewPart()
	{
		InitializeComponent();
	}
}
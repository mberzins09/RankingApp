using CommunityToolkit.Maui.Views;

namespace RankingApp.Views;

public partial class ProcessingPopup : Popup
{
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        nameof(Message),
        typeof(string),
        typeof(ProcessingPopup),
        default(string));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ProcessingPopup()
    {
        InitializeComponent();
        BindingContext = this;
    }
}